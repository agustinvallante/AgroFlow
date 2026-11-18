// Simula un mensaje entrante de WhatsApp contra una AgroFlow API ya levantada,
// ejecutando Chatbot.json con el simulador. La API es real; Evolution y el
// agente son dobles (no se envía nada a WhatsApp ni se llama a OpenAI).
//
//   $env:AGROFLOW_SERVICE_KEY = "<la misma clave que ChannelIntegration__ServiceKey>"
//   node chatbot/tests/simulate-message.mjs --from +5493815550155 --text "Hola"
//
// Opciones: --id <providerMessageId> (repetirlo simula un duplicado),
// --reply <texto que devolvería el agente>. Variables: AGROFLOW_API_URL
// (por defecto http://localhost:5000) y EVOLUTION_INSTANCE (agroflow-demo).
import { randomUUID } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { simulate } from './n8n-simulator.mjs';

const { values } = parseArgs({
  options: {
    from: { type: 'string' },
    text: { type: 'string' },
    id: { type: 'string' },
    reply: { type: 'string', default: '[SIMULADO] Recibimos tu mensaje. Este texto reemplaza la respuesta del agente.' },
  },
});

const api = (process.env.AGROFLOW_API_URL ?? 'http://localhost:5000').replace(/\/+$/, '');
const instance = process.env.EVOLUTION_INSTANCE ?? 'agroflow-demo';
const serviceKey = process.env.AGROFLOW_SERVICE_KEY;
if (!serviceKey || !values.from || !values.text) {
  console.error('Uso: AGROFLOW_SERVICE_KEY=<clave> node chatbot/tests/simulate-message.mjs --from +549… --text "…"');
  process.exit(2);
}
if (!/^\+[1-9][0-9]{7,14}$/.test(values.from)) {
  console.error('--from debe ser un número E.164 ficticio o de prueba consentida, por ejemplo +5493815550155.');
  process.exit(2);
}

const workflow = JSON.parse(readFileSync(new URL('../workflows/Chatbot.json', import.meta.url), 'utf8'));
const providerMessageId = values.id ?? `SIMULATED-${randomUUID()}`;
const body = {
  event: 'messages.upsert',
  instance,
  data: {
    key: { remoteJid: `${values.from.slice(1)}@s.whatsapp.net`, fromMe: false, id: providerMessageId },
    message: { conversation: values.text },
    messageType: 'conversation',
    messageTimestamp: Math.floor(Date.now() / 1000),
  },
};

const result = await simulate(workflow, {
  body,
  env: { AGROFLOW_API_URL: api, EVOLUTION_INTERNAL_URL: 'http://evolution.simulated', EVOLUTION_INSTANCE: instance },
  http: async request => {
    if (!request.url.startsWith(api)) {
      console.log(`· Evolution simulado: se habría enviado ${JSON.stringify(request.body.text)}`);
      return { statusCode: 201, body: { key: { id: `SIMULATED-OUT-${randomUUID()}` } } };
    }
    const response = await fetch(request.url, {
      method: request.method,
      headers: { 'Content-Type': 'application/json', 'X-AgroFlow-Service-Key': serviceKey },
      body: request.body === undefined ? undefined : JSON.stringify(request.body),
    });
    return { statusCode: response.status, body: await response.json().catch(() => ({})) };
  },
  agent: async () => values.reply,
});

const ingest = result.outputs.get('Register Inbound Message');
console.log(`· providerMessageId: ${providerMessageId}`);
console.log(`· Ingesta: HTTP ${ingest?.statusCode ?? 'sin respuesta'} ${ingest?.body?.disposition ?? ingest?.body?.code ?? ''}`);
if (ingest?.body?.sender) console.log(`· Remitente reconocido: ${ingest.body.sender.recognized ? 'sí' : 'no'}`);
const status = result.outputs.get('Update Send Status');
if (status) console.log(`· Estado de envío registrado: ${status.body?.sendStatus ?? status.statusCode}`);
console.log(`· Respuesta al webhook: ${result.response?.status} ${JSON.stringify(result.response?.body)}`);
if (!ingest || ingest.statusCode >= 400) process.exitCode = 1;
