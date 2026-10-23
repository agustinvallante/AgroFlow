// Prueba de aceptación local del circuito persistido, sin Docker:
//   fixture de Evolution → Chatbot.json (simulador) → AgroFlow API real → SQLite
//   → endpoints del dashboard → proxy de Vite del frontend → reinicio de la API.
// Evolution, WhatsApp y el agente son dobles: esta prueba NO demuestra que
// funcionen en vivo. Usa una base SQLite temporal y claves aleatorias que
// nunca se imprimen; no toca bases existentes.
//
// Requisitos: `dotnet build backend/AgroFlow.Api` y `npm ci` en frontend/.
// Uso: node chatbot/tests/acceptance.mjs
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { existsSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { simulate } from './n8n-simulator.mjs';

const root = resolve(fileURLToPath(new URL('../..', import.meta.url)));
const apiDir = join(root, 'backend', 'AgroFlow.Api');
const apiDll = join(apiDir, 'bin', 'Debug', 'net8.0', 'AgroFlow.Api.dll');
const viteBin = join(root, 'frontend', 'node_modules', 'vite', 'bin', 'vite.js');
const workflow = JSON.parse(readFileSync(join(root, 'chatbot', 'workflows', 'Chatbot.json'), 'utf8'));
const fixture = JSON.parse(readFileSync(new URL('./fixtures/evolution-text-registered.json', import.meta.url), 'utf8'));

assert.ok(existsSync(apiDll), 'Compilá la API antes: dotnet build backend/AgroFlow.Api');
assert.ok(existsSync(viteBin), 'Instalá el frontend antes: npm ci (en frontend/)');

const API_PORT = 5099;
const VITE_PORT = 5179;
const API = `http://127.0.0.1:${API_PORT}`;
const INSTANCE = 'agroflow-demo';
const SERVICE_KEY = randomBytes(32).toString('hex');
const DASHBOARD_KEY = randomBytes(32).toString('hex');
// Número sintético en el rango ficticio 555 de la demo; se registra por la API.
const DRIVER = { name: '[SINTÉTICO] Transportista Aceptación', dni: '20777000177', whatsApp: '+5493815550177' };
const JID = '5493815550177@s.whatsapp.net';

const workdir = mkdtempSync(join(tmpdir(), 'agroflow-acceptance-'));
const dbPath = join(workdir, 'agroflow-acceptance.db');
const children = new Set();
const step = (n, text) => console.log(`✓ ${n}. ${text}`);

function start(command, args, options) {
  const child = spawn(command, args, { ...options, stdio: ['ignore', 'pipe', 'pipe'] });
  children.add(child);
  let output = '';
  child.stdout.on('data', chunk => { output += chunk; });
  child.stderr.on('data', chunk => { output += chunk; });
  child.on('exit', () => children.delete(child));
  child.output = () => output;
  return child;
}

async function stop(child) {
  if (!child || child.exitCode !== null) return;
  const exited = new Promise(done => child.once('exit', done));
  child.kill();
  await exited;
}

async function waitFor(url, child, label) {
  for (let attempt = 0; attempt < 120; attempt++) {
    if (child.exitCode !== null) throw new Error(`${label} terminó antes de responder (código ${child.exitCode}).`);
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch { /* todavía no escucha */ }
    await new Promise(done => setTimeout(done, 500));
  }
  throw new Error(`${label} no respondió en ${url}.`);
}

function startApi() {
  return start('dotnet', [apiDll], {
    cwd: apiDir,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: API,
      LocalDemo__Enabled: 'true',
      // Nunca la precarga privada real del equipo en esta prueba.
      LocalDemo__TeamFile: join(workdir, 'sin-precarga.json'),
      LocalDemo__WhatsAppInstance: INSTANCE,
      ConnectionStrings__AgroFlowDb: `Data Source=${dbPath};Default Timeout=5`,
      ChannelIntegration__ServiceKey: SERVICE_KEY,
      Dashboard__ApiKey: DASHBOARD_KEY,
      LogPath: '',
    },
  });
}

const dashboard = (path, init = {}) => fetch(`${API}${path}`, {
  ...init,
  headers: { 'Content-Type': 'application/json', 'X-AgroFlow-Dashboard-Key': DASHBOARD_KEY, ...init.headers },
});

/** Ejecuta el workflow exportado contra la API real; Evolution y el agente son dobles. */
function runWorkflow(calls) {
  return simulate(workflow, {
    body: { ...fixture, data: { ...fixture.data, key: { ...fixture.data.key, remoteJid: JID, id: 'FIXTURE-ACCEPTANCE-0001' } } },
    env: { AGROFLOW_API_URL: API, EVOLUTION_INTERNAL_URL: 'http://evolution.test', EVOLUTION_INSTANCE: INSTANCE },
    http: async request => {
      if (!request.url.startsWith(API)) {
        calls.evolution.push(request.body);
        return { statusCode: 201, body: { key: { id: 'FIXTURE-PROVIDER-ACCEPTANCE-1' } } };
      }
      assert.equal(request.credential, 'AgroFlow Service Key account');
      const response = await fetch(request.url, {
        method: request.method,
        headers: { 'Content-Type': 'application/json', 'X-AgroFlow-Service-Key': SERVICE_KEY },
        body: request.body === undefined ? undefined : JSON.stringify(request.body),
      });
      return { statusCode: response.status, body: await response.json() };
    },
    agent: async ({ text }) => {
      calls.agent.push(text);
      return '[SINTÉTICO] Tu consulta quedó registrada.';
    },
  });
}

function countRows(table) {
  // Lectura directa del archivo temporal con el SQLite embebido de Node.
  const { DatabaseSync } = process.getBuiltinModule('node:sqlite');
  const db = new DatabaseSync(dbPath, { readOnly: true });
  try {
    return db.prepare(`SELECT COUNT(*) AS total FROM ${table}`).get().total;
  } finally {
    db.close();
  }
}

async function main() {
  let api = startApi();
  await waitFor(`${API}/health`, api, 'La API');
  step(0, `API real en ${API} con SQLite temporal (migraciones aplicadas al arrancar)`);

  const created = await dashboard('/api/v1/drivers', {
    method: 'POST', body: JSON.stringify(DRIVER), headers: { 'Idempotency-Key': 'acceptance-driver-1' },
  });
  assert.equal(created.status, 201, 'alta del transportista');
  const driverId = (await created.json()).resourceId;
  step(1, 'transportista sintético registrado por POST /api/v1/drivers');

  const calls = { agent: [], evolution: [] };
  const first = await runWorkflow(calls);
  const ingest = first.outputs.get('Register Inbound Message');
  assert.equal(ingest.statusCode, 201);
  assert.equal(ingest.body.disposition, 'process');
  step(2, 'mensaje simulado desde ese número procesado por Chatbot.json');

  assert.equal(ingest.body.sender.recognized, true);
  assert.equal(ingest.body.sender.transportistaId, driverId);
  step(3, 'el backend identificó al transportista');

  assert.equal(countRows('ConversationMessages'), 2);
  assert.equal(countRows('Conversations'), 1);
  step(4, 'mensaje entrante y respuesta almacenados en SQLite');

  const repeat = await runWorkflow(calls);
  assert.equal(repeat.outputs.get('Register Inbound Message').body.disposition, 'duplicate_stop');
  assert.deepEqual(repeat.response.body, { status: 'duplicate' });
  assert.equal(calls.agent.length, 1, 'el duplicado no invoca al agente');
  assert.equal(calls.evolution.length, 1, 'el duplicado no envía otra respuesta');
  assert.equal(countRows('ConversationMessages'), 2);
  step(5, 'el mismo mensaje repetido devolvió duplicate_stop sin agente ni envío');

  const intent = first.outputs.get('Register Reply Intent');
  assert.equal(intent.statusCode, 201);
  assert.equal(intent.body.recipientAddress, JID);
  step(6, 'respuesta del chatbot registrada como intención antes del envío');

  const status = first.outputs.get('Update Send Status');
  assert.equal(status.statusCode, 200);
  assert.equal(status.body.sendStatus, 'ACCEPTED');
  step(7, 'resultado del envío simulado informado como ACCEPTED');

  const list = await (await dashboard('/api/v1/conversations')).json();
  const conversation = list.items.find(item => item.sender.transportistaId === driverId);
  assert.ok(conversation, 'la conversación aparece en el dashboard');
  assert.equal(conversation.sender.name, DRIVER.name);
  assert.ok(!conversation.remoteAddressMasked.includes('5493815550177'));
  const messages = await (await dashboard(`/api/v1/conversations/${conversation.id}/messages`)).json();
  assert.deepEqual(messages.items.map(m => m.direction), ['INBOUND', 'OUTBOUND']);
  assert.equal(messages.items[0].processingStatus, 'COMPLETED');
  assert.equal(messages.items[1].sendStatus, 'ACCEPTED');
  step(8, 'conversación y mensajes consultados por los endpoints del dashboard');

  const vite = start(process.execPath, [viteBin, '--port', String(VITE_PORT), '--strictPort', '--host', '127.0.0.1'], {
    cwd: join(root, 'frontend'),
    env: { ...process.env, VITE_DATA_SOURCE: 'http', VITE_API_URL: API, AGROFLOW_DASHBOARD_KEY: DASHBOARD_KEY },
  });
  const front = `http://127.0.0.1:${VITE_PORT}`;
  await waitFor(front, vite, 'El frontend');
  const viaProxy = await fetch(`${front}/api/v1/conversations`);
  assert.equal(viaProxy.status, 200, 'el proxy agrega la clave del dashboard');
  const proxied = await viaProxy.json();
  assert.ok(proxied.items.some(item => item.id === conversation.id));
  const channelViaProxy = await fetch(`${front}/api/v1/conversation-events`, { method: 'POST', body: '{}' });
  assert.equal(channelViaProxy.status, 404, 'el navegador no alcanza las rutas del canal');
  const foreignOrigin = await fetch(`${front}/api/v1/conversations`, { headers: { Origin: 'https://attacker.example' } });
  assert.equal(foreignOrigin.status, 404, 'otro origen no recibe la clave');
  const page = await (await fetch(front)).text();
  assert.ok(!page.includes(DASHBOARD_KEY), 'la clave no aparece en la página');
  await stop(vite);
  step(9, 'el frontend en modo http obtiene los datos reales por su proxy local (sin exponer la clave)');

  // Hallazgo P0: editar el WhatsApp de un transportista sembrado no debe impedir el reinicio.
  const seeded = (await (await dashboard('/api/v1/drivers')).json()).find(d => d.whatsApp === '+5493815550101');
  const edit = await dashboard(`/api/v1/drivers/${seeded.id}`, {
    method: 'PUT', headers: { 'Idempotency-Key': 'acceptance-edit-seed-1' },
    body: JSON.stringify({ name: seeded.name, dni: seeded.dni, whatsApp: '+5493815550111' }),
  });
  assert.equal(edit.status, 200, 'edición del WhatsApp sembrado');

  await stop(api);
  api = startApi();
  await waitFor(`${API}/health`, api, 'La API reiniciada');
  const driversAfter = await (await dashboard('/api/v1/drivers')).json();
  assert.equal(driversAfter.length, 6, 'cinco posiciones de la demo y el alta sintética, sin duplicados');
  const editedAfter = driversAfter.find(d => d.id === seeded.id);
  assert.equal(editedAfter.whatsApp, '+5493815550111');
  assert.equal(editedAfter.canRequestAppointments, true);
  assert.equal(driversAfter.find(d => d.id === driverId).canRequestAppointments, false,
    'un transportista sin camión asociado no puede solicitar turnos');
  const appointment = await fetch(`${API}/api/v1/appointments`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ carrierPhone: '+5493815550111', truckPlate: 'AF123BC', farmCode: 'FINCA-NORTE',
      cutAt: new Date(Date.now() - 3_600_000).toISOString(), estimatedLoadTons: 25 }),
  });
  assert.equal(appointment.status, 201, 'turno del transportista sembrado con asociación activa');
  step('10a', 'la API arrancó tras editar el WhatsApp sembrado; sin duplicados y con turno creado por su camión');
  const afterRestart = await (await dashboard(`/api/v1/conversations/${conversation.id}/messages`)).json();
  assert.deepEqual(afterRestart.items.map(m => m.id), messages.items.map(m => m.id));
  const replay = await runWorkflow(calls);
  assert.equal(replay.outputs.get('Register Inbound Message').body.disposition, 'duplicate_stop');
  assert.equal(calls.agent.length, 1);
  step(10, 'tras reiniciar la API los mensajes siguen almacenados y la repetición sigue detenida');

  await stop(api);
  console.log('\nAceptación local completa. Evolution, WhatsApp y el agente fueron simulados.');
}

try {
  await main();
} catch (error) {
  console.error(`✗ ${error.message}`);
  for (const child of children) {
    // Sólo las últimas líneas, sin claves: la API no registra secretos.
    console.error(child.output().split('\n').slice(-15).join('\n'));
  }
  process.exitCode = 1;
} finally {
  await Promise.all([...children].map(stop));
  rmSync(workdir, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
}
