import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, rmSync, rmdirSync, unlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { checkRepository, integrationPaths, validateWorkflow, validateExampleEnvironment, validateIntegrationAssets, validateTrackedPaths }
  from '../validate-integrations.mjs';

function workflow() {
  return { active: false, pinData: {}, nodes: [
    { id: 'a', name: 'Input', type: 'n8n-nodes-base.webhook', typeVersion: 2.1, parameters: {} },
    { id: 'b', name: 'Tool', type: 'n8n-nodes-base.httpRequestTool', typeVersion: 4.2,
      parameters: { url: '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments' },
      credentials: { httpHeaderAuth: { name: 'Header Auth account' } } },
  ], connections: { Input: { main: [[{ node: 'Tool', index: 0, type: 'main' }]] } } };
}

function environment() {
  return [
    'N8N_VERSION=2.37.10', 'EVOLUTION_VERSION=2.3.7',
    'POSTGRES_VERSION=16-alpine', 'REDIS_VERSION=7-alpine',
    'N8N_ENCRYPTION_KEY=replace-with-encryption-key',
    'EVOLUTION_API_KEY=replace-with-api-key', 'EVOLUTION_DB_PASSWORD=replace-with-db-password',
    'AGROFLOW_API_URL=http://host.docker.internal:5000',
  ].join('\n');
}

function repository(t) {
  const root = mkdtempSync(join(tmpdir(), 'agroflow-validator-'));
  t.after(() => {
    assert.ok(resolve(root).startsWith(resolve(tmpdir(), 'agroflow-validator-')));
    rmSync(root, { recursive: true, force: true });
  });
  const git = (...args) => execFileSync('git', ['-c', `safe.directory=${root}`,
    '-c', `core.hooksPath=${join(root, '.git/no-hooks')}`, '-c', 'commit.gpgSign=false',
    '-c', 'user.name=AgroFlow Validator Tests', '-c', 'user.email=ci@example.invalid', ...args],
  { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
  git('init', '--quiet');
  for (const path of integrationPaths) {
    mkdirSync(dirname(join(root, path)), { recursive: true });
    writeFileSync(join(root, path), path.endsWith('.json') ? JSON.stringify(workflow())
      : path.endsWith('env.example') ? environment() : 'Synthetic test fixture\n');
  }
  git('add', '--', ...integrationPaths);
  git('commit', '--quiet', '-m', 'Synthetic integrated baseline');
  return { root, base: git('rev-parse', 'HEAD'), tree: git('rev-parse', 'HEAD^{tree}') };
}

function removeIntegration(root) {
  for (const path of integrationPaths) unlinkSync(join(root, path));
  for (const path of ['chatbot/workflows', 'chatbot', 'infra']) rmdirSync(join(root, path));
}

test('portable workflow and placeholder environment pass', () => {
  validateWorkflow(workflow()); validateExampleEnvironment(environment());
});

test('main may precede chatbot import, but a partial import fails', () => {
  assert.equal(validateIntegrationAssets(new Map()), false);
  assert.throws(() => validateIntegrationAssets(new Map([[integrationPaths[0], '{}']])), /Incomplete/);
});

test('an integrated chatbot cannot silently disappear from the next revision', () => {
  assert.throws(() => validateIntegrationAssets(new Map(), true), /cannot silently disappear/);
});

test('complete import is validated', () => {
  const files = new Map(integrationPaths.map(path => [path, 'documentation']));
  files.set(integrationPaths[0], JSON.stringify(workflow()));
  files.set('infra/env.example', environment());
  assert.equal(validateIntegrationAssets(files), true);
});

test('invalid JSON fails without exposing source content', () => {
  const files = new Map(integrationPaths.map(path => [path, 'documentation']));
  files.set(integrationPaths[0], 'synthetic-private-value is not JSON');
  assert.throws(() => validateIntegrationAssets(files), error =>
    error.message.includes('Invalid workflow JSON') && !error.message.includes('synthetic-private-value'));
});

test('active exports fail', () => {
  const value = workflow(); value.active = true;
  assert.throws(() => validateWorkflow(value), /inactive/);
});

test('pinned execution data fails', () => {
  const value = workflow(); value.pinData = { Input: [{ json: { text: 'synthetic' } }] };
  assert.throws(() => validateWorkflow(value), /Pinned/);
});

test('instance metadata fails', () => {
  const value = workflow(); value.meta = { instanceId: 'synthetic-instance' };
  assert.throws(() => validateWorkflow(value), /metadata/);
});

test('credential IDs or values fail', () => {
  for (const field of ['id', 'value']) {
    const value = workflow(); value.nodes[1].credentials.httpHeaderAuth[field] = 'synthetic';
    assert.throws(() => validateWorkflow(value), /credential names only/);
  }
});

test('personal credential names fail', () => {
  const value = workflow(); value.nodes[1].credentials.httpHeaderAuth.name = 'person@example.test';
  assert.throws(() => validateWorkflow(value), /generic credential/);
});

test('unknown connection source or target fails', () => {
  const badTarget = workflow(); badTarget.connections.Input.main[0][0].node = 'Missing';
  assert.throws(() => validateWorkflow(badTarget), /Unknown connection target/);
  const badSource = workflow(); badSource.connections.Missing = { main: [] };
  assert.throws(() => validateWorkflow(badSource), /Unknown connection source/);
});

test('duplicate node names and IDs fail', () => {
  const names = workflow(); names.nodes[1].name = 'Input';
  assert.throws(() => validateWorkflow(names), /Unique node names/);
  const ids = workflow(); ids.nodes[1].id = 'a';
  assert.throws(() => validateWorkflow(ids), /Unique node IDs/);
});

test('invalid connection index fails', () => {
  const value = workflow(); value.connections.Input.main[0][0].index = -1;
  assert.throws(() => validateWorkflow(value), /index/);
});

test('unknown expression reference fails', () => {
  const value = workflow(); value.nodes[1].parameters.body = `={{ $('Missing').item.json.text }}`;
  assert.throws(() => validateWorkflow(value), /Unknown expression/);
});

test('expression references with double quotes are checked without JSON escaping artifacts', () => {
  const value = workflow(); const quote = String.fromCharCode(34);
  value.nodes[1].parameters.body = '={{ $(' + quote + 'Input' + quote + ').item.json.text }}';
  validateWorkflow(value);
  value.nodes[1].parameters.body = '={{ $(' + quote + 'Missing' + quote + ').item.json.text }}';
  assert.throws(() => validateWorkflow(value), /Unknown expression/);
});

test('tracked private environments fail but examples are allowed', () => {
  validateTrackedPaths(['frontend/.env.example', 'infra/env.example', 'README.md']);
  for (const path of ['infra/.env', '.env', 'frontend/.env.production']) {
    assert.throws(() => validateTrackedPaths([path]), /must not be tracked/);
  }
});

test('hardcoded business endpoint fails', () => {
  const value = workflow(); value.nodes[1].parameters.url = 'http://localhost:5000/api/v1/appointments';
  assert.throws(() => validateWorkflow(value), /configured AgroFlow API/);
});

test('business tools accept the exported base template and dynamic appointment routes', () => {
  for (const suffix of ['', '/{{ $json.appointmentId }}', '/{{ $json.appointmentId }}/transitions',
    "/{{ /*n8n-auto-generated-fromAI-override*/ $fromAI('appointmentId', 'ID from lookup', 'string') }}/transitions"]) {
    const value = workflow();
    value.nodes[1].parameters.url = '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments' + suffix;
    validateWorkflow(value);
  }
});

test('a foreign destination cannot pass by including the expected API substrings', () => {
  const value = workflow();
  value.nodes[1].parameters.url = '={{ "https://attacker.invalid/?target=" + $env.AGROFLOW_API_URL + "&path=/api/v1/appointments" }}';
  assert.throws(() => validateWorkflow(value), /configured AgroFlow API/);
});

test('business URL validation rejects unsupported expressions, sibling routes and appended destinations', () => {
  for (const url of [
    '={{ $env.AGROFLOW_API_URL + "/api/v1/appointments" }}',
    '={{ $env.AGROFLOW_API_URL.replace("configured", "other") }}/api/v1/appointments',
    'prefix={{ $env.AGROFLOW_API_URL }}/api/v1/appointments',
    '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments-other',
    '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments/../../other',
    '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments?redirect=https://attacker.invalid',
    '={{ $env.AGROFLOW_API_URL }}/api/v1/appointments\n',
    null, {},
  ]) {
    const value = workflow(); value.nodes[1].parameters.url = url;
    assert.throws(() => validateWorkflow(value), /configured AgroFlow API/);
  }
});

test('hardcoded appointment UUID fails', () => {
  const value = workflow(); value.nodes[1].parameters.url += '/00000000-0000-0000-0000-000000000001/transitions';
  assert.throws(() => validateWorkflow(value), /UUID/);
});

for (const field of ['bodyParameters', 'queryParameters']) {
  test(`fixed appointment IDs fail in ${field}, including expressions containing a literal UUID`, () => {
    for (const name of ['appointmentId', 'appointment_id', 'turnoId', 'id_turno', 'id']) {
      for (const id of ['00000000-0000-0000-0000-000000000001', "={{ '00000000-0000-0000-0000-000000000001' }}"]) {
        const value = workflow();
        value.nodes[1].parameters[field] = { parameters: [{ name, value: id }] };
        assert.throws(() => validateWorkflow(value), /Hardcoded appointment UUID/);
      }
    }
  });
}

for (const field of ['jsonBody', 'jsonQuery', 'body', 'query']) {
  test(`fixed appointment IDs fail in ${field} objects and JSON strings`, () => {
    for (const body of [
      { appointmentId: '00000000-0000-0000-0000-000000000001' },
      { appointment: { id: '00000000-0000-0000-0000-000000000001' } },
      { id: '00000000-0000-0000-0000-000000000001' },
    ]) {
      for (const representation of [body, JSON.stringify(body), '=' + JSON.stringify(body)]) {
        const value = workflow(); value.nodes[1].parameters[field] = representation;
        assert.throws(() => validateWorkflow(value), /Hardcoded appointment UUID/);
      }
    }
  });
}

test('templated JSON cannot hide a fixed appointment ID beside unquoted dynamic values', () => {
  for (const id of ['00000000-0000-0000-0000-000000000001', "{{ '00000000-0000-0000-0000-000000000001' }}"]) {
    const value = workflow();
    value.nodes[1].parameters.jsonBody = '={ "appointmentId": "' + id
      + '", "estimatedLoadTons": {{ $json.load }} }';
    assert.throws(() => validateWorkflow(value), /Hardcoded appointment UUID/);
  }
});

test('named fixed appointment IDs inside an opaque n8n object expression fail', () => {
  const value = workflow();
  value.nodes[1].parameters.jsonBody = "={{ { appointmentId: '00000000-0000-0000-0000-000000000001' } }}";
  assert.throws(() => validateWorkflow(value), /Hardcoded appointment UUID/);
});

test('dynamic appointment IDs and legitimate IDs of other entities are allowed', () => {
  const value = workflow();
  const otherId = '00000000-0000-0000-0000-000000000002';
  value.nodes[1].parameters.bodyParameters = { parameters: [
    { name: 'appointmentId', value: '={{ $json.appointmentId }}' }, { name: 'truckId', value: otherId },
  ] };
  value.nodes[1].parameters.queryParameters = { parameters: [{ name: 'carrierId', value: otherId }] };
  value.nodes[1].parameters.jsonBody = '=' + JSON.stringify({
    appointmentId: "{{ $fromAI('appointmentId', 'ID from lookup', 'string') }}",
    carrierId: otherId, truckId: otherId, farmId: otherId, truck: { id: otherId }, note: otherId,
  });
  validateWorkflow(value);
});

test('templated JSON preserves dynamic IDs, unquoted values and escaped text without evaluation', () => {
  const value = workflow();
  const body = { note: 'Synthetic "quoted" text', appointmentId: '{{ $json.id }}',
    estimatedLoadTons: '__DYNAMIC_LOAD__', farmId: '00000000-0000-0000-0000-000000000002' };
  const template = object => '=' + JSON.stringify(object).replace('"__DYNAMIC_LOAD__"', '{{ $json.load }}');
  value.nodes[1].parameters.jsonBody = template(body);
  validateWorkflow(value);
  value.nodes[1].parameters.jsonBody = template({ ...body, id: '00000000-0000-0000-0000-000000000001' });
  assert.throws(() => validateWorkflow(value), /Hardcoded appointment UUID/);
});

test('checkRepository rejects complete component removal against a real Git baseline', t => {
  const { root, base } = repository(t);
  assert.equal(checkRepository(root, base), true);
  removeIntegration(root);
  assert.equal(checkRepository(root), false);
  assert.throws(() => checkRepository(root, base), /cannot silently disappear/);
});

test('checkRepository rejects invalid comparison refs and unknown commit SHAs', t => {
  const { root, tree } = repository(t);
  for (const base of ['main', '--all', '../outside', 'abc']) {
    assert.throws(() => checkRepository(root, base), /must be a commit SHA/);
  }
  for (const base of ['f'.repeat(40), tree]) {
    assert.throws(() => checkRepository(root, base), /must resolve to an available commit/);
  }
});

test('manual validation without a comparison base explicitly reports the missing historical check', t => {
  const { root } = repository(t);
  const script = fileURLToPath(new URL('../validate-integrations.mjs', import.meta.url));
  for (const base of ['', '0'.repeat(40)]) {
    const output = execFileSync(process.execPath, [script, root], {
      encoding: 'utf8', env: { ...process.env, INTEGRATION_BASE_REF: base, GITHUB_EVENT_NAME: 'workflow_dispatch' },
    });
    assert.match(output, /::notice::Historical integration removal check NOT RUN/);
    assert.match(output, /Portable chatbot export\/environment validated/);
  }
});

test('CLI with a comparison base detects removal and does not claim a skipped historical check', t => {
  const { root, base } = repository(t);
  const script = fileURLToPath(new URL('../validate-integrations.mjs', import.meta.url));
  const options = { encoding: 'utf8', env: { ...process.env, INTEGRATION_BASE_REF: base } };
  assert.doesNotMatch(execFileSync(process.execPath, [script, root], options), /NOT RUN/);
  removeIntegration(root);
  assert.throws(() => execFileSync(process.execPath, [script, root], { ...options, stdio: ['ignore', 'pipe', 'pipe'] }),
    error => error.status === 1 && /cannot silently disappear/.test(error.stderr));
});

test('literal authentication headers fail without exposing their value', () => {
  const value = workflow(); value.nodes[1].parameters.header = { name: 'apikey', value: 'synthetic-private-value' };
  assert.throws(() => validateWorkflow(value), error =>
    error.message.includes('value redacted') && !error.message.includes('synthetic-private-value'));
});

test('key-shaped strings fail without exposing their value', () => {
  const value = workflow(); const fake = 'sk-' + 'x'.repeat(24);
  value.nodes[1].parameters.body = fake;
  assert.throws(() => validateWorkflow(value), error => !error.message.includes(fake));
});

test('example environment rejects real-looking credentials', () => {
  assert.throws(() => validateExampleEnvironment(environment().replace('replace-with-api-key', 'synthetic-private-value')),
    /placeholder/);
});

test('example environment rejects floating image tags', () => {
  assert.throws(() => validateExampleEnvironment(environment().replace('2.37.10', 'latest')), /pinned/);
});
