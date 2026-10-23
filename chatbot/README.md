# Chatbot de AgroFlow

Workflow de n8n para conversar por WhatsApp mediante Evolution API: registra cada mensaje en AgroFlow API, reúne datos y consume la API; no asigna turnos, no decide identidad ni deduplica, y no mantiene estado operativo propio. El [contrato HTTP canónico](../docs/contracts/openapi.yaml) y la [especificación de integración](../openspec/specs/integracion-whatsapp-n8n/spec.md) son la referencia.

## Ejecutar e importar

Seguir la [guía central de demo local](../docs/development/local-demo.md) desde la raíz del monorepo. Importar [`workflows/Chatbot.json`](workflows/Chatbot.json) en n8n y seleccionar las credenciales de **esta instancia**:

- `OpenAI Chat Model`: credencial OpenAI (`OpenAI account`).
- `Send WhatsApp reply`: Header Auth (`Header Auth account`), encabezado `apikey` con la clave local de Evolution.
- Nodos del canal (`Register Inbound Message`, `Register Reply Intent`, `Update Send Status`, `Report Agent Failure`, `Report Reply Failure`): Header Auth (`AgroFlow Service Key account`), encabezado `X-AgroFlow-Service-Key` con el valor de `ChannelIntegration__ServiceKey` de la API. Nunca reutilizar la credencial de Evolution.

El archivo conserva referencias por nombre, sin IDs ni valores de credenciales; los nombres no garantizan la vinculación automática. Reasignar ambos nodos, guardar y publicar el workflow. Se importa inactivo, sin datos fijados ni historial de ejecuciones.

Requiere compatibilidad con **AI Agent 3.1**, **OpenAI Chat Model 1.3** y acceso al modelo **gpt-5.4-mini**. La versión fijada de n8n está en [`infra/env.example`](../infra/env.example). No sustituir el agente por el antiguo 2.1.

## Recorrido

```text
Webhook → Is Supported Text Message ──no──→ Acknowledge Ignored Event (200 ignored)
  └─sí→ Edit Fields → Register Inbound Message ──error/4xx/5xx──→ Reject Unpersisted Event (503)
          └→ Is New Message To Process ──no──→ Is Duplicate Message → Acknowledge Duplicate (200) · fin
               └─sí→ Respond to Webhook (200 accepted) → Is Registered Transportista
                       ├─sí→ AI Agent1 (+ herramientas de turnos) → Prepare Agent Reply ─┐
                       │        └─error→ Report Agent Failure (AGENT_ERROR)               │
                       └─no→ Prepare Unregistered Reply (plantilla fija, sin agente) ─────┤
                                                                                          ↓
                 Register Reply Intent → Can Send Reply ──no──→ Report Reply Failure (REPLY_NOT_SENT)
                   └─sí→ Send WhatsApp reply → Classify Send Result / Mark Send Unknown → Update Send Status
```

- **Persistir antes de todo**: el acuse al webhook sale sólo después de que la API registró el mensaje; si la API no responde o lo rechaza, el webhook recibe `503` y no se invoca al agente (OD-011, fail-closed).
- **Duplicados**: la API decide; ante `duplicate_stop` el workflow se detiene y no hay agente ni envío, aunque la autorización anterior haya vencido.
- **Remitentes**: el agente y sus herramientas sólo corren para transportistas registrados; el resto recibe una plantilla fija (DD-3, recomendación pendiente de aprobación).
- **Respuesta**: se envía exactamente el texto y destinatario de la intención registrada, sólo si está `PENDING_SEND` y su instancia coincide con `EVOLUTION_INSTANCE`.
- **Resultado**: `2xx` → `accepted` (con el identificador del proveedor sólo si Evolution lo devuelve en `key.id`), `4xx` → `failed`, `5xx` o timeout (10 s) → `unknown`. Nada se informa como entregado.
- **Datos mínimos**: no se envía `pushName`, el evento completo de Evolution ni metadatos de mensajes citados. El workflow no guarda datos de ejecuciones exitosas (`settings`), y Compose aplica la misma política a la instancia.
- **Mensajes admitidos**: texto simple (`conversation`) y texto extendido (`extendedTextMessage`: respuestas citadas o con vista previa de enlace), sólo de chats 1:1 `<dígitos>@s.whatsapp.net` o `<dígitos>@lid`. Se ignoran grupos, difusiones, estados, mensajes propios y multimedia.
- **LID**: un remitente `@lid` se registra sin teléfono y como no identificado, aunque Evolution adjunte un número alternativo (`remoteJidAlt`, `senderPn`): ese dato no viaja a la API ni vincula al transportista. Recibe la plantilla, que no afirma que el número no esté registrado.
- **Turnos**: el agente sólo corre para transportistas reconocidos, pero la API crea turnos sólo si además tienen una asociación activa con un camión; un transportista recién registrado sin asociación recibe `REFERENCE_NOT_FOUND`.

## Pruebas sin servicios en vivo

- `node --test chatbot/tests/workflow.test.mjs` (CI): ejecuta el JSON exportado con un [simulador](tests/n8n-simulator.mjs) y [fixtures sintéticos](tests/fixtures/) de Evolution, con API, Evolution y agente en memoria.
- `node chatbot/tests/acceptance.mjs`: aceptación local contra la API real con SQLite temporal y el proxy del frontend (requiere `dotnet build` y `npm ci`; no corre en CI).
- `node chatbot/tests/simulate-message.mjs --from +549… --text "…"`: simula un mensaje contra una API levantada para la demo.

El simulador interpreta sólo los nodos y expresiones de este workflow; no reemplaza una prueba en n8n. Evolution, WhatsApp y OpenAI siguen sin verificarse en vivo con esta versión; en particular, falta confirmar en vivo el envío de respuestas a direcciones `@lid` y la forma real de `extendedTextMessage` en la versión fijada de Evolution. El webhook Evolution → n8n no está autenticado: ver mitigaciones en la [guía de la demo](../docs/development/local-demo.md#webhook-evolution--n8n-sin-autenticación).

## Procedencia y alcance verificado

La infraestructura local y el workflow previo provienen de `agroflow-chatbot`, commit `e7f7fe7`. Este workflow incorpora además la actualización local probada y aportada por el usuario (Agent 3.1 / GPT-5.4-mini), que **no pertenece a ese commit**. Se retiraron el agente antiguo desconectado y los metadatos de instancia; se conservaron prompts, memoria, herramientas, conexiones activas y webhook.

Sólo se confirmó una respuesta al saludo. Las herramientas de alta, consulta y aviso `EN_CAMINO`, y el E2E de turnos, siguen pendientes de prueba; el remitente real usado no está registrado en el seed ficticio de la API.
