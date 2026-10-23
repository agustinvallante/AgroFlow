// Behaviour of the exported Chatbot.json with synthetic Evolution envelopes.
// AgroFlow API, Evolution and the AI agent are in-memory doubles: these tests
// prove the workflow's routing and payloads, not the live services.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { simulate } from './n8n-simulator.mjs';

const workflow = JSON.parse(readFileSync(new URL('../workflows/Chatbot.json', import.meta.url), 'utf8'));
const fixture = name => JSON.parse(readFileSync(new URL(`./fixtures/${name}.json`, import.meta.url), 'utf8'));
const env = {
  AGROFLOW_API_URL: 'http://api.test',
  EVOLUTION_INTERNAL_URL: 'http://evolution.test',
  EVOLUTION_INSTANCE: 'agroflow-demo',
};

/** In-memory double with the backend semantics relevant to the workflow. */
class FakeAgroFlowApi {
  constructor({ registeredPhones = ['5493815550101'] } = {}) {
    this.registered = new Set(registeredPhones);
    this.messages = new Map();
    this.replies = new Map();
    this.sendStatuses = [];
    this.failures = [];
  }

  handle(request) {
    const path = request.url.replace(env.AGROFLOW_API_URL, '');
    assert.equal(request.credential, 'AgroFlow Service Key account', `${request.node} must use the service key`);
    if (request.method === 'POST' && path === '/api/v1/conversation-events') return this.ingest(request.body);
    if (request.method === 'POST' && path === '/api/v1/conversation-reply-intents') return this.intent(request.body);
    let match = /^\/api\/v1\/conversation-reply-intents\/([^/]+)\/send-status$/.exec(path);
    if (request.method === 'PATCH' && match) return this.sendStatus(match[1], request.body);
    match = /^\/api\/v1\/conversation-events\/([^/]+)\/processing$/.exec(path);
    if (request.method === 'PATCH' && match) return this.failure(match[1], request.body);
    throw new Error(`Unexpected AgroFlow request ${request.method} ${path}`);
  }

  ingest(body) {
    assert.deepEqual(Object.keys(body).sort(),
      ['contentType', 'instance', 'provider', 'providerMessageId', 'providerTimestamp', 'remoteAddress', 'text']);
    const key = `${body.instance}|${body.remoteAddress}|${body.providerMessageId}`;
    const phone = /^(\d+)@s\.whatsapp\.net$/.exec(body.remoteAddress)?.[1];
    const sender = { recognized: Boolean(phone && this.registered.has(phone)) };
    const existing = this.messages.get(key);
    if (existing) {
      if (existing.text !== body.text) return { statusCode: 409, body: { code: 'CONVERSATION_EVENT_CONFLICT' } };
      return { statusCode: 200, body: { disposition: 'duplicate_stop', messageId: existing.id,
        conversationId: 'conversation-1', processing: { status: 'PROCESSING' }, sender } };
    }
    const message = { id: `message-${this.messages.size + 1}`, text: body.text, token: `token-${this.messages.size + 1}`,
      remoteAddress: body.remoteAddress, instance: body.instance };
    this.messages.set(key, message);
    return { statusCode: 201, body: { disposition: 'process', messageId: message.id, conversationId: 'conversation-1',
      processing: { status: 'PROCESSING', claimToken: message.token }, sender } };
  }

  inbound(id) {
    return [...this.messages.values()].find(message => message.id === id);
  }

  intent(body) {
    const message = this.inbound(body.inboundMessageId);
    if (!message) return { statusCode: 404, body: { code: 'MESSAGE_NOT_FOUND' } };
    if (body.claimToken !== message.token) return { statusCode: 409, body: { code: 'PROCESSING_CLAIM_INVALID' } };
    const existing = this.replies.get(message.id);
    if (existing) return { statusCode: 200, body: existing };
    const reply = { replyId: `reply-${this.replies.size + 1}`, inboundMessageId: message.id, instance: message.instance,
      recipientAddress: message.remoteAddress, text: body.text, sendStatus: 'PENDING_SEND', providerMessageId: null };
    this.replies.set(message.id, reply);
    return { statusCode: 201, body: reply };
  }

  sendStatus(replyId, body) {
    const reply = [...this.replies.values()].find(candidate => candidate.replyId === replyId);
    if (!reply) return { statusCode: 404, body: { code: 'REPLY_NOT_FOUND' } };
    this.sendStatuses.push({ replyId, ...body });
    reply.sendStatus = body.status.toUpperCase();
    return { statusCode: 200, body: reply };
  }

  failure(messageId, body) {
    const message = this.inbound(messageId);
    if (body.claimToken !== message?.token) return { statusCode: 409, body: { code: 'PROCESSING_CLAIM_INVALID' } };
    this.failures.push({ messageId, errorCode: body.errorCode });
    return { statusCode: 200, body: { messageId, status: 'FAILED', errorCode: body.errorCode } };
  }
}

function harness({ api = new FakeAgroFlowApi(), evolution, agent } = {}) {
  const calls = { evolution: [], agent: [] };
  const run = body => simulate(workflow, {
    body,
    env,
    http: async request => {
      if (request.url.startsWith(env.AGROFLOW_API_URL)) {
        if (api === 'down') throw new Error('connect ECONNREFUSED');
        return api.handle(request);
      }
      assert.equal(request.url, `${env.EVOLUTION_INTERNAL_URL}/message/sendText/${env.EVOLUTION_INSTANCE}`);
      assert.equal(request.credential, 'Header Auth account', 'Evolution uses its own credential');
      calls.evolution.push(request.body);
      return evolution ? evolution(request) : { statusCode: 201, body: { key: { id: 'FIXTURE-OUT-PROVIDER-1' } } };
    },
    agent: async input => {
      calls.agent.push(input);
      return agent ? agent(input) : '[SINTÉTICO] Respuesta del agente';
    },
  });
  return { api, calls, run };
}

test('a registered sender is persisted, acknowledged, answered by the agent and reported as accepted', async () => {
  const { api, calls, run } = harness();

  const result = await run(fixture('evolution-text-registered'));

  assert.deepEqual(result.response.body, { status: 'accepted' });
  assert.equal(result.response.status, 200);
  // The webhook is acknowledged after persistence and before the agent runs.
  assert.ok(result.trace.indexOf('Register Inbound Message') < result.response.atStep);
  assert.ok(result.response.atStep < result.trace.indexOf('AI Agent1'));
  assert.equal(calls.agent.length, 1);
  assert.equal(calls.agent[0].text, '[SINTÉTICO] Hola, quiero consultar mi turno');
  assert.deepEqual(calls.agent[0].tools.sort(), ['Create Appointment', 'Lookup Appointments By Phone', 'Report En Camino']);
  assert.deepEqual(calls.evolution, [{ number: '5493815550101@s.whatsapp.net', text: '[SINTÉTICO] Respuesta del agente' }]);
  assert.deepEqual(api.sendStatuses, [{ replyId: 'reply-1', status: 'accepted', providerMessageId: 'FIXTURE-OUT-PROVIDER-1' }]);
  const ingest = result.requests.find(request => request.node === 'Register Inbound Message').body;
  assert.equal(ingest.providerTimestamp, '2026-01-01T12:00:00.000Z');
  assert.ok(!JSON.stringify(result.requests).includes('Remitente'), 'pushName never leaves n8n');
});

test('a repeated envelope stops before the agent and never sends another reply', async () => {
  const { api, calls, run } = harness();
  await run(fixture('evolution-text-registered'));

  const repeat = await run(fixture('evolution-text-registered'));

  assert.deepEqual(repeat.response.body, { status: 'duplicate' });
  assert.equal(calls.agent.length, 1);
  assert.equal(calls.evolution.length, 1);
  assert.equal(api.replies.size, 1);
  assert.ok(!repeat.trace.includes('AI Agent1'));
  assert.ok(!repeat.trace.includes('Register Reply Intent'));
});

test('an unregistered sender gets the fixed template without invoking the agent or its tools', async () => {
  const { api, calls, run } = harness();

  const result = await run(fixture('evolution-text-unregistered'));

  assert.equal(calls.agent.length, 0);
  assert.equal(calls.evolution.length, 1);
  assert.match(calls.evolution[0].text, /No pudimos identificar este número como transportista registrado/);
  assert.equal(api.sendStatuses[0].status, 'accepted');
  assert.deepEqual(result.response.body, { status: 'accepted' });
});

test('a LID sender is never treated as a registered phone', async () => {
  const { calls, run } = harness({ api: new FakeAgroFlowApi({ registeredPhones: ['123456789012345'] }) });

  await run(fixture('evolution-text-lid'));

  assert.equal(calls.agent.length, 0);
  assert.equal(calls.evolution[0].number, '123456789012345@lid');
});

for (const name of ['evolution-from-me', 'evolution-group', 'evolution-image', 'evolution-other-instance', 'evolution-blank-text',
  'evolution-extended-text-empty', 'evolution-status-broadcast']) {
  test(`${name} is acknowledged as ignored without calling the API, the agent or Evolution`, async () => {
    const { calls, run } = harness();

    const result = await run(fixture(name));

    assert.deepEqual(result.response.body, { status: 'ignored' });
    assert.deepEqual(result.requests, []);
    assert.equal(calls.agent.length, 0);
  });
}

test('an extended text (quoted reply or link preview) is processed like a plain text message', async () => {
  const { api, calls, run } = harness();

  const result = await run(fixture('evolution-extended-text'));

  assert.deepEqual(result.response.body, { status: 'accepted' });
  const ingest = result.requests.find(request => request.node === 'Register Inbound Message').body;
  assert.equal(ingest.text, '[SINTÉTICO] Respondo a tu mensaje anterior');
  assert.equal(ingest.providerMessageId, 'FIXTURE-IN-0008');
  assert.equal(calls.agent.length, 1);
  assert.equal(calls.agent[0].text, '[SINTÉTICO] Respondo a tu mensaje anterior');
  assert.equal(api.sendStatuses[0].status, 'accepted');
  assert.ok(!JSON.stringify(result.requests).includes('stanzaId'), 'quoted-message metadata is not forwarded');
});

test('a LID sender with a phone hint from Evolution is not linked to that phone', async () => {
  // The hint matches a registered number; using it would identify the sender without sufficient evidence.
  const { calls, run } = harness();

  const result = await run(fixture('evolution-lid-with-phone-hint'));

  const ingest = result.requests.find(request => request.node === 'Register Inbound Message').body;
  assert.equal(ingest.remoteAddress, '123456789012345@lid');
  assert.ok(!JSON.stringify(result.requests).includes('5493815550101'), 'the alternative phone never leaves n8n');
  assert.equal(calls.agent.length, 0);
  assert.equal(calls.evolution[0].number, '123456789012345@lid');
  assert.match(calls.evolution[0].text, /No pudimos identificar/);
});

test('an unavailable API fails closed: 503, no agent and no reply', async () => {
  const { calls, run } = harness({ api: 'down' });

  const result = await run(fixture('evolution-text-registered'));

  assert.equal(result.response.status, 503);
  assert.deepEqual(result.response.body, { status: 'not_persisted' });
  assert.equal(calls.agent.length, 0);
  assert.equal(calls.evolution.length, 0);
});

test('a rejected event (for example an unknown instance) fails closed', async () => {
  const api = new FakeAgroFlowApi();
  api.ingest = () => ({ statusCode: 403, body: { code: 'CHANNEL_INSTANCE_NOT_ALLOWED' } });
  const { calls, run } = harness({ api });

  const result = await run(fixture('evolution-text-registered'));

  assert.equal(result.response.status, 503);
  assert.equal(calls.agent.length, 0);
});

test('an agent failure closes the authorization as AGENT_ERROR and sends nothing', async () => {
  const { api, calls, run } = harness({ agent: () => { throw new Error('model unavailable'); } });

  await run(fixture('evolution-text-registered'));

  assert.deepEqual(api.failures, [{ messageId: 'message-1', errorCode: 'AGENT_ERROR' }]);
  assert.equal(calls.evolution.length, 0);
});

test('an Evolution timeout is reported as unknown, never as delivered', async () => {
  const { api, run } = harness({ evolution: () => { throw new Error('timeout of 10000ms exceeded'); } });

  await run(fixture('evolution-text-registered'));

  assert.deepEqual(api.sendStatuses, [{ replyId: 'reply-1', status: 'unknown' }]);
});

test('Evolution 4xx is failed and 5xx is unknown', async () => {
  for (const [statusCode, expected] of [[400, 'failed'], [502, 'unknown']]) {
    const { api, run } = harness({ evolution: () => ({ statusCode, body: {} }) });
    await run(fixture('evolution-text-registered'));
    assert.deepEqual(api.sendStatuses, [{ replyId: 'reply-1', status: expected }]);
  }
});

test('an accepted send without a provider identifier omits it instead of inventing one', async () => {
  const { api, run } = harness({ evolution: () => ({ statusCode: 200, body: { status: 'PENDING' } }) });

  await run(fixture('evolution-text-registered'));

  assert.deepEqual(api.sendStatuses, [{ replyId: 'reply-1', status: 'accepted' }]);
});

test('a reply intent that is not pending is never sent again', async () => {
  const api = new FakeAgroFlowApi();
  const original = api.intent.bind(api);
  api.intent = body => {
    const result = original(body);
    return { statusCode: 200, body: { ...result.body, sendStatus: 'ACCEPTED' } };
  };
  const { calls, run } = harness({ api });

  await run(fixture('evolution-text-registered'));

  assert.equal(calls.evolution.length, 0);
  assert.deepEqual(api.failures, [{ messageId: 'message-1', errorCode: 'REPLY_NOT_SENT' }]);
});

test('an intent rejected by the backend is not sent', async () => {
  const api = new FakeAgroFlowApi();
  api.intent = () => ({ statusCode: 409, body: { code: 'PROCESSING_CLAIM_INVALID' } });
  const { calls, run } = harness({ api });

  await run(fixture('evolution-text-registered'));

  assert.equal(calls.evolution.length, 0);
});

test('a reply intent for another instance is not sent through this instance', async () => {
  const api = new FakeAgroFlowApi();
  const original = api.intent.bind(api);
  api.intent = body => {
    const result = original(body);
    return { ...result, body: { ...result.body, instance: 'fixture-other-instance' } };
  };
  const { calls, run } = harness({ api });

  await run(fixture('evolution-text-registered'));

  assert.equal(calls.evolution.length, 0);
});

test('every path answers the webhook exactly once', async () => {
  for (const name of ['evolution-text-registered', 'evolution-text-unregistered', 'evolution-from-me']) {
    const { run } = harness();
    const result = await run(fixture(name));
    assert.ok(result.response, `${name} must be acknowledged`);
  }
});

test('the export is importable: valid JSON shape, positions, unique names and resolvable connections', () => {
  const names = new Set(workflow.nodes.map(node => node.name));
  assert.equal(names.size, workflow.nodes.length);
  for (const node of workflow.nodes) {
    assert.ok(Array.isArray(node.position) && node.position.length === 2 && node.position.every(Number.isFinite), node.name);
    assert.ok(typeof node.id === 'string' && node.id.length > 0);
    for (const credential of Object.values(node.credentials ?? {})) assert.deepEqual(Object.keys(credential), ['name']);
  }
  for (const [source, ports] of Object.entries(workflow.connections)) {
    assert.ok(names.has(source));
    for (const group of Object.values(ports).flat()) for (const edge of group) assert.ok(names.has(edge.node), edge.node);
  }
  assert.equal(workflow.active, false);
  assert.deepEqual(workflow.pinData, {});
});
