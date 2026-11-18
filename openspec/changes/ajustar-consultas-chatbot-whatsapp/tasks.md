# Tareas: ajustar las consultas del chatbot de WhatsApp

## 1. Turnos activos (Backend/Contrato)

- [x] 1.1 `AppointmentQuery.ActiveOnly`, sin fecha por defecto en `EfAppointmentStore.ListAsync` cuando está activo. Verificación: `dotnet test --filter EfAppointmentStoreTests`.
- [x] 1.2 Parámetro `active` (sólo `true`) en `AppointmentsController` y OpenAPI. Verificación: `AppointmentsHttpTests`; Redocly válido.

## 2. Catálogo de fincas (Backend/Contrato)

- [x] 2.1 `FarmSearch` y `MasterDataService.SearchFarms` sobre `ListFincas`. Verificación: `FarmSearchTests`.
- [x] 2.2 `FarmsController` (LocalDemo) con la política `FarmCatalogRead` y ruta en OpenAPI. Verificación: `FarmsHttpTests`, `ConversationsHttpTests` (documento OpenAPI) y `MasterDataHttpTests` (sin ruta fuera de LocalDemo).

## 3. Workflow y CI (Integración)

- [x] 3.1 `Lookup Farms`, `active=true` en `Lookup Appointments By Phone` e instrucciones del agente, sin códigos de finca. Verificación: `node --test chatbot/tests/workflow.test.mjs`.
- [x] 3.2 Excepción acotada del validador para la lectura de fincas. Verificación: `node --test .github/scripts/tests/integrations.test.mjs` y `node .github/scripts/validate-integrations.mjs`.
- [x] 3.3 Aceptación local contra la API real. Verificación: `node chatbot/tests/acceptance.mjs`, pasos 11 y 12.
- [x] 3.4 `fullResponse: true` en herramientas de consulta (`Lookup Appointments By Phone` y `Lookup Farms`), fidelidad de arreglos vacíos en `n8n-simulator.mjs` y regla dura en `AI Agent1` para no rechazar solicitudes desde `Window Buffer Memory`. Verificación: `node --test chatbot/tests/workflow.test.mjs` y `node chatbot/tests/acceptance.mjs`.

## 4. Verificación en vivo y revisión (Equipo)

- [x] 4.1 Verificar contra la API en `LocalDemo` los endpoints `/api/v1/farms` y `/api/v1/appointments?phone=…&active=true` junto con la aceptación automatizada (`node chatbot/tests/acceptance.mjs`) y la conversación real WhatsApp → Evolution API → n8n → OpenAI → backend tras cancelación desde el panel. Verificación: PR #97 y PR de corrección de listas vacías/memoria conversacional.
- [x] 4.2 Aprobar para el alcance `LocalDemo` la ampliación acotada de la clave del canal a la lectura `GET /api/v1/farms` dentro de la resolución parcial de `OD-008` en `open-decisions.md`. Verificación: aprobación en la PR #97.
