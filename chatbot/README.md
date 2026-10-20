# Chatbot de AgroFlow

Workflow de n8n para conversar por WhatsApp mediante Evolution API: reúne datos y consume la API; no asigna turnos ni mantiene estado operativo propio. El [contrato HTTP canónico](../docs/contracts/openapi.yaml) y la [especificación de integración](../openspec/specs/integracion-whatsapp-n8n/spec.md) son la referencia.

## Ejecutar e importar

Seguir la [guía central de demo local](../docs/development/local-demo.md) desde la raíz del monorepo. Importar [`workflows/Chatbot.json`](workflows/Chatbot.json) en n8n y seleccionar las credenciales de **esta instancia**:

- `OpenAI Chat Model`: credencial OpenAI (`OpenAI account`).
- `Send WhatsApp reply`: Header Auth (`Header Auth account`), encabezado `apikey` con la clave local de Evolution.

El archivo conserva referencias por nombre, sin IDs ni valores de credenciales; los nombres no garantizan la vinculación automática. Reasignar ambos nodos, guardar y publicar el workflow. Se importa inactivo, sin datos fijados ni historial de ejecuciones.

Requiere compatibilidad con **AI Agent 3.1**, **OpenAI Chat Model 1.3** y acceso al modelo **gpt-5.4-mini**. La versión fijada de n8n está en [`infra/env.example`](../infra/env.example). No sustituir el agente por el antiguo 2.1.

## Procedencia y alcance verificado

La infraestructura local y el workflow previo provienen de `agroflow-chatbot`, commit `e7f7fe7`. Este workflow incorpora además la actualización local probada y aportada por el usuario (Agent 3.1 / GPT-5.4-mini), que **no pertenece a ese commit**. Se retiraron el agente antiguo desconectado y los metadatos de instancia; se conservaron prompts, memoria, herramientas, conexiones activas y webhook.

Sólo se confirmó una respuesta al saludo. Las herramientas de alta, consulta y aviso `EN_CAMINO`, y el E2E de turnos, siguen pendientes de prueba; el remitente real usado no está registrado en el seed ficticio de la API.
