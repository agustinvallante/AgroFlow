# Tareas: persistencia de conversaciones de WhatsApp

Cada etapa es un PR pequeño sobre `feat/whatsapp-persistence` o una rama derivada. Una etapa no comienza hasta que sus decisiones bloqueantes estén aprobadas y registradas en el [registro de decisiones](../../../docs/planning/open-decisions.md). Cada PR actualiza contrato, pruebas y documentación juntos (AGENTS.md).

## 0. Diseño y contratos (Docs)

- [x] 0.1 Crear propuesta, diseño, tareas y deltas de este cambio. Verificación: `openspec validate persistir-conversaciones-whatsapp --strict`.
- [ ] 0.2 Revisar con el equipo y aprobar o modificar OD-006, OD-008, OD-009, OD-010, OD-011, OD-013 y DD-1 a DD-4. Verificación: estados actualizados y enlazados en `open-decisions.md`.
- [ ] 0.3 Corregir en la propuesta del compañero la frase que ubica el workflow fuera de `main` (#79 ya integrada). Verificación: enlaces locales válidos.
- [ ] 0.4 Ajustar este cambio a las decisiones aprobadas. Verificación: OpenSpec estricto en verde.

## 1. Fundación persistente de datos maestros (Backend/Persistencia)

Bloqueantes: OD-006 (encabezado `Idempotency-Key`), B02 (columnas de tiempo).

- [x] 1.1 Mapear `AuditEvent` e `IdempotencyRecord` en `AgroFlowDbContext` y crear la migración `AddMasterDataFoundationTables`. Verificación: prueba de migración sobre una base vacía y otra con turnos previos.
- [x] 1.2 Actualizar `EfModelAndMigrationVerificationTests` y `EfMasterDataUnitOfWorkTests` para el nuevo modelo. Verificación: `dotnet test --filter EfModel|EfMasterData`.

Aceptación: `MasterDataService.CreateDriver` persiste el transportista, el registro de idempotencia y el evento de auditoría en una transacción contra SQLite migrada.

## 2. API de transportistas (Backend/Contrato)

Bloqueantes: B00, OD-013 (formato E.164), DD-1.

- [x] 2.1 `LocalDemoTenantContext` y registro en DI de `MasterDataService`, `EfMasterDataUnitOfWork` e `ITenantContext` por perfil. Verificación: pruebas de resolución por perfil.
- [x] 2.2 Validar el WhatsApp como E.164 y crear el índice único global (`AddGlobalWhatsAppUniqueIndex`) con verificación previa de duplicados. Verificación: pruebas de dominio, migración y conflicto entre ingenios.
- [x] 2.3 Quitar `[NonController]`, alinear los códigos de error y publicar las rutas `drivers` en OpenAPI. Verificación: `MasterDataHttpTests` y `BackendSurfaceTests` actualizados; Redocly válido. *Implementado con `DriversController` (sólo transportistas, sobre `MasterDataService`); `MasterDataController` sigue `[NonController]` para no publicar camiones, fincas ni asociaciones. Las rutas exigen `DashboardKey`.*

- [ ] 2.4 **Bloqueada por OD-002/OD-007/OD-013 (gestión de asociaciones pendiente):** publicar la asociación transportista–camión. Sin ella, un transportista registrado por la API es reconocido por el canal pero `POST /api/v1/appointments` responde `404 REFERENCE_NOT_FOUND`. Alternativa propuesta para aprobar: publicar `CreateAssociation`/`SetAssociationActive` y el alta de camiones de `MasterDataService` (ya implementados y probados, con la política `AssociationManagement` hoy cerrada). Mientras tanto, `Driver` expone en sólo lectura `activeTruckPlates` y `canRequestAppointments`. Verificación actual: `DriverLifecycleHttpTests`.
- [x] 2.5 Identidad estable del seed (`SeedRecords`, migración `AddSeedRecords`): editar el WhatsApp, DNI o patente de una fila sembrada ya no hace que el reinicio la recree ni la sobrescriba; las bases previas adoptan sus filas una sola vez. Verificación: `SeedIdentityTests`, `DriverLifecycleHttpTests`, `LocalDemoDatabaseTests`.

Aceptación: con la API en `LocalDemo`, `POST /api/v1/drivers` registra un número y `POST /api/v1/appointments` con ese `carrierPhone` crea un turno sin tocar el seed. Un número duplicado devuelve `409`. *Parcial: el turno requiere además la asociación de la tarea 2.4.*

## 3. Canal y autenticación de servicio (Backend/Seguridad)

Bloqueantes: OD-008, DD-2.

- [x] 3.1 Entidad `ChannelInstance`, migración `AddChannelInstances` y seed desde `LocalDemo:WhatsAppInstance`. Verificación: pruebas de seed idempotente.
- [x] 3.2 Esquema `ServiceKey` y política `ChannelIngest` con comparación en tiempo constante y arranque fallido si falta la clave. Verificación: pruebas HTTP de `401` y de arranque. *Desviación a confirmar: una clave ausente deja las rutas en `401` sin abortar, para conservar el arranque de la demo de turnos desde un clon limpio; una clave corta o compartida sí aborta. Se agregó `DashboardKey` para que conversaciones y transportistas tampoco sean anónimos en LocalDemo (reemplaza la recomendación DD-1).*

Aceptación: ninguna ruta del canal responde sin clave válida en ningún perfil; la instancia determina el ingenio.

## 4. Ingesta de mensajes entrantes (Backend/Persistencia/Contrato)

Bloqueantes: OD-006, OD-009, etapa 3.

- [x] 4.1 Entidades `Conversation`, `ConversationMessage` y `MessageProcessing` (según la opción aprobada) con sus invariantes. Verificación: pruebas de dominio.
- [x] 4.2 Migración `AddConversations` con índices filtrados y checks. Verificación: prueba de modelo y migración.
- [x] 4.3 `IConversationStore`, `EfConversationStore` y `ConversationService.IngestAsync` con inserción atómica, relectura ante unicidad y reconocimiento del remitente. Verificación: pruebas de servicio y de concurrencia en SQLite.
- [x] 4.4 `POST /api/v1/conversation-events` y su contrato OpenAPI. Verificación: pruebas HTTP de `201`, `200 duplicate_stop`, `409`, `400` y `403`.

Aceptación: un mensaje repetido, aun en paralelo, deja una sola fila y una sola autorización; un número registrado se reconoce y un LID no.

## 5. Respuestas y procesamiento (Backend/Contrato)

Bloqueantes: OD-010, OD-006, etapa 4.

- [x] 5.1 `POST /api/v1/conversation-reply-intents` con validación del token y unicidad por mensaje entrante. Verificación: pruebas HTTP de repetición, conflicto y token inválido.
- [x] 5.2 `PATCH …/send-status` con la máquina de estados y cierre del procesamiento. Verificación: pruebas de transiciones válidas e inválidas.
- [x] 5.3 `PATCH /api/v1/conversation-events/{id}/processing` para cierres sin respuesta. Verificación: pruebas del catálogo de códigos.

Aceptación: no puede registrarse una respuesta sin una autorización vigente; un envío incierto queda `UNKNOWN` y nunca se presenta como entregado.

## 6. Consultas del dashboard (Backend/Contrato)

Bloqueantes: OD-009 (lectores), DD-1, etapa 4.

- [x] 6.1 `GET /api/v1/conversations`, `GET …/{id}` y `GET …/{id}/messages` con cursor opaco. Verificación: pruebas HTTP con dos ingenios, cursor manipulado e inserciones entre páginas.

Aceptación: el dashboard obtiene solo las conversaciones del ingenio, ordenadas y paginadas sin duplicados ni omisiones.

## 7. Workflow de n8n y CI (Integración)

Bloqueantes: OD-008, OD-010, OD-011, etapas 3 a 5.

- [x] 7.1 Ampliar `validate-integrations.mjs` y sus pruebas para las rutas del canal y la credencial de servicio. Verificación: `node --test .github/scripts/tests/integrations.test.mjs`.
- [x] 7.2 Fixtures sintéticos de envelopes de Evolution y copia aislada del workflow con agente y proveedor simulados. Verificación: los casos de la sección 6 de la propuesta, ejecutados sin OpenAI ni Evolution reales.
- [x] 7.3 Insertar los nodos de validación, ingesta, enrutamiento, intención y estado en `Chatbot.json`, y mover el acuse según OD-011. Verificación: `validate-integrations.mjs` en verde.

Aceptación: una repetición no invoca al agente ni a las herramientas; un remitente no reconocido recibe la plantilla sin herramientas.

*Se agregaron texto extendido (`extendedTextMessage`), filtro de chats 1:1 (teléfono o LID) y la política de no guardar ejecuciones exitosas en n8n. El webhook Evolution → n8n sigue sin autenticación (mitigaciones en `docs/development/local-demo.md`). Verificado con el simulador del JSON exportado y fixtures sintéticos (`chatbot/tests/`), no en n8n en vivo. El acuse se movió después de la ingesta (recomendación de OD-011, pendiente de aprobación).*

## 8. Dashboard (Frontend)

Bloqueantes: etapas 2 y 6.

- [x] 8.1 `HttpConversacionRepository` con su DTO y mapper, y ajuste de la entidad `Conversacion`. Verificación: Vitest.
- [x] 8.2 `HttpTransportistaRepository` alineado con el modelo de la API. Verificación: Vitest.
- [x] 8.3 Composición en `container.ts` para el modo HTTP. Verificación: typecheck, lint, build y prueba manual en la demo.

Aceptación: en modo `http`, las vistas Chatbot y Transportistas muestran datos persistidos y dejan de marcarse como demostración.

*La clave del dashboard la agrega el proxy de Vite del lado servidor (`frontend/server/dashboardProxy.ts`); nunca llega al navegador. Métricas y plantillas del chatbot siguen marcadas como demostración porque la API no las publica.*

## 9. Evidencia y cierre

- [ ] 9.1 E2E con un número de prueba consentido, registrado por la API. *Avance: aceptación local automatizada (`node chatbot/tests/acceptance.mjs`) contra la API real, SQLite temporal y el proxy del frontend, con Evolution, WhatsApp y el agente simulados. Falta el recorrido en vivo con n8n y Evolution en Docker y un número consentido.* Verificación: evidencia según la guía de aceptación del MVP, sin capturas de datos reales versionadas.
- [ ] 9.2 Consolidar los deltas en `openspec/specs/` y archivar el cambio. Verificación: `openspec validate --all --strict` y `--archived`.
