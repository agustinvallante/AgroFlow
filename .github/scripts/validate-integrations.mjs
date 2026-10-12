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
      assert.ok(['OpenAI account', 'Header Auth account'].includes(credential.name), 'Use generic credential names');
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
    if (node.type !== 'n8n-nodes-base.httpRequestTool') continue;
    const url = node.parameters?.url ?? '';
    assert.ok(url.includes('$env.AGROFLOW_API_URL') && url.includes('/api/v1/appointments'),
      `Business tool ${node.name} must use configured AgroFlow API`);
    assert.ok(!/[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}/i.test(url), 'Hardcoded appointment UUID forbidden');
  }
  checkSecrets(workflow);
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
    const baseFiles = execFileSync('git', ['-c', `safe.directory=${root}`, 'ls-tree', '-r', '--name-only', baseRef],
      { cwd: root, encoding: 'utf8' }).split(/\r?\n/);
    required ||= integrationPaths.some(path => baseFiles.includes(path));
  }
  return validateIntegrationAssets(files, required);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const root = resolve(process.argv[2] ?? '.');
  try {
    const present = checkRepository(root, process.env.INTEGRATION_BASE_REF ?? '');
    console.log(present ? 'Portable chatbot export/environment validated; no runtime or paid calls executed.'
      : 'Chatbot not yet integrated: validator unit tests still run; partial imports will fail.');
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
