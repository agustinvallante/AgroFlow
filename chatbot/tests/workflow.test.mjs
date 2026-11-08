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

  // Agent tools. Turnos are anonymous in LocalDemo; the farm catalog needs the
  // service key. Tests replace these to shape the API answer.
  lookupAppointments() { return { statusCode: 200, body: [] }; }
  lookupFarms() { return { statusCode: 200, body: [] }; }
  createAppointment(body) { return { statusCode: 201, body: { id: 'appointment-1', status: 'ASIGNADO', ...body } }; }

  handle(request) {
    const path = request.url.replace(env.AGROFLOW_API_URL, '');
    const { pathname, searchParams } = new URL(request.url);
    if (pathname === '/api/v1/appointments') {
      assert.equal(request.credential, null, `${request.node} uses the anonymous LocalDemo appointment API`);
      return request.method === 'GET' ? this.lookupAppointments(searchParams) : this.createAppointment(request.body);
    }
    if (request.method === 'GET' && pathname === '/api/v1/farms') {
      assert.equal(request.credential, 'AgroFlow Service Key account', `${request.node} must use the service key`);
      return this.lookupFarms(searchParams);
    }
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
  assert.deepEqual(calls.agent[0].tools.sort(),
    ['Create Appointment', 'Lookup Appointments By Phone', 'Lookup Farms', 'Report En Camino']);
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

/** Runs the registered-sender fixture with an agent that only calls `name` once with `args`. */
async function callToolOnce(api, name, args = {}) {
  const results = [];
  const { run } = harness({ api, agent: async ({ callTool }) => {
    results.push(await callTool(name, args));
    return '[SINTÉTICO] Respuesta del agente';
  } });
  const run1 = await run(fixture('evolution-text-registered'));
  const requests = run1.requests.filter(request => request.node === name);
  assert.equal(requests.length, 1);
  return { request: requests[0], query: new URL(requests[0].url).searchParams, result: results[0] };
}

const tomorrowAppointment = { id: 'appointment-tomorrow', truck: { id: 'truck-1', plate: 'AF123BC' },
  window: { startAt: '2026-01-02T08:00:00-03:00', endAt: '2026-01-02T08:30:00-03:00' }, status: 'ASIGNADO' };

test('the appointment lookup asks for the sender\'s active appointments on any date, so tomorrow\'s turno is found', async () => {
  const api = new FakeAgroFlowApi();
  // Like the API: without `active` the default is today, which is how the turno was lost.
  api.lookupAppointments = query => ({ statusCode: 200,
    body: query.get('active') === 'true' && !query.has('date') ? [tomorrowAppointment] : [] });

  const { request, query, result } = await callToolOnce(api, 'Lookup Appointments By Phone');

  assert.equal(request.method, 'GET');
  assert.deepEqual([...query.keys()].sort(), ['active', 'phone']);
  assert.equal(query.get('phone'), '+5493815550101');
  assert.ok(request.url.includes('phone=%2B5493815550101'), 'the + of E.164 travels encoded');
  assert.equal(query.get('active'), 'true');
  assert.equal(result.statusCode, 200);
  assert.deepEqual(result.body, [tomorrowAppointment]);
});

test('no appointments, an API error and an unreachable API reach the agent as different results', async () => {
  const empty = new FakeAgroFlowApi();
  const failing = new FakeAgroFlowApi();
  failing.lookupAppointments = () => ({ statusCode: 500, body: { status: 500, code: 'INTERNAL_ERROR' } });
  const unreachable = new FakeAgroFlowApi();
  unreachable.lookupAppointments = () => { throw new Error('connect ECONNREFUSED'); };

  const emptyResult = (await callToolOnce(empty, 'Lookup Appointments By Phone')).result;
  assert.equal(emptyResult.statusCode, 200);
  assert.deepEqual(emptyResult.body, []);
  const failingResult = (await callToolOnce(failing, 'Lookup Appointments By Phone')).result;
  assert.equal(failingResult.statusCode, 500);
  assert.equal(failingResult.body.code, 'INTERNAL_ERROR');
  assert.match((await callToolOnce(unreachable, 'Lookup Appointments By Phone')).result.error, /ECONNREFUSED/);
});

test('array-returning lookup tools enable fullResponse so n8n never collapses an empty list into an empty string', async () => {
  for (const toolName of ['Lookup Appointments By Phone', 'Lookup Farms']) {
    const node = workflow.nodes.find(candidate => candidate.name === toolName);
    assert.equal(node.parameters.options?.response?.response?.fullResponse, true,
      `${toolName} must keep fullResponse enabled`);
  }

  const degraded = structuredClone(workflow);
  const brokenLookup = degraded.nodes.find(node => node.name === 'Lookup Appointments By Phone');
  delete brokenLookup.parameters.options.response.response.fullResponse;
  const api = new FakeAgroFlowApi();
  let observation;
  await simulate(degraded, {
    body: fixture('evolution-text-registered'),
    env,
    http: async request => request.url.startsWith(env.AGROFLOW_API_URL)
      ? api.handle(request)
      : { statusCode: 201, body: { key: { id: 'FIXTURE-OUT-PROVIDER-1' } } },
    agent: async ({ callTool }) => {
      observation = await callTool('Lookup Appointments By Phone');
      return '[SINTÉTICO] Respuesta del agente';
    },
  });
  assert.equal(observation, '', 'without fullResponse, n8n ToolsAgent V3 turns 200 [] into an empty observation');
});

const norte = { id: 'farm-1', code: 'FINCA-NORTE', name: 'Finca Norte', isActive: true };
const sur = { id: 'farm-2', code: 'FINCA-SUR', name: 'Finca Sur', isActive: true };

test('the farm lookup sends the caller\'s words unchanged, with the service key and only active farms', async () => {
  // n8n does not normalize names: the API decides which farm (if any) matches.
  for (const [search, farm] of [['FINCA-NORTE', norte], ['Finca Norte', norte], ['norte', norte], ['finca sur', sur]]) {
    const api = new FakeAgroFlowApi();
    api.lookupFarms = query => ({ statusCode: 200, body: query.get('search') === search ? [farm] : [] });

    const { request, query, result } = await callToolOnce(api, 'Lookup Farms', { search });

    assert.equal(request.method, 'GET');
    assert.equal(request.credential, 'AgroFlow Service Key account');
    assert.deepEqual([...query.keys()].sort(), ['active', 'search']);
    assert.equal(query.get('active'), 'true');
    assert.equal(query.get('search'), search);
    assert.equal(request.body, undefined);
    assert.equal(result.statusCode, 200);
    assert.deepEqual(result.body, [farm]);
  }
});

test('unknown, ambiguous and failed farm lookups reach the agent as distinct results', async () => {
  const answers = {
    'Finca Oeste': { statusCode: 200, body: [] },
    san: { statusCode: 200, body: [{ ...norte, code: 'F-SJ', name: 'Finca San José' }, { ...sur, code: 'F-SP', name: 'Finca San Pedro' }] },
    rechazada: { statusCode: 401, body: { status: 401, code: 'UNAUTHENTICATED' } },
    rota: { statusCode: 500, body: { status: 500, code: 'INTERNAL_ERROR' } },
  };
  const results = {};
  for (const search of [...Object.keys(answers), 'caida']) {
    const api = new FakeAgroFlowApi();
    api.lookupFarms = query => {
      if (query.get('search') === 'caida') throw new Error('connect ECONNREFUSED');
      return answers[query.get('search')];
    };
    results[search] = (await callToolOnce(api, 'Lookup Farms', { search })).result;
  }

  assert.equal(results['Finca Oeste'].statusCode, 200);
  assert.deepEqual(results['Finca Oeste'].body, []);
  assert.equal(results.san.statusCode, 200);
  assert.equal(results.san.body.length, 2);
  assert.equal(results.rechazada.statusCode, 401);
  assert.equal(results.rechazada.body.code, 'UNAUTHENTICATED');
  assert.equal(results.rota.statusCode, 500);
  assert.equal(results.rota.body.code, 'INTERNAL_ERROR');
  assert.match(results.caida.error, /ECONNREFUSED/);
});

test('the workflow keeps no farm codes: creation sends the code the agent obtained from the lookup', async () => {
  assert.ok(!/FINCA-|F-\d/.test(JSON.stringify(workflow)), 'no farm code is hardcoded in the export');
  const api = new FakeAgroFlowApi();

  const { request, result } = await callToolOnce(api, 'Create Appointment', {
    truckPlate: 'AF123BC', farmCode: 'FINCA-NORTE', cutAt: '2026-01-01T09:00:00-03:00', estimatedLoadTons: 25,
  });

  assert.equal(request.method, 'POST');
  assert.deepEqual(request.body, { carrierPhone: '+5493815550101', truckPlate: 'AF123BC', farmCode: 'FINCA-NORTE',
    cutAt: '2026-01-01T09:00:00-03:00', estimatedLoadTons: 25 });
  assert.equal(result.status, 'ASIGNADO');
});

test('the agent is told to resolve farms through the API and to ask when the name is ambiguous', () => {
  const prompt = workflow.nodes.find(node => node.name === 'AI Agent1').parameters.options.systemMessage;
  const create = workflow.nodes.find(node => node.name === 'Create Appointment').parameters.jsonBody;
  const lookup = workflow.nodes.find(node => node.name === 'Lookup Appointments By Phone').parameters.toolDescription;

  assert.match(prompt, /## Resolving the farm/);
  assert.match(prompt, /Several farms: list their names and ask the caller which one they mean/);
  assert.match(prompt, /never invent a farm or a code/);
  assert.match(prompt, /Never send the creation tool a farm code that did not come from a successful farm lookup/);
  assert.match(prompt, /an error body is not an empty result/);
  assert.match(create, /returned by the farm lookup tool/);
  assert.match(lookup, /including appointments assigned for tomorrow or later days/);
});

test('the agent is forbidden from refusing a new appointment request based on chat memory', () => {
  const prompt = workflow.nodes.find(node => node.name === 'AI Agent1').parameters.options.systemMessage;

  assert.match(prompt, /Never assume from earlier conversation messages that a truck still has \(or does not have\) an active appointment/);
  assert.match(prompt, /Never refuse a new appointment request from chat memory/);
  assert.match(prompt, /call the creation tool so the backend API decides/);
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
