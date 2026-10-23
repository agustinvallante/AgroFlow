# Diseño: persistencia de conversaciones de WhatsApp

> **Estado: diseño propuesto, no aprobado.** Las secciones marcadas como *recomendación* dependen de decisiones abiertas. Ningún nombre de tabla, campo o ruta de este documento es contrato hasta que la etapa correspondiente se apruebe y se publique en [OpenAPI](../../../docs/contracts/openapi.yaml).

## 1. Punto de partida verificado

| Elemento | Estado en `main` (9060190) |
|---|---|
| `Transportista` | Tabla migrada con `WhatsApp` y `NormalizedWhatsApp` (solo dígitos); índice único `(IngenioId, NormalizedWhatsApp)`. `CreatedAtUtc`/`InactivatedAtUtc` ignorados por EF. |
| Resolución por teléfono | Implícita dentro de `EfAppointmentStore`: crea y lista turnos buscando `NormalizedWhatsApp` en todos los ingenios. |
| Datos maestros (PR #47) | `MasterDataService`, `EfMasterDataUnitOfWork` y `MasterDataController` escritos; el controlador es `[NonController]`, no hay registro en DI y `AuditEvent`/`IdempotencyRecord` no están en el modelo EF, por lo que toda mutación falla. |
| Autenticación | Perfil `LocalDemo`: sin autenticación. Fuera de él: Identity + JWT sin claim `ingenioId`, por lo que `HttpTenantContext` no puede resolver el ingenio (B03). |
| n8n | `Webhook → Respond to Webhook → Filter → Edit Fields → AI Agent1 → Send WhatsApp reply`. Herramientas sin credencial contra `/api/v1/appointments`. |
| CI | `validate-integrations.mjs` solo admite rutas `/api/v1/appointments` y `sendText` de Evolution en el workflow. |
| Conversaciones | No existen en backend, contrato ni specs; el frontend usa un repositorio mock. |

## 2. Principios

1. **Backend autoritativo** (ADR-003): n8n y el frontend no deciden identidad, deduplicación ni estado.
2. **Un solo `DbContext` y un solo archivo SQLite** (ADR-006): todo se agrega a `AgroFlowDbContext` y a su cadena de migraciones. No se reutilizan PostgreSQL ni Redis de Evolution ni el almacenamiento de n8n.
3. **Reutilizar antes que duplicar**: `Transportista` sigue siendo la única identidad del transportista; `MasterDataService` se habilita, no se reescribe.
4. **Idempotencia natural**: la unicidad de los mensajes descansa en índices únicos de base, no en lecturas previas sin protección.
5. **Datos mínimos**: texto, identificadores opacos y tiempos. No se guardan `pushName`, payloads completos, prompts ni resultados de herramientas.
6. **Patrón de capas existente**: entidades en `AgroFlow.Domain`, interfaz de almacenamiento y servicio en `AgroFlow.Application` (como `IAppointmentStore`/`AppointmentService`), adaptador EF en `AgroFlow.Data`, controladores y DTO en `AgroFlow.Api`.

## 3. Modelo de entidades

Todas heredan de `EntityBase` (`Guid Id`), usan constructores privados para EF y setters privados, y validan con `MasterDataValidation`. Los tiempos se guardan como `DateTime` UTC: el proveedor SQLite no traduce comparaciones de `DateTimeOffset`, la misma razón que motivó `Ventana.StartAtUtc`.

### 3.1 `Transportista` (existente)

| Aspecto | Detalle |
|---|---|
| Atributos | `IngenioId`, `Name`, `DNI`, `NormalizedDni`, `WhatsApp`, `NormalizedWhatsApp`, `IsActive`, `CreatedAtUtc`, `InactivatedAtUtc`. |
| Responsabilidad | Identidad del transportista. Es la única fuente para reconocer al remitente; ninguna entidad nueva copia su nombre ni su DNI. |
| Relaciones | 1–N `TransportistaCamion`, 1–N `Turno`, 0..1–N `Conversation` (nueva). |
| Restricciones actuales | Único por `(IngenioId, NormalizedWhatsApp)`; el servicio ya rechaza duplicados globales de DNI y WhatsApp. |
| Cambios propuestos | (a) Índice único global sobre `NormalizedWhatsApp`, alineado con la spec canónica "únicos dentro del sistema" y requerido para que un número identifique un solo transportista. (b) Exigir formato internacional al registrar (ver delta de `datos-maestros`). (c) Migrar `CreatedAtUtc`/`InactivatedAtUtc` cuando B02 lo apruebe; no es bloqueante para identificar. |
| Persistencia | Tabla `Transportistas` existente; migración aditiva de índice. Antes de crear el índice global, la migración debe detectar duplicados entre ingenios y abortar con un mensaje claro en lugar de borrar datos. |

### 3.2 `ChannelInstance` (nueva, de soporte)

La propuesta exige validar la instancia recibida contra instancias habilitadas y no aceptar el ingenio desde el cuerpo. Se necesita un vínculo persistido instancia → ingenio.

| Aspecto | Detalle |
|---|---|
| Atributos | `IngenioId`, `Provider` (`EVOLUTION`), `InstanceName` (máx. 100), `IsActive`. |
| Restricciones | Único `(Provider, InstanceName)`. |
| Responsabilidad | Resolver el ingenio de un evento del canal y rechazar instancias desconocidas o inactivas. |
| Persistencia | Tabla `ChannelInstances`. El seed de demo crea una fila para el ingenio sembrado con el nombre configurado en `LocalDemo:WhatsAppInstance` (valor de ejemplo `agroflow-demo`, igual a `EVOLUTION_INSTANCE`). |
| Alternativa | Mapeo solo por configuración. Se descarta como recomendación porque obliga a conocer el `Guid` del ingenio, que se genera en el seed. |

### 3.3 `Conversation`

| Aspecto | Detalle |
|---|---|
| Atributos | `IngenioId`, `ChannelInstanceId`, `RemoteAddress` (JID completo, máx. 100), `AddressKind` (`PHONE`, `LID`, `OTHER`), `NormalizedPhone?` (solo si `AddressKind = PHONE`), `TransportistaId?`, `CreatedAtUtc`, `LastMessageAtUtc`. |
| Relaciones | N–1 `Ingenio`, N–1 `ChannelInstance`, N–0..1 `Transportista` (FK restrictiva), 1–N `ConversationMessage`. |
| Restricciones | Único `(ChannelInstanceId, RemoteAddress)`, que equivale al ámbito + proveedor + instancia + dirección de la propuesta. `IngenioId` debe coincidir con el de la instancia y, si existe, con el del transportista. |
| Índices | `(IngenioId, LastMessageAtUtc, Id)` para el listado; `(TransportistaId)`. |
| Responsabilidad | Agrupar mensajes de una dirección remota dentro del ámbito autorizado. Una dirección no prueba identidad: `TransportistaId` solo se asigna cuando el número normalizado coincide con un transportista **activo del mismo ingenio**. |
| Reglas | El vínculo se evalúa en cada mensaje entrante. Si una conversación no reconocida pasa a reconocerse porque se registró el número, se vincula sin reescribir los mensajes previos. Si el transportista se inhabilita, el vínculo histórico se conserva, pero el remitente se informa como no reconocido. Las direcciones `@lid` nunca derivan un teléfono. |
| Persistencia | Tabla `Conversations`. Creación con "insertar; ante violación de unicidad, releer" dentro de la transacción de ingesta. `LastMessageAtUtc` se actualiza con `ExecuteUpdate` condicional (`WHERE LastMessageAtUtc < @nuevo`). |

### 3.4 `ConversationMessage`

| Aspecto | Detalle |
|---|---|
| Atributos | `ConversationId`, `IngenioId` (desnormalizado, como el resto de las entidades, para filtrar sin join), `Direction` (`INBOUND`, `OUTBOUND`), `ProviderMessageId?` (máx. 128), `ContentType` (`TEXT`), `Text` (máx. 4096), `ProviderTimestampUtc?`, `PersistedAtUtc`, `InReplyToMessageId?`, `SendStatus?` (`PENDING_SEND`, `ACCEPTED`, `FAILED`, `UNKNOWN`), `SendStatusUpdatedAtUtc?`. |
| Relaciones | N–1 `Conversation`; autorreferencia `InReplyToMessageId` hacia un mensaje entrante de la misma conversación; 1–0..1 `MessageProcessing`. |
| Restricciones | Único `(ConversationId, Direction, ProviderMessageId)` filtrado a `ProviderMessageId IS NOT NULL`. Único `(InReplyToMessageId)` filtrado a salientes: una intención por mensaje entrante. Checks: entrante ⇒ `ProviderMessageId` no nulo y `SendStatus` nulo; saliente ⇒ `SendStatus` no nulo e `InReplyToMessageId` no nulo. |
| Índices | `(ConversationId, PersistedAtUtc, Id)` para paginación estable. |
| Responsabilidad | Registro inmutable del contenido. Solo `SendStatus`, `SendStatusUpdatedAtUtc` y el `ProviderMessageId` saliente cambian, y solo por transiciones válidas. |
| Orden | El orden de lectura es `PersistedAtUtc, Id`, asignado por el backend con `TimeProvider`. `ProviderTimestampUtc` se muestra, pero no ordena. |
| Persistencia | Tabla `ConversationMessages`. El `ProviderMessageId` saliente solo se acepta desde la evidencia del proveedor informada por n8n, nunca desde el agente ni copiando el entrante. |
| Diferido | `AppointmentId?` de la propuesta queda fuera de la primera iteración: exige verificar la pertenencia de un turno confirmado y no aporta a los dos requerimientos. |

### 3.5 `MessageProcessing`: evaluación

La propuesta distingue tres duplicaciones: filas repetidas, procesamientos concurrentes y envíos externos repetidos.

| Problema | ¿Lo resuelve el índice único? | ¿Aporta `MessageProcessing`? |
|---|---|---|
| Dos webhooks con el mismo mensaje | Sí: solo un `INSERT` gana; el otro recibe `duplicate_stop`. | No es necesario. |
| Dos ejecuciones concurrentes del agente | Sí, si la autorización se otorga en la misma transacción del `INSERT`. | Hace explícita la autorización, con un token que identifica a su titular. |
| Una segunda ejecución que intenta registrar una respuesta | No. | Sí: la intención de respuesta exige el token vigente del titular. |
| Caída entre la ingesta y la respuesta | No: el reintento recibe `duplicate_stop` y el mensaje queda sin respuesta, sin que nadie lo vea. | Sí: estado `PROCESSING` con vencimiento visible en el dashboard como "requiere revisión", sin reprocesar automáticamente. |
| Errores del agente sin respuesta | No hay dónde registrarlos. | Sí: `FAILED` con un código acotado. |

**Opciones**

- **A. Tabla separada 1:1 (recomendada).** `InboundMessageId` (único), `Status` (`PROCESSING`, `COMPLETED`, `FAILED`), `ClaimToken`, `ClaimExpiresAtUtc`, `CompletedAtUtc?`, `LastErrorCode?` (máx. 64, catálogo cerrado). Mantiene inmutable el registro del mensaje y separa el estado operativo, como propone el modelo conceptual.
- **B. Columnas en `ConversationMessage`.** Menos tablas, pero mezcla contenido inmutable con estado mutable y deja columnas nulas en los mensajes salientes.
- **C. Diferirla.** Alcanza para la demo feliz, pero deja invisibles los mensajes sin respuesta y no protege el registro de la respuesta.

**Recomendación: opción A, mínima.**

- La autorización se crea en la misma transacción que el mensaje entrante y su token se devuelve una sola vez a quien ganó la inserción.
- El estado `pending` de la propuesta no se materializa: todo mensaje admitido nace autorizado. "Vencido" es un estado derivado (`PROCESSING` y `ClaimExpiresAtUtc < ahora`), no persistido.
- No hay renovación ni reclamo de autorizaciones vencidas en esta iteración. Vencer **no autoriza** reprocesar, tal como exige la propuesta.
- `COMPLETED` significa "n8n terminó su recorrido e informó el resultado del envío". No implica entrega de WhatsApp ni éxito de un turno.

La elección final pertenece a OD-006.

### 3.6 Relaciones

```text
Ingenio 1─N ChannelInstance 1─N Conversation N─0..1 Transportista
                                     │
                                     1─N ConversationMessage (INBOUND) 1─0..1 MessageProcessing
                                              └─0..1 ConversationMessage (OUTBOUND, InReplyTo)
```

### 3.7 Entidades existentes que no se reutilizan para esto

- **`AuditEvent`**: la auditoría registra acciones de actores; el historial conversacional es contenido de canal (OD-009 las distingue). Se migra solo porque `MasterDataService` la escribe.
- **`IdempotencyRecord`**: su clave es actor + operación + encabezado `Idempotency-Key`. Para los mensajes, la clave natural del proveedor es más precisa y no exige que n8n invente claves. Se migra solo para las mutaciones de transportistas.

## 4. Reutilización

| Recurso | Uso |
|---|---|
| `Transportista`, `MasterDataValidation` | Normalización y reconocimiento del remitente. |
| `MasterDataService`, `EfMasterDataUnitOfWork`, `MasterDataController` | Registro y consulta de transportistas: se habilitan (quitar `[NonController]`, registrar en DI, migrar `AuditEvent`/`IdempotencyRecord`) en lugar de escribir un CRUD nuevo. |
| `AgroFlowDbContext` y migraciones `Migrations/Appointments` | Único contexto y cadena de migraciones. Las nuevas configuraciones se agregan en `OnModelCreating`. |
| Detección de violaciones de unicidad SQLite de `EfMasterDataUnitOfWork` | Se extrae a un helper compartido en `AgroFlow.Data` para la ingesta. |
| `AppointmentProblemDetails` y el estilo de validación estricta de `AppointmentsController` | Errores `application/problem+json` con `code` estable; rechazo de propiedades desconocidas o repetidas. |
| `ITenantContext` | Sigue siendo la abstracción de ingenio para los usuarios internos; se agrega una implementación de demo. |
| `AgroFlowPolicies.Read` y `CatalogManagement` | Lectura de conversaciones y gestión de transportistas fuera de `LocalDemo`. |
| `TimeProvider` | Tiempos de persistencia y vencimiento deterministas en pruebas. |
| n8n `Edit Fields` | Fuente de `chatId`, `messageId`, `instance`, `text` y `messageTimestamp` para el payload mínimo. |
| Credencial Header Auth de n8n | El mismo mecanismo que usa `Send WhatsApp reply` sirve para la clave de servicio, sin secretos en el JSON. |

## 5. Contratos propuestos (no publicados)

Todos los errores usan `application/problem+json` con `code`. Los cuerpos rechazan propiedades desconocidas, como hoy hace el contrato de turnos.

### 5.1 Transportistas (usuarios internos)

| Método y ruta | Descripción | Respuestas |
|---|---|---|
| `GET /api/v1/drivers?active=` | Lista del ingenio. | `200` |
| `GET /api/v1/drivers/{id}` | Detalle; otro ingenio ⇒ `404`. | `200`, `404` |
| `POST /api/v1/drivers` | Alta `{name, dni, whatsApp}`. | `201`, `400`, `409 duplicate_dni`/`duplicate_whatsapp`/`duplicate_identifier` |
| `PUT /api/v1/drivers/{id}` | Edición. | `200`, `400`, `404`, `409` |
| `POST /api/v1/drivers/{id}/inactivation` | Inhabilitación. | `200`, `404` |

Las rutas y el recibo de mutación ya existen en `MasterDataController`. Para publicarlas hay que decidir si se conserva el encabezado obligatorio `Idempotency-Key` (OD-006) y alinear los códigos de error en minúsculas con el estilo en mayúsculas del contrato de turnos.

### 5.2 Ingesta para n8n (credencial de servicio)

`POST /api/v1/conversation-events`

```json
{
  "provider": "evolution",
  "instance": "fixture-instance-A",
  "providerMessageId": "fixture-in-001",
  "remoteAddress": "<REMITENTE_ENMASCARADO>@s.whatsapp.net",
  "providerTimestamp": "2030-01-01T12:00:00Z",
  "contentType": "text",
  "text": "[SINTÉTICO] mensaje de prueba"
}
```

`201 Created` (nuevo) o `200 OK` (repetición idéntica):

```json
{
  "disposition": "process",
  "conversationId": "…",
  "messageId": "…",
  "processing": { "claimToken": "…", "expiresAt": "…" },
  "sender": { "recognized": true, "transportistaId": "…" }
}
```

- La repetición idéntica devuelve `disposition: "duplicate_stop"`, los mismos IDs y el estado del procesamiento, **sin** `claimToken`.
- `409 CONVERSATION_EVENT_CONFLICT`: misma clave con texto, tipo o tiempo del proveedor distintos.
- `400 VALIDATION_ERROR`: campos ausentes o formato inválido (texto vacío o mayor de 4096, timestamp fuera de rango, dirección de grupo `@g.us`).
- `401`: credencial ausente o inválida. `403 CHANNEL_INSTANCE_NOT_ALLOWED`: instancia desconocida o inactiva.
- `sender.recognized = false` para direcciones `LID`/`OTHER` o números sin transportista activo en el ingenio de la instancia. No se devuelve el nombre del transportista.
- El campo `normalizedPhone` de la propuesta no se recibe: el backend lo deriva de `remoteAddress` solo si es `@s.whatsapp.net`, evitando que n8n decida identidad.

### 5.3 Respuestas y procesamiento (credencial de servicio)

| Método y ruta | Cuerpo | Efecto |
|---|---|---|
| `POST /api/v1/conversation-reply-intents` | `{inboundMessageId, claimToken, text}` | Crea la saliente `PENDING_SEND`. Devuelve `{replyId, conversationId, instance, recipientAddress, text}` para que n8n envíe exactamente eso. Repetición con el mismo texto ⇒ `200` con la misma intención; texto distinto ⇒ `409 REPLY_INTENT_CONFLICT`; token vencido o ajeno ⇒ `409 PROCESSING_CLAIM_INVALID`. |
| `PATCH /api/v1/conversation-reply-intents/{replyId}/send-status` | `{status: accepted\|failed\|unknown, providerMessageId?}` | Transiciones `PENDING_SEND→ACCEPTED\|FAILED\|UNKNOWN` y `UNKNOWN→ACCEPTED\|FAILED`. Repetir el mismo estado ⇒ `200`; otra transición ⇒ `409 INVALID_SEND_STATUS_TRANSITION`. Completa el procesamiento del mensaje entrante. |
| `PATCH /api/v1/conversation-events/{messageId}/processing` | `{claimToken, status: failed, errorCode}` | Cierra el procesamiento sin respuesta (agente fallido, remitente rechazado por política). `errorCode` pertenece a un catálogo cerrado. |

### 5.4 Consultas del dashboard (usuarios internos)

| Método y ruta | Descripción |
|---|---|
| `GET /api/v1/conversations?cursor=&limit=` | Conversaciones del ingenio por `LastMessageAtUtc` descendente. Cada ítem: `id`, `remoteAddressMasked`, `sender {recognized, transportistaId?, name?}`, `lastMessageAt`, `lastMessagePreview` (máx. 80), `needsAttention` (procesamiento fallido, vencido o envío incierto). `limit` entre 1 y 50 (por defecto 20). |
| `GET /api/v1/conversations/{id}` | Cabecera de la conversación; otro ingenio ⇒ `404`. |
| `GET /api/v1/conversations/{id}/messages?cursor=&limit=` | Mensajes en orden `PersistedAtUtc, Id` ascendente, con `direction`, `text`, `providerTimestamp`, `persistedAt`, `sendStatus` y `processingStatus`. |

El cursor es opaco (base64url de `PersistedAtUtc` o `LastMessageAtUtc` más `Id`), siempre se combina con el filtro de ingenio y un cursor mal formado devuelve `400`. La respuesta incluye `nextCursor` o `null`.

## 6. Autenticación y aislamiento por ingenio

### 6.1 n8n como servicio (recomendación pendiente de OD-008)

- Esquema de autenticación `ServiceKey`: encabezado `X-AgroFlow-Service-Key`, comparación en tiempo constante y clave de al menos 32 bytes desde secretos o variables de entorno (`ChannelIntegration:ServiceKey`), nunca en `appsettings.json` versionado ni en logs.
- La política `ChannelIngest` exige la credencial de servicio en **ambos** perfiles. No se publica una ruta de ingesta anónima, ni siquiera en `LocalDemo`.
- El ingenio sale de `ChannelInstance`, nunca del cuerpo. Cualquier `tenantId`/`ingenioId` recibido es una propiedad desconocida ⇒ `400`.
- En n8n, la clave vive en una credencial Header Auth distinta de la de Evolution.

### 6.2 Usuarios internos

- **Fuera de `LocalDemo`**: JWT con rol `OPERADOR`/`SUPERVISOR` y claim `ingenioId` mediante `HttpTenantContext`. Depende de B03, porque hoy el login no emite `ingenioId`.
- **En `LocalDemo`** (recomendación a aprobar): `LocalDemoTenantContext` resuelve el único ingenio sembrado y el actor `local-demo`, igual que hoy la API de turnos opera sin identidad en ese perfil. Lecturas de conversaciones y gestión de transportistas anónimas **solo** en ese perfil, documentadas en OpenAPI como en turnos.
- Toda consulta filtra por `IngenioId` en la base. Un ID de otro ingenio devuelve `404`, nunca `403`, para no revelar existencia.

## 7. Decisiones pendientes que bloquean la implementación

Ninguna está aprobada. Las recomendaciones son técnicas y requieren aprobación del equipo antes de programar la etapa que bloquean.

### OD-006 — Concurrencia e idempotencia

- **Problema**: definir la clave de deduplicación, la respuesta a repeticiones y conflictos, la reserva de procesamiento y si las mutaciones de transportistas exigen `Idempotency-Key`.
- **Alternativas**: (1) clave natural del proveedor con índice único; (2) `Idempotency-Key` enviada por n8n; (3) deduplicar solo en n8n. Para el procesamiento: opciones A, B y C de la sección 3.5. Para transportistas: conservar el encabezado obligatorio de PR #47 o quitarlo y confiar en la unicidad de DNI/WhatsApp.
- **Recomendación**: (1) con `201`/`200 duplicate_stop`/`409`; procesamiento opción A; conservar `Idempotency-Key` en transportistas.
- **Justificación**: (1) es atómica en SQLite sin coordinación externa y es la clave que propone el documento; A hace visible lo que queda sin respuesta sin reprocesar a ciegas; el código de PR #47 ya implementa y prueba la idempotencia de mutaciones.
- **Impacto en el MVP**: bloquea las etapas 1, 4 y 5. Es requisito de "los reintentos no duplican mutaciones" en la aceptación del MVP.

### OD-008 — Seguridad del canal n8n

- **Problema**: autenticar a n8n, validar la instancia, enrutar al ingenio, resolver LID frente a teléfono y verificar el origen del webhook.
- **Alternativas**: clave de servicio estática; JWT de servicio (requiere B03); firma HMAC del cuerpo; dejar la demo anónima.
- **Recomendación**: clave de servicio + `ChannelInstance`; LID registrado como no reconocido, sin inferir teléfono; verificación del webhook Evolution → n8n fuera de esta iteración, documentada como riesgo.
- **Justificación**: es la opción más simple que impide escrituras anónimas de datos personales, funciona sin B03 y reutiliza Header Auth de n8n. HMAC agrega reloj y ventana de repetición sin beneficio a escala de demo.
- **Impacto en el MVP**: bloquea las etapas 3 a 5. Sin esta decisión no puede exponerse ninguna escritura del canal.

### OD-009 — Auditoría y conservación

- **Problema**: qué texto se conserva, quién lo lee, qué pasa con remitentes no reconocidos, y cuánto tiempo y cómo se borra.
- **Alternativas**: conservar texto completo; solo metadatos; texto con retención limitada. Para los no reconocidos: registrar marcado, registrar solo metadatos o rechazar sin registrar.
- **Recomendación**: conservar texto (máx. 4096) de entrantes y salientes; lectores operador y supervisor del ingenio; registrar no reconocidos marcados (permite al operador detectar números a dar de alta); sin `pushName`, payloads ni logs de texto o teléfono; datos ficticios o de prueba consentida; retención académica con purga manual documentada al cierre de la demo, sin endpoint de borrado en esta iteración.
- **Justificación**: el dashboard no puede mostrar conversaciones sin texto; el registro de no reconocidos conecta directamente con el requerimiento de alta de transportistas.
- **Impacto en el MVP**: bloquea las etapas 4 y 6 (qué se guarda y quién lo ve).

### OD-010 — Entrega de notificaciones

- **Problema**: persistir antes o después de enviar, estados de envío, reconciliar la incertidumbre y reintentos.
- **Alternativas**: persistir intención antes de enviar; enviar y registrar después; no registrar salientes.
- **Recomendación**: intención antes de enviar; estados `PENDING_SEND/ACCEPTED/FAILED/UNKNOWN`; sin reintentos automáticos del envío; `UNKNOWN` ante timeout; sin callbacks de entrega en esta iteración.
- **Justificación**: es el candidato que recomienda la propuesta; deja trazado cada intento y evita reenvíos ciegos.
- **Impacto en el MVP**: bloquea la etapa 5. Los estados de entrega o lectura no forman parte del MVP.

### OD-011 — Operación y calidad de servicio

- **Problema**: qué hacer si la API de historial falla, dónde ubicar el acuse del webhook, timeouts y datos personales en logs.
- **Alternativas**: fail-closed (sin ingesta no hay agente ni envío); continuidad degradada. Acuse: mantener el temprano o moverlo después de la ingesta.
- **Recomendación**: fail-closed para la demo; mover `Respond to Webhook` después de una ingesta exitosa; timeouts finitos (10 s) en los nodos HTTP nuevos; logs sin texto, direcciones ni teléfonos.
- **Justificación**: sin API, el agente tampoco puede consultar ni crear turnos, así que la continuidad degradada no aporta funcionalidad real y sí respuestas sin registro. El acuse posterior evita confirmar al proveedor algo que no quedó durable.
- **Impacto en el MVP**: bloquea la etapa 7 (workflow). No bloquea el backend.

### OD-013 — Datos maestros (complementaria)

- **Problema**: el formato del WhatsApp registrado determina si coincide con el JID.
- **Alternativas**: solo dígitos sin validar formato; E.164 obligatorio; heurísticas para el `9` móvil argentino.
- **Recomendación**: E.164 obligatorio con el mismo patrón que `carrierPhone` (`^\+[1-9][0-9]{7,14}$`), normalizado a dígitos; sin heurísticas; índice único global.
- **Justificación**: coincidencia exacta y predecible con `remoteJid` (`549…`) y coherencia con el contrato de turnos.
- **Impacto en el MVP**: bloquea la etapa 2.

### Decisiones de diseño nuevas (no figuran en el registro)

| ID provisorio | Pregunta | Recomendación |
|---|---|---|
| DD-1 | ¿Gestión de transportistas y lectura de conversaciones anónimas en `LocalDemo`? | Sí, solo en ese perfil y con la misma advertencia que turnos. Fuera de él, JWT (B03). |
| DD-2 | ¿Tabla `ChannelInstance` o configuración? | Tabla sembrada desde configuración. |
| DD-3 | ¿Respuesta automática a remitentes no reconocidos? | Texto fijo por plantilla de n8n, registrado como intención, sin invocar al agente ni herramientas. |
| DD-4 | Vencimiento de la autorización de procesamiento | 5 minutos (configurable), solo informativo. |

## 8. Cambios propuestos al contrato OpenAPI

No se modifica [openapi.yaml](../../../docs/contracts/openapi.yaml) en la Fase 0. Cada etapa aprobada agregará en el mismo PR:

- tags `Transportistas`, `Canal` y `Conversaciones`;
- esquema de seguridad `ServiceKey` (`apiKey` en encabezado `X-AgroFlow-Service-Key`) aplicado a las rutas del canal; `security: []` en lecturas y transportistas solo con la descripción del perfil `LocalDemo`;
- esquemas `DriverInput`, `Driver`, `MutationReceipt`, `ConversationEventInput`, `ConversationEventResult`, `ReplyIntentInput`, `ReplyIntent`, `SendStatusInput`, `ProcessingResultInput`, `ConversationSummary`, `ConversationDetail`, `ConversationMessage`, `Page<T>`;
- códigos nuevos en `ProblemDetails.code`;
- aumento de `info.version` a 0.4.0 y actualización de su descripción.

## 9. Migraciones

Se agregan a `backend/AgroFlow.Data/Migrations/Appointments/` en orden, una por etapa:

1. `AddMasterDataFoundationTables`: `AuditEvents`, `IdempotencyRecords` (único `(Scope, ActorIdentity, Operation, Key)`) y, si B02 lo aprueba, `CreatedAtUtc`/`InactivatedAtUtc` de los maestros.
2. `AddGlobalWhatsAppUniqueIndex`: índice único sobre `Transportistas.NormalizedWhatsApp`, con verificación previa de duplicados.
3. `AddChannelInstances`.
4. `AddConversations`: `Conversations`, `ConversationMessages`, `MessageProcessings` con sus índices filtrados y checks.

Todas son aditivas y deben aplicarse sobre una base de demo existente sin pérdida de turnos, respetando la continuidad de SQLite de `plataforma-y-segregacion`. `EfModelAndMigrationVerificationTests` se actualiza para afirmar el nuevo modelo en lugar de la fundación ignorada.

## 10. Estrategia de pruebas

| Nivel | Cobertura |
|---|---|
| Dominio | Invariantes de las entidades: direcciones, estados, transiciones de envío y procesamiento, longitud del texto y UTC. |
| Persistencia (SQLite migrada) | Índices únicos, checks, carrera de dos inserciones del mismo mensaje y de dos mensajes nuevos de la misma dirección, `LastMessageAtUtc` monótono y migración sobre una base con datos previos. |
| Servicio | Disposición `process`/`duplicate_stop`/conflicto; reconocimiento (activo, inactivo, otro ingenio, LID); validación del token de procesamiento; transiciones. |
| HTTP (`WebApplicationFactory`) | `401` sin clave, `403` con instancia inactiva, `400` con propiedades desconocidas o `ingenioId` en el cuerpo, `404` entre ingenios, paginación con cursor manipulado, sin duplicados ni omisiones con inserciones concurrentes entre páginas. |
| Superficie | `BackendSurfaceTests` y `MasterDataHttpTests` se actualizan para afirmar las rutas habilitadas y la ausencia de rutas no aprobadas. |
| Frontend | Mapper, repositorio HTTP y hook de conversaciones y transportistas con Vitest. |
| n8n | Fixtures sintéticos de los envelopes de Evolution; copia aislada del workflow con agente y proveedor simulados; casos de la sección 6 de la propuesta. |
| E2E | Número de prueba consentido, registrado por la API y nunca versionado. |

Todos los datos de prueba son ficticios y se usan dos ingenios en las pruebas de aislamiento.

## 11. Riesgos

| Riesgo | Mitigación |
|---|---|
| La verificación del webhook Evolution → n8n sigue sin autenticar | Documentado; la clave de servicio protege la API, no el webhook. |
| El acuse temprano actual pierde mensajes ante una caída | Mover el acuse (OD-011); vencimiento visible de la autorización. |
| Escrituras concurrentes en SQLite sin WAL | Transacciones cortas, `Default Timeout=5` actual y pruebas de concurrencia; WAL según OD-014. |
| El índice global falla si ya hay duplicados entre ingenios | Verificación previa en la migración, con mensaje claro. |
| Datos personales en una API de demo anónima | Escrituras siempre autenticadas; lecturas anónimas solo en `LocalDemo` y escuchando en loopback. |
| El CI rechaza el workflow con rutas nuevas | Actualizar `validate-integrations.mjs` y sus pruebas en la misma etapa que el workflow. |
| Divergencia entre el modelo mock del frontend y la API | El adaptador HTTP mapea a la entidad de presentación; el estado `escalado` del mock no se emula. |

## 12. Responsabilidades

| Área | Responsabilidad |
|---|---|
| Backend | Identidad, deduplicación, autorización de procesamiento, estados, aislamiento y paginación. |
| Contrato | OpenAPI por etapa, sin rutas no implementadas. |
| n8n | Validar estructura, invocar la API, enrutar por `disposition` y `sender`, enviar solo el texto de la intención e informar el resultado. Sin reglas de negocio. |
| Frontend | Mostrar lo que devuelve la API; no recalcular reconocimiento ni estados. |
