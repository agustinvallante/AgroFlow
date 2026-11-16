// Ensayo reproducible del guion de la demo LocalDemo (issue #111).
//
//   Escenario A: turno solicitado por el canal SIMULADO, consultado y llevado hasta FINALIZADO.
//   Escenario B: turno cancelado, cupo liberado y reutilizado por una nueva solicitud.
//   Tras cada ejecución se reinicia la API con el MISMO archivo SQLite y se compara todo.
//   Se ejecutan dos pasadas seguidas sobre esa base.
//
// REALES: AgroFlow API (perfil LocalDemo), SQLite temporal, proxy de Vite y el
//   workflow exportado Chatbot.json (interpretado por n8n-simulator.mjs).
// SIMULADOS: Evolution, WhatsApp, n8n (lo reemplaza el simulador) y el agente de IA
//   (un doble que elige las herramientas). Esto NO demuestra WhatsApp real (issue #110).
//
// Usa una base temporal nueva y claves aleatorias que nunca se imprimen; no toca
// bases existentes ni la precarga privada del equipo.
//
// Requisitos: `dotnet build backend/AgroFlow.Api` (Debug) y `npm ci` en frontend/.
// Uso: node chatbot/tests/demo-rehearsal.mjs [--sin-frontend] [--conservar]
//   --sin-frontend  omite el proxy de Vite (sólo API y SQLite).
//   --conservar     no borra la base temporal al terminar e imprime su ruta.
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { existsSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { simulate } from './n8n-simulator.mjs';

const flags = new Set(process.argv.slice(2));
const SIN_FRONTEND = flags.has('--sin-frontend');
const CONSERVAR = flags.has('--conservar');
const EJECUCIONES = 2;

const root = resolve(fileURLToPath(new URL('../..', import.meta.url)));
const apiDir = join(root, 'backend', 'AgroFlow.Api');
const apiDll = join(apiDir, 'bin', 'Debug', 'net8.0', 'AgroFlow.Api.dll');
const viteBin = join(root, 'frontend', 'node_modules', 'vite', 'bin', 'vite.js');
const workflow = JSON.parse(readFileSync(join(root, 'chatbot', 'workflows', 'Chatbot.json'), 'utf8'));
const fixture = JSON.parse(readFileSync(new URL('./fixtures/evolution-text-registered.json', import.meta.url), 'utf8'));

assert.ok(existsSync(apiDll), 'Compilá la API antes: dotnet build backend/AgroFlow.Api');
assert.ok(SIN_FRONTEND || existsSync(viteBin), 'Instalá el frontend antes (npm ci en frontend/) o usá --sin-frontend');

// Puertos distintos de acceptance.mjs (5099/5179) para poder tener ambos a mano.
const API = 'http://127.0.0.1:5098';
const FRONT = 'http://127.0.0.1:5178';
const INSTANCE = 'agroflow-demo';
const SERVICE_KEY = randomBytes(32).toString('hex');
const DASHBOARD_KEY = randomBytes(32).toString('hex');
const DASH = { 'X-AgroFlow-Dashboard-Key': DASHBOARD_KEY };

// Posiciones ficticias del seed (docs/development/local-demo-runbook.md), sin precarga privada.
const ACTOR_A = { phone: '+5493815550101', plate: 'AF123BC', farm: 'FINCA-NORTE' }; // demo-uno
const ACTOR_B = { phone: '+5493815550102', plate: 'AF456DE', farm: 'FINCA-SUR' }; // demo-dos
const SECUENCIA = ['EN_CAMINO', 'EN_ESPERA', 'INGRESADO', 'EN_DESCARGA', 'FINALIZADO'];

const workdir = mkdtempSync(join(tmpdir(), 'agroflow-rehearsal-'));
const dbPath = join(workdir, 'agroflow-rehearsal.db');
const children = new Set();
let ok = 0;
const step = (id, tag, text) => { ok += 1; console.log(`✓ ${id} [${tag}] ${text}`); };

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
      if ((await fetch(url)).ok) {
        // Si el puerto lo tenía otro proceso, el nuestro muere al no poder escuchar.
        await new Promise(done => setTimeout(done, 1500));
        if (child.exitCode !== null) throw new Error(`${label} no pudo iniciar (¿puerto ocupado por otro proceso?).`);
        return;
      }
    } catch { /* todavía no escucha */ }
    await new Promise(done => setTimeout(done, 500));
  }
  throw new Error(`${label} no respondió en ${url} (¿puerto ocupado?).`);
}

/** Evita medir otro proceso: si el puerto ya responde, ese servidor usa otra base. */
async function exigirPuertoLibre(url, etiqueta) {
  try { await fetch(url); } catch { return; }
  throw new Error(`${etiqueta} ya responde en ${url}: cerrá ese proceso (¿otro ensayo en curso?) y reintentá.`);
}

function startApi() {
  return start('dotnet', [apiDll], {
    cwd: apiDir,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: API,
      LocalDemo__Enabled: 'true',
      LocalDemo__TeamFile: join(workdir, 'sin-precarga.json'), // nunca la precarga privada real
      LocalDemo__WhatsAppInstance: INSTANCE,
      ConnectionStrings__AgroFlowDb: `Data Source=${dbPath};Default Timeout=5`,
      ChannelIntegration__ServiceKey: SERVICE_KEY,
      Dashboard__ApiKey: DASHBOARD_KEY,
      LogPath: '',
    },
  });
}

async function call(url, { method = 'GET', headers = {}, body } = {}) {
  const response = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json', ...headers },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  return { status: response.status, body: text ? JSON.parse(text) : null };
}

const crearTurno = actor => call(`${API}/api/v1/appointments`, {
  method: 'POST',
  body: {
    carrierPhone: actor.phone, truckPlate: actor.plate, farmCode: actor.farm,
    cutAt: new Date(Date.now() - 3_600_000).toISOString(), estimatedLoadTons: 25,
  },
});
const transicion = (id, newStatus) => call(`${API}/api/v1/appointments/${id}/transitions`, { method: 'POST', body: { newStatus } });
const detalle = (id, base = API) => call(`${base}/api/v1/appointments/${id}`);

/** Clave común: el turno sólo informa startAt/endAt de su ventana (no su id). */
const instante = ventana => Date.parse(ventana.startAt);
const sumarDia = fecha => new Date(Date.parse(`${fecha}T00:00:00Z`) + 86_400_000).toISOString().slice(0, 10);

/** Ventanas de hoy y mañana (la API decide "hoy"; la asignada puede caer mañana tras el cierre). */
async function leerVentanas(base = API, headers = DASH) {
  const hoy = await call(`${base}/api/v1/windows`, { headers });
  assert.equal(hoy.status, 200, 'GET /api/v1/windows');
  const fechaHoy = hoy.body[0]?.startAt.slice(0, 10) ?? new Date().toISOString().slice(0, 10);
  const manana = await call(`${base}/api/v1/windows?date=${sumarDia(fechaHoy)}`, { headers });
  assert.equal(manana.status, 200, 'GET /api/v1/windows?date=mañana');
  return new Map([...hoy.body, ...manana.body].map(w => [instante(w), w]));
}

function filas(sql) {
  const { DatabaseSync } = process.getBuiltinModule('node:sqlite');
  const db = new DatabaseSync(dbPath, { readOnly: true });
  try {
    return JSON.parse(JSON.stringify(db.prepare(sql).all()));
  } finally {
    db.close();
  }
}

/** Ejecuta Chatbot.json contra la API real; Evolution y el agente son dobles. */
function ejecutarCanal(jid, id, usarHerramientas) {
  const dobles = { agente: 0, evolution: 0 };
  return simulate(workflow, {
    body: { ...fixture, data: { ...fixture.data, key: { ...fixture.data.key, remoteJid: jid, id } } },
    env: { AGROFLOW_API_URL: API, EVOLUTION_INTERNAL_URL: 'http://evolution.test', EVOLUTION_INSTANCE: INSTANCE },
    http: async request => {
      if (!request.url.startsWith(API)) {
        dobles.evolution += 1;
        return { statusCode: 201, body: { key: { id: 'REHEARSAL-PROVIDER-1' } } };
      }
      const withKey = request.credential === 'AgroFlow Service Key account';
      assert.ok(withKey || new URL(request.url).pathname.startsWith('/api/v1/appointments'),
        `${request.node} debe usar la clave del servicio`);
      const response = await fetch(request.url, {
        method: request.method,
        headers: { 'Content-Type': 'application/json', ...(withKey ? { 'X-AgroFlow-Service-Key': SERVICE_KEY } : {}) },
        body: request.body === undefined ? undefined : JSON.stringify(request.body),
      });
      return { statusCode: response.status, body: await response.json() };
    },
    agent: async ({ callTool }) => {
      dobles.agente += 1;
      await usarHerramientas(callTool);
      return '[SINTÉTICO] Tu turno quedó registrado.';
    },
  }).then(resultado => ({ resultado, dobles }));
}

/** Estado completo que debe sobrevivir a un reinicio con el mismo archivo SQLite. */
async function capturar() {
  const conversaciones = (await call(`${API}/api/v1/conversations`, { headers: DASH })).body.items;
  const mensajes = {};
  for (const conversacion of conversaciones) {
    const lista = await call(`${API}/api/v1/conversations/${conversacion.id}/messages`, { headers: DASH });
    mensajes[conversacion.id] = lista.body.items.map(m => `${m.direction}:${m.id}`);
  }
  return {
    turnos: filas('SELECT Id, Status, VentanaId, CamionId FROM Turnos ORDER BY Id'),
    ventanas: [...(await leerVentanas()).values()].map(w => `${w.id}:${w.occupied}/${w.capacity}`).sort(),
    mensajes,
  };
}

/** Invariantes en SQLite: el cupo ocupado es exacto y no hay turnos activos duplicados por camión. */
function verificarInvariantes(id) {
  const desajustes = filas(`
    SELECT v.Id, v.Occupied AS ocupado,
      (SELECT COUNT(*) FROM Turnos t WHERE t.VentanaId = v.Id AND UPPER(t.Status) <> 'CANCELADO') AS turnos
    FROM Ventanas v WHERE v.Occupied <> turnos`);
  assert.deepEqual(desajustes, [], 'Ventanas.Occupied debe coincidir con los turnos no cancelados');
  const duplicados = filas('SELECT CamionId, COUNT(*) AS activos FROM Turnos WHERE IsActive = 1 GROUP BY CamionId HAVING activos > 1');
  assert.deepEqual(duplicados, [], 'un camión no puede tener dos turnos activos');
  step(id, 'REAL', 'SQLite: ocupación de cada ventana = turnos no cancelados; sin turnos activos duplicados por camión');
}

async function escenarioA(n) {
  const antes = await leerVentanas();
  const creado = {};
  const { dobles } = await ejecutarCanal(`${ACTOR_A.phone.slice(1)}@s.whatsapp.net`, `REHEARSAL-A-${n}`, async callTool => {
    Object.assign(creado, await callTool('Create Appointment', {
      truckPlate: ACTOR_A.plate, farmCode: ACTOR_A.farm,
      cutAt: new Date(Date.now() - 3_600_000).toISOString(), estimatedLoadTons: 25,
    }));
    creado.consulta = await callTool('Lookup Appointments By Phone');
  });
  assert.equal(creado.status, 'ASIGNADO', 'el canal debe crear el turno ASIGNADO');
  assert.equal(dobles.agente, 1);
  assert.deepEqual(creado.consulta.body.map(t => t.id), [creado.id], 'la consulta del canal encuentra el turno');
  step(`${n}.A1`, 'SIMULADO', `canal simulado (Chatbot.json + agente doble): turno ${creado.id} solicitado y consultado`);

  const ventana = creado.window;
  const ventanas = await leerVentanas();
  assert.equal(ventanas.get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied + 1, 'el alta ocupa un cupo');
  step(`${n}.A2`, 'REAL', `GET /api/v1/windows: la ventana ${ventana.startAt} pasó a ${ventanas.get(instante(ventana)).occupied}/${ventanas.get(instante(ventana)).capacity}`);

  assert.equal((await detalle(creado.id)).body.status, 'ASIGNADO');
  if (!SIN_FRONTEND) {
    const viaProxy = await detalle(creado.id, FRONT);
    assert.equal(viaProxy.status, 200);
    assert.equal(viaProxy.body.status, 'ASIGNADO');
    const ventanasProxy = await leerVentanas(FRONT, {}); // sin clave: la agrega el proxy del lado servidor
    assert.equal(ventanasProxy.get(instante(ventana)).occupied, ventanas.get(instante(ventana)).occupied);
    step(`${n}.A3`, 'REAL', 'el dashboard (proxy de Vite) lee el mismo turno y la misma ocupación, sin clave en el navegador');
  }

  assert.equal((await transicion(creado.id, 'INGRESADO')).status, 409, 'no se puede saltar estados');
  for (const siguiente of SECUENCIA) {
    const respuesta = await transicion(creado.id, siguiente);
    assert.equal(respuesta.status, 200, `transición a ${siguiente}`);
    assert.equal(respuesta.body.status, siguiente);
    assert.equal((await detalle(creado.id)).body.status, siguiente, `${siguiente} persistido`);
  }
  assert.equal((await transicion(creado.id, 'CANCELADO')).status, 409, 'FINALIZADO no admite cancelación');
  assert.equal((await transicion(creado.id, 'EN_CAMINO')).status, 409, 'FINALIZADO no admite retroceso');
  step(`${n}.A4`, 'REAL', 'ASIGNADO → … → FINALIZADO una transición por vez; el salto y los cambios posteriores responden 409');

  const final = await leerVentanas();
  assert.equal(final.get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied + 1, 'FINALIZADO conserva el cupo usado');
  step(`${n}.A5`, 'REAL', 'el turno FINALIZADO conserva su cupo en la ventana (sólo la cancelación libera)');
}

async function escenarioB(n) {
  const antes = await leerVentanas();
  const primero = await crearTurno(ACTOR_B);
  assert.equal(primero.status, 201);
  assert.equal(primero.body.status, 'ASIGNADO');
  const ventana = primero.body.window;
  assert.equal((await leerVentanas()).get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied + 1);
  step(`${n}.B1`, 'REAL', `turno ${primero.body.id} reservado en la ventana ${ventana.startAt}`);

  const cancelado = await transicion(primero.body.id, 'CANCELADO');
  assert.equal(cancelado.status, 200);
  assert.equal(cancelado.body.status, 'CANCELADO');
  assert.equal((await detalle(primero.body.id)).body.status, 'CANCELADO');
  assert.equal((await leerVentanas()).get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied, 'la cancelación libera el cupo');
  step(`${n}.B2`, 'REAL', 'cancelación por el flujo existente: el turno queda CANCELADO y la ventana vuelve a su ocupación inicial');

  const segundo = await crearTurno(ACTOR_B);
  assert.equal(segundo.status, 201, 'el camión admite un turno nuevo tras cancelar');
  assert.equal(instante(segundo.body.window), instante(ventana), 'la nueva solicitud reutiliza el cupo liberado');
  assert.equal((await leerVentanas()).get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied + 1);
  step(`${n}.B3`, 'REAL', `nueva solicitud ${segundo.body.id} asignada a la misma ventana: cupo liberado y reutilizado`);

  // Cierre del ensayo: deja el camión libre para repetir el guion sobre la misma base.
  assert.equal((await transicion(segundo.body.id, 'CANCELADO')).status, 200);
  assert.equal((await leerVentanas()).get(instante(ventana)).occupied, antes.get(instante(ventana)).occupied);
  step(`${n}.B4`, 'REAL', 'cierre del ensayo: segundo turno cancelado para poder repetir (los tres turnos de B quedan en la base)');
}

async function reiniciarYComparar(n, api) {
  const antes = await capturar();
  await stop(api);
  const nueva = startApi();
  await waitFor(`${API}/health`, nueva, 'La API reiniciada');
  const despues = await capturar();
  assert.deepEqual(despues, antes, 'turnos, estados, ocupación y conversaciones deben sobrevivir al reinicio');
  for (const turno of despues.turnos) {
    assert.equal((await detalle(turno.Id)).status, 200, 'el turno sigue disponible por la API');
  }
  step(`${n}.R`, 'REAL', `reinicio con el mismo SQLite: ${despues.turnos.length} turnos, ${despues.ventanas.length} ventanas y ${Object.keys(despues.mensajes).length} conversación(es) idénticos`);
  return nueva;
}

async function main() {
  await exigirPuertoLibre(`${API}/health`, "El puerto de la API");
  if (!SIN_FRONTEND) await exigirPuertoLibre(FRONT, "El puerto del frontend");
  let api = startApi();
  await waitFor(`${API}/health`, api, 'La API');
  console.log(`Base temporal de ensayo: ${dbPath}`);
  console.log('SIMULADOS: Evolution, WhatsApp, n8n (simulador) y agente de IA. REALES: API, SQLite, proxy de Vite.\n');
  if (!SIN_FRONTEND) {
    const vite = start(process.execPath, [viteBin, '--port', '5178', '--strictPort', '--host', '127.0.0.1'], {
      cwd: join(root, 'frontend'),
      env: { ...process.env, VITE_API_URL: API, AGROFLOW_DASHBOARD_KEY: DASHBOARD_KEY },
    });
    await waitFor(FRONT, vite, 'El frontend');
  }
  for (let n = 1; n <= EJECUCIONES; n++) {
    console.log(`── Ejecución ${n} de ${EJECUCIONES} ──`);
    await escenarioA(n);
    await escenarioB(n);
    verificarInvariantes(`${n}.I`);
    api = await reiniciarYComparar(n, api);
  }
  await stop(api);
  console.log(`\nEnsayo completo: ${ok} comprobaciones, ${EJECUCIONES} ejecuciones, reinicio sin pérdida.`);
  console.log('El canal (Evolution/WhatsApp/agente) fue simulado: no prueba el WhatsApp real.');
}

try {
  await main();
} catch (error) {
  console.error(`✗ ${error.message}`);
  for (const child of children) console.error(child.output().split('\n').slice(-15).join('\n'));
  process.exitCode = 1;
} finally {
  await Promise.all([...children].map(stop));
  if (CONSERVAR) console.log(`Base conservada en ${dbPath}`);
  else rmSync(workdir, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
}
