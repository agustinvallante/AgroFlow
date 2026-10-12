import test from 'node:test';
import assert from 'node:assert/strict';
import { integrationPaths, validateWorkflow, validateExampleEnvironment, validateIntegrationAssets, validateTrackedPaths }
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

test('hardcoded appointment UUID fails', () => {
  const value = workflow(); value.nodes[1].parameters.url += '/00000000-0000-0000-0000-000000000001/transitions';
  assert.throws(() => validateWorkflow(value), /UUID/);
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
