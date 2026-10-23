import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

export const integrationPaths = [
  'chatbot/workflows/Chatbot.json', 'chatbot/README.md',
  'infra/compose.yaml', 'infra/env.example',
];

const secretName = /^(authorization|apikey|api[-_]?key|access[-_]?token|password|secret)$/i;
const secretValue = /sk-[a-zA-Z0-9_-]{20,}|gh[pousr]_[a-zA-Z0-9]{20,}|-----BEGIN [A-Z ]*PRIVATE KEY-----/;
const uuid = /[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}/i;
// Deliberately accept the exported n8n template format, not arbitrary JavaScript.
const businessUrl = /^=\{\{\s*\$env\.AGROFLOW_API_URL\s*\}\}\/api\/v1\/appointments(?:\/(?:[a-zA-Z0-9-]+|\{\{(?:(?!\{\{|\}\})[\s\S])+\}\})(?:\/transitions)?)?$/;
const evolutionUrl = /^=\{\{\s*\$env\.EVOLUTION_INTERNAL_URL\s*\}\}\/message\/sendText\/\{\{\s*\$env\.EVOLUTION_INSTANCE\s*\}\}$/;
// Persistence routes of the WhatsApp channel: exact templates and methods, only
// for the ordinary HTTP node (never an agent tool) and only with the service key.
const apiBase = String.raw`^=\{\{\s*\$env\.AGROFLOW_API_URL\s*\}\}\/api\/v1\/`;
const dynamicSegment = String.raw`\{\{(?:(?!\{\{|\}\})[\s\S])+\}\}`;
const channelRoutes = [
  { method: 'POST', pattern: new RegExp(`${apiBase}conversation-events$`) },
  { method: 'PATCH', pattern: new RegExp(`${apiBase}conversation-events\/${dynamicSegment}\/processing$`) },
  { method: 'POST', pattern: new RegExp(`${apiBase}conversation-reply-intents$`) },
  { method: 'PATCH', pattern: new RegExp(`${apiBase}conversation-reply-intents\/${dynamicSegment}\/send-status$`) },
];
const channelPrefix = /\/api\/v1\/conversation-(?:events|reply-intents)(?![a-z])/;
const serviceCredential = 'AgroFlow Service Key account';
const allowedCredentialNames = ['OpenAI account', 'Header Auth account', serviceCredential];
const agentNodeType = '@n8n/n8n-nodes-langchain.agent';
const httpNodeTypes = new Set([
  'n8n-nodes-base.httpRequest', 'n8n-nodes-base.httpRequestTool',
  '@n8n/n8n-nodes-langchain.toolHttpRequest',
]);

function matchesTemplate(pattern, value) {
  const match = typeof value === 'string' ? pattern.exec(value) : null;
  return Boolean(match && match[0] === value);
}

function usesServiceCredential(node) {
  return Object.values(node.credentials ?? {}).some(credential => credential?.name === serviceCredential);
}

function isChannelRequest(node) {
  const parameters = node.parameters ?? {};
  return node.type === 'n8n-nodes-base.httpRequest' && channelRoutes.some(route =>
    route.method === parameters.method && matchesTemplate(route.pattern, parameters.url));
}

function checkChannelRequest(node) {
  const parameters = node.parameters ?? {};
  assert.ok(!uuid.test(parameters.url), 'Hardcoded channel resource UUID forbidden');
  assert.ok(parameters.authentication === 'genericCredentialType' && parameters.genericAuthType === 'httpHeaderAuth',
    `Channel request ${node.name} must authenticate with the AgroFlow service key credential`);
  const credentials = Object.entries(node.credentials ?? {});
  assert.ok(credentials.length === 1 && credentials[0][0] === 'httpHeaderAuth' && credentials[0][1]?.name === serviceCredential,
    `Channel request ${node.name} must authenticate with the AgroFlow service key credential`);
}

function checkHttpDestination(node) {
  if (usesServiceCredential(node)) {
    // The service key may only travel to the channel persistence routes.
    assert.ok(isChannelRequest(node), `The AgroFlow service key credential is only allowed on channel requests (${node.name})`);
  }
  if (!httpNodeTypes.has(node.type)) {
    // A different package/alias must get an explicit policy, not a silent skip.
    assert.ok(!/(?:^|\.)(?:httpRequest(?:Tool)?|toolHttpRequest)$/i.test(node.type), 'Unsupported HTTP node type');
    return;
  }
  const parameters = node.parameters ?? {};
  const options = Object.hasOwn(parameters, 'options') ? parameters.options : {};
  assert.ok(options && typeof options === 'object' && !Array.isArray(options),
    'HTTP options must be a static object, not an expression');
  for (const proxy of [parameters.proxy, options.proxy]) {
    assert.ok(proxy === undefined || proxy === '', 'HTTP proxy overrides are forbidden; value redacted');
  }
  // Pagination may issue requests to a secondary URL. None is needed by this export.
  assert.ok(!Object.hasOwn(options, 'pagination'), 'HTTP pagination requires an explicit destination policy');

  const url = parameters.url;
  if (typeof url === 'string' && channelPrefix.test(url)) {
    // Agent tools never reach ingestion, reply intents or send status: the
    // model must not be able to fabricate channel records or results.
    assert.ok(isChannelRequest(node),
      `Channel route in ${node.name} must use the ordinary HTTP node with its exact method and template`);
    checkChannelRequest(node);
    return;
  }
  const business = matchesTemplate(businessUrl, url);
  const evolution = node.type === 'n8n-nodes-base.httpRequest' && parameters.method === 'POST'
    && matchesTemplate(evolutionUrl, url);
  assert.ok(business || evolution,
    `HTTP node ${node.name} must use configured AgroFlow API appointment routes or the permitted Evolution sendText template`);
  if (!business) return;
  assert.ok(!uuid.test(url), 'Hardcoded appointment UUID forbidden');
  // The legacy tool uses parametersBody/parametersQuery instead of bodyParameters/queryParameters.
  for (const field of ['bodyParameters', 'queryParameters', 'parametersBody', 'parametersQuery',
    'jsonBody', 'jsonQuery', 'body', 'query']) {
    checkRequestIdentifiers(parameters[field]);
  }
}

function appointmentField(name, parent = '', namedParameter = false) {
  const normalized = name.replace(/[_-]/g, '').toLowerCase();
  return ['appointmentid', 'turnoid', 'idturno'].includes(normalized)
    || (normalized === 'id' && (namedParameter || ['', 'appointment', 'turno'].includes(parent.toLowerCase())));
}

function rejectFixedAppointmentId(value) {
  assert.ok(!uuid.test(JSON.stringify(value) ?? ''), 'Hardcoded appointment UUID forbidden');
}

function parseRequestJson(text) {
  const source = text.trim().replace(/^=/, '');
  // Replace template tokens before parsing JSON, without evaluating expressions.
  // Restore them afterwards so a literal UUID inside an expression is still checked.
  const expressions = new Map();
  let quoted = false;
  let escaped = false;
  let json = '';
  for (let index = 0; index < source.length; index++) {
    if (source.startsWith('{{', index)) {
      const end = source.indexOf('}}', index + 2);
      if (end < 0) return undefined;
      const marker = `__agroflow_expression_${expressions.size}__`;
      expressions.set(marker, source.slice(index, end + 2));
      json += quoted ? marker : JSON.stringify(marker);
      index = end + 1;
      continue;
    }
    const char = source[index];
    json += char;
    if (escaped) escaped = false;
    else if (quoted && char === '\\') escaped = true;
    else if (char === '"') quoted = !quoted;
  }
  try {
    return JSON.parse(json, (_key, value) => typeof value === 'string'
      ? value.replace(/__agroflow_expression_\d+__/g, marker => expressions.get(marker) ?? marker) : value);
  } catch { return undefined; }
}

function checkRequestIdentifiers(value, parent = '') {
  if (typeof value === 'string') {
    const parsed = parseRequestJson(value);
    if (parsed && typeof parsed === 'object') checkRequestIdentifiers(parsed, parent);
    else {
      // Also catch named literal IDs in an opaque n8n object expression. This is
      // a static guard, not a JavaScript evaluator or a runtime identity check.
      for (const match of value.matchAll(/(?:^|[,{])\s*["']?(?:appointment[_-]?id|turno[_-]?id|id[_-]?turno)["']?\s*:\s*([^,}\r\n]*)/gi)) {
        rejectFixedAppointmentId(match[1]);
      }
    }
    return;
  }
  if (!value || typeof value !== 'object') return;
  if (Array.isArray(value)) {
    for (const item of value) checkRequestIdentifiers(item, parent);
    return;
  }
  if (typeof value.name === 'string' && appointmentField(value.name, parent, true)) {
    rejectFixedAppointmentId(value.value);
  }
  for (const [key, child] of Object.entries(value)) {
    if (appointmentField(key, parent)) rejectFixedAppointmentId(child);
    checkRequestIdentifiers(child, key);
  }
}

function checkSecrets(value, path = 'workflow') {
  if (typeof value === 'string') {
    assert.ok(!secretValue.test(value), `Possible embedded secret at ${path}; value redacted`);
    return;
  }
  if (!value || typeof value !== 'object') return;
  if (typeof value.name === 'string' && secretName.test(value.name) && typeof value.value === 'string') {
    assert.ok(value.value.startsWith('=') || value.value.startsWith('replace-with-'),
      `Literal authentication header at ${path}; value redacted`);
  }
  for (const [key, child] of Object.entries(value)) {
    if (secretName.test(key) && typeof child === 'string' && child.length) {
      assert.ok(child.startsWith('=') || child.startsWith('replace-with-'),
        `Literal credential at ${path}.${key}; value redacted`);
    }
    checkSecrets(child, `${path}.${key}`);
  }
}

function checkNodeReferences(value, names) {
  if (typeof value === 'string') {
    for (const match of value.matchAll(/\$\((['\x22])([^'\x22]+)\1\)/g)) {
      assert.ok(names.has(match[2]), `Unknown expression reference ${match[2]}`);
    }
  } else if (value && typeof value === 'object') {
    for (const child of Object.values(value)) checkNodeReferences(child, names);
  }
}

export function validateTrackedPaths(paths) {
  for (const path of paths) {
    const filename = path.split('/').at(-1);
    assert.ok(filename === '.env.example' || !/^\.env(?:\.|$)/.test(filename),
      `Private environment must not be tracked: ${path}`);
  }
}

export function validateWorkflow(workflow) {
  checkSecrets(workflow);
  assert.equal(workflow.active, false, 'Portable workflows must be inactive');
  assert.ok(workflow.pinData && !Object.keys(workflow.pinData).length, 'Pinned execution data is forbidden');
  assert.ok(Array.isArray(workflow.nodes) && workflow.nodes.length, 'Workflow nodes are required');
  assert.ok(!workflow.id && !workflow.meta?.instanceId, 'Instance metadata is not portable');
  const names = new Set();
  const ids = new Set();
  for (const node of workflow.nodes) {
    assert.ok(typeof node.name === 'string' && node.name.length && !names.has(node.name), 'Unique node names required');
    assert.ok(typeof node.id === 'string' && node.id.length && !ids.has(node.id), 'Unique node IDs required');
    assert.ok(typeof node.type === 'string' && Number.isFinite(node.typeVersion), 'Node type/version required');
    names.add(node.name);
    ids.add(node.id);
    for (const credential of Object.values(node.credentials ?? {})) {
      assert.deepEqual(Object.keys(credential), ['name'], 'Export credential names only, never IDs or values');
      assert.ok(allowedCredentialNames.includes(credential.name), 'Use generic credential names');
    }
  }
  assert.ok(workflow.connections && typeof workflow.connections === 'object', 'Connections required');
  for (const [source, ports] of Object.entries(workflow.connections)) {
    assert.ok(names.has(source), `Unknown connection source ${source}`);
    for (const groups of Object.values(ports)) {
      assert.ok(Array.isArray(groups), 'Connection ports must contain arrays');
      for (const group of groups) {
        assert.ok(Array.isArray(group), 'Connection groups must be arrays');
        for (const edge of group) {
          assert.ok(names.has(edge.node), `Unknown connection target ${edge.node}`);
          assert.ok(Number.isInteger(edge.index) && edge.index >= 0, 'Invalid connection index');
        }
      }
    }
  }
  for (const node of workflow.nodes) {
    checkNodeReferences(node.parameters ?? {}, names);
    checkHttpDestination(node);
  }
  checkConversationOrdering(workflow);
  checkSecrets(workflow);
}

function reachableWithout(workflow, blocked) {
  const starts = workflow.nodes.filter(node => node.type === 'n8n-nodes-base.webhook').map(node => node.name);
  const seen = new Set();
  const pending = starts.filter(name => !blocked.has(name));
  while (pending.length) {
    const name = pending.pop();
    if (seen.has(name)) continue;
    seen.add(name);
    for (const group of workflow.connections[name]?.main ?? []) {
      for (const edge of group ?? []) {
        if (!blocked.has(edge.node)) pending.push(edge.node);
      }
    }
  }
  return seen;
}

/**
 * In a conversational workflow (one with an AI agent) the agent must be
 * reachable only through the inbound persistence request, and the Evolution
 * send only through a registered reply intent. Static graph check over main
 * connections; it does not evaluate expressions or runtime branches.
 */
function checkConversationOrdering(workflow) {
  const agents = workflow.nodes.filter(node => node.type === agentNodeType);
  if (!agents.length) return;
  // Successful executions would keep message text and phone numbers in n8n;
  // AgroFlow API holds the authoritative history.
  const settings = workflow.settings ?? {};
  assert.ok(settings.saveDataSuccessExecution === 'none' && settings.saveManualExecutions === false
    && settings.saveExecutionProgress === false,
  'Conversational workflows must not retain successful, manual or in-progress execution data');
  const channelNodes = route => new Set(workflow.nodes.filter(node => isChannelRequest(node)
    && matchesTemplate(channelRoutes[route].pattern, node.parameters.url)).map(node => node.name));
  const ingest = channelNodes(0);
  const intents = channelNodes(2);
  const sends = workflow.nodes.filter(node => node.type === 'n8n-nodes-base.httpRequest'
    && matchesTemplate(evolutionUrl, node.parameters?.url)).map(node => node.name);

  const beforeIngest = reachableWithout(workflow, ingest);
  for (const name of [...agents.map(node => node.name), ...sends]) {
    assert.ok(!beforeIngest.has(name), `${name} must only run after the inbound message is persisted`);
  }
  const beforeIntent = reachableWithout(workflow, intents);
  for (const name of sends) {
    assert.ok(!beforeIntent.has(name), `${name} must only run after the reply intent is registered`);
  }
}

export function validateExampleEnvironment(text) {
  const values = Object.fromEntries(text.split(/\r?\n/).filter(line => line.trim() && !line.trim().startsWith('#'))
    .map(line => { const split = line.indexOf('='); assert.ok(split > 0, 'Invalid environment assignment');
      return [line.slice(0, split).trim(), line.slice(split + 1).trim()]; }));
  for (const key of ['N8N_ENCRYPTION_KEY', 'EVOLUTION_API_KEY', 'EVOLUTION_DB_PASSWORD']) {
    assert.ok(values[key]?.startsWith('replace-with-'), `${key} must contain a placeholder; value redacted`);
  }
  for (const key of ['N8N_VERSION', 'EVOLUTION_VERSION', 'POSTGRES_VERSION', 'REDIS_VERSION']) {
    assert.match(values[key] ?? '', /^\d+(?:\.\d+){0,2}(?:-[a-z0-9.-]+)?$/i, `${key} must be pinned, not latest`);
  }
  assert.ok(values.AGROFLOW_API_URL?.startsWith('http'), 'API URL required');
  assert.ok(!secretValue.test(text), 'Possible embedded secret in example environment; value redacted');
}

export function validateIntegrationAssets(files, required = false) {
  const present = integrationPaths.filter(path => files.has(path));
  if (!present.length) {
    assert.ok(!required, 'Chatbot assets present in the base cannot silently disappear');
    return false;
  }
  assert.equal(present.length, integrationPaths.length, 'Incomplete chatbot/infra import');
  let workflow;
  try { workflow = JSON.parse(files.get(integrationPaths[0])); }
  catch { throw new Error('Invalid workflow JSON; content is not echoed'); }
  validateWorkflow(workflow);
  validateExampleEnvironment(files.get('infra/env.example'));
  return true;
}

export function checkRepository(root, baseRef = '') {
  const tracked = execFileSync('git', ['-c', `safe.directory=${root}`, 'ls-files', '--cached'],
    { cwd: root, encoding: 'utf8' }).split(/\r?\n/);
  validateTrackedPaths(tracked);
  const files = new Map(integrationPaths.filter(path => existsSync(resolve(root, path)))
    .map(path => [path, readFileSync(resolve(root, path), 'utf8')]));
  let required = existsSync(resolve(root, 'chatbot'));
  if (baseRef && !/^0+$/.test(baseRef)) {
    assert.match(baseRef, /^[a-f0-9]{40}$/i, 'CI base must be a commit SHA');
    let baseFiles;
    try {
      baseFiles = execFileSync('git', ['-c', `safe.directory=${root}`, 'ls-tree', '-r', '--name-only', `${baseRef}^{commit}`],
        { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).split(/\r?\n/);
    } catch { throw new Error('CI base must resolve to an available commit'); }
    required ||= integrationPaths.some(path => baseFiles.includes(path));
  }
  return validateIntegrationAssets(files, required);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const root = resolve(process.argv[2] ?? '.');
  try {
    const baseRef = process.env.INTEGRATION_BASE_REF ?? '';
    const present = checkRepository(root, baseRef);
    if (!baseRef || /^0+$/.test(baseRef)) {
      console.log('::notice::Historical integration removal check NOT RUN: no comparison commit SHA was supplied. '
        + 'workflow_dispatch has no automatic base; supply INTEGRATION_BASE_REF locally to enable this check.');
    }
    console.log(present ? 'Portable chatbot export/environment validated; no runtime or paid calls executed.'
      : 'Chatbot not yet integrated: validator unit tests still run; partial imports will fail.');
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
