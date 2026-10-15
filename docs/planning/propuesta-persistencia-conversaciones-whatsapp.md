# Persistencia de conversaciones de WhatsApp

**Propuesta no aprobada/no implementada.** Documento de discusión, no contrato ni nueva especificación. Recomendación: probar primero un flujo aislado basado en el JSON real de n8n, con fixtures inventados y API/proveedor simulados; después, si se aprueba, guardar eventos mínimos en el backend de AgroFlow y leerlos desde el dashboard. No se modificaron workflows, API ni OpenSpec; no se ejecutaron estas pruebas.

## 1. Qué existe y qué falta

**Alcance de la evidencia:** el workflow, README del chatbot y Compose citados pertenecen a la integración propuesta en [PR #79](https://github.com/agustinvallante/AgroFlow/pull/79), fijada al commit `15944f4031e903e1f0a31aac7a9a20de59e2a138`; no forman parte del baseline actual de `main`. Los demás enlaces locales describen el repositorio actual.

| Observado en la evidencia indicada | Consecuencia |
|---|---|
| [Workflow real propuesto en PR #79](https://github.com/agustinvallante/AgroFlow/blob/15944f4031e903e1f0a31aac7a9a20de59e2a138/chatbot/workflows/Chatbot.json): `Webhook → Respond to Webhook → Filter → Edit Fields → AI Agent1 → Send WhatsApp reply` | La respuesta HTTP al webhook ocurre antes del agente y de cualquier persistencia propuesta; no demuestra éxito de negocio ni entrega de WhatsApp. |
| `Filter`: `fromMe == false`, `messageType == conversation`, `remoteJid` sin `@g.us` | Hoy sólo procesa texto simple entrante no grupal. No guarda otros tipos ni estados de entrega. |
| `AI Agent1` usa `OpenAI Chat Model`, `Window Buffer Memory` y tres herramientas HTTP | `Create Appointment`, `Lookup Appointments By Phone` y `Report En Camino` consumen la API. La memoria de ventana (10, clave `chatId`) no es un historial autoritativo. |
| [OpenAPI actual](../contracts/openapi.yaml) | Sólo salud y alta/consulta/transición de turnos; **no hay API de historial**. La demo declara `security: []` y un único ingenio: no equivale a seguridad MVP. |
| [Composición frontend](../../frontend/src/composition/container.ts) y [repositorio mock](../../frontend/src/infrastructure/mock/repositories/MockConversacionRepository.ts) | Conversaciones siguen simuladas incluso en modo HTTP; [datos mock](../../frontend/src/infrastructure/mock/data/conversaciones.mock.ts) y [vista](../../frontend/src/presentation/views/ChatbotView.tsx) no prueban integración ni requisitos. |
| [Compose propuesto en PR #79](https://github.com/agustinvallante/AgroFlow/blob/15944f4031e903e1f0a31aac7a9a20de59e2a138/infra/compose.yaml) | PostgreSQL y Redis sirven a **Evolution**, no al historial AgroFlow. n8n tiene almacenamiento local propio (SQLite/volumen `n8n_data`), separado del archivo SQLite de la API de negocio. No se inspeccionaron bases. |

El [README del chatbot propuesto en PR #79](https://github.com/agustinvallante/AgroFlow/blob/15944f4031e903e1f0a31aac7a9a20de59e2a138/chatbot/README.md) sólo registra una respuesta al saludo; herramientas y E2E de turnos siguen pendientes. El JSON se importa inactivo, con `pinData` vacío y referencias de credenciales por nombre, no secretos.

La autoridad vigente es [OpenSpec de integración](../../openspec/specs/integracion-whatsapp-n8n/spec.md), [acceso](../../openspec/specs/acceso-y-autorizacion/spec.md), [segregación](../../openspec/specs/plataforma-y-segregacion/spec.md) y [monitoreo](../../openspec/specs/monitoreo-operativo/spec.md). Aplican [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md), [ADR-003](../architecture/decisions/ADR-003-backend-fuente-de-verdad.md), [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md) y [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md): fuente documental central, backend autoritativo, React/Vite y SQLite académico; PostgreSQL comercial requiere transición aprobada, no reutilizar la base de Evolution.

## 2. Campos reales y adaptación propuesta

Las expresiones siguientes son **observadas**, no garantizan identidad autenticada ni formato válido. En `Edit Fields`, `$json` es el item entrante:

| Campo existente | Fuente/expresión existente | Tratamiento candidato |
|---|---|---|
| `chatId` | `$json.body.data.key.remoteJid` | Conservar como dirección remota del canal; no confundir con teléfono. |
| `phone` | `$json.body.data.key.remoteJid.split('@')[0]` | No basta para E.164. Validar tipo de dirección y normalización autorizada. |
| `name` | `$json.body.data.pushName || ''` | Omitir del primer payload: no aporta identidad verificada. |
| `messageId` | `$json.body.data.key.id` | Fuente de `providerMessageId` entrante; exigir presencia y formato. |
| `instance` | `$json.body.instance` | Validar contra instancias habilitadas para la credencial de servicio. |
| `text` | `$json.body.data.message.conversation.trim()` | Texto mínimo con límites acordados; validar antes de `trim()`. |
| `textNormalized` | `conversation.trim().toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '')` | No guardar una segunda copia; no sustituye el texto original mínimo. |
| `receivedAt` | `new Date($json.body.data.messageTimestamp * 1000).toISOString()` | Es tiempo del **proveedor**, no recepción API; proponer `providerTimestamp`. Validar segundos/rango. |

Las herramientas actuales agregan `+` a `phone`; la consulta usa parámetro `phone`, el alta usa `carrierPhone`. No ampliar esa regla a direcciones `@lid`: WhatsApp LID no demuestra un teléfono. Su resolución sigue abierta; sin vínculo verificado no llamar herramientas de negocio ni inventar E.164. Debe decidirse si conservar un evento restringido de identidad no resuelta o rechazarlo.

`Send WhatsApp reply` usa la instancia de `$env.EVOLUTION_INSTANCE` en `/message/sendText/…`, no directamente `body.instance`; envía `number: $('Edit Fields').item.json.chatId` y `text: $json.output`. Exigir coherencia entre instancia recibida, autorizada y usada para enviar. La forma de la respuesta de Evolution **no está demostrada por este JSON**; no suponer dónde devuelve el ID.

## 3. Payload mínimo candidato (no soportado hoy)

Primera iteración: **texto solamente**, sin adjuntos, nombre visible, prompt, transcripción completa, webhook completo, resultados completos de herramientas ni datos maestros duplicados. La identidad del evento nunca se pide al modelo.

Ejemplos ilustrativos con IDs inventados y texto explícitamente sintético. Dirección/teléfono enmascarados: **no ejecutables**, no son números ni JIDs válidos. Un fixture del mock podrá usar identificadores exclusivos de su simulador, nunca destinos reales.

Entrada:

```json
{
  "direction": "inbound",
  "provider": "evolution",
  "instance": "fixture-instance-A",
  "providerMessageId": "fixture-in-001",
  "remoteSenderAddress": "<REMITENTE_ENMASCARADO>@s.whatsapp.net",
  "normalizedPhone": "+<TELEFONO_ENMASCARADO_NO_EJECUTABLE>",
  "providerTimestamp": "2030-01-01T12:00:00Z",
  "contentType": "text",
  "text": "[SINTÉTICO] mensaje de prueba sin persona ni turno real"
}
```

Intención de salida, antes del envío:

```json
{
  "direction": "outbound",
  "provider": "evolution",
  "instance": "fixture-instance-A",
  "outboundIntentId": "fixture-intent-001",
  "inReplyToEventId": "fixture-event-001",
  "conversationId": "fixture-conversation-001",
  "remoteRecipientAddress": "<DESTINATARIO_ENMASCARADO>@s.whatsapp.net",
  "providerMessageId": null,
  "contentType": "text",
  "text": "[SINTÉTICO] respuesta de prueba sin resultado de negocio",
  "status": "pending_send"
}
```

- `providerMessageId` de salida sólo se incorpora desde evidencia validada del proveedor, nunca desde el agente ni copiando el ID entrante. `outboundIntentId` debe asignarse y conservarse fuera del LLM, preferentemente por el backend.
- Backend asignaría `eventId`, `conversationId`, ámbito autorizado, `apiReceivedAt` y `persistedAt` UTC; el tiempo del proveedor es separado y no controla por sí solo el orden de ingestión. El ejemplo de salida usa IDs devueltos por el mock/backend, no libremente elegidos en producción.
- Backend resolvería conversación por canal/instancia/remitente verificados y reglas de enrutamiento aprobadas. No aceptar `tenantId`/`ingenioId` del cuerpo como autorización; validar también cualquier `conversationId` recibido.
- `appointmentId` sería opcional y sólo desde una respuesta confirmada de la API; backend verifica pertenencia al mismo ingenio/transportista. El historial no convierte una intención conversacional en turno creado.
- Clave entrante candidata: ámbito autorizado + proveedor + instancia + dirección + `providerMessageId`; índice único y comparación del contenido relevante. Mismo ID en otra instancia no debe colisionar; misma clave con contenido cambiado debe producir conflicto, no sobrescribir silenciosamente.

### Modelo conceptual candidato: no es un esquema EF aprobado

Los nombres siguientes ilustran responsabilidades y relaciones, **no obligan a implementar estos campos, tablas ni tipos**. `?` indica un dato opcional o todavía no disponible; mapeo, restricciones definitivas y migraciones requieren aprobación.

| Entidad candidata | Campos ilustrativos | Responsabilidad y límites |
|---|---|---|
| `Conversation` | `Id`, `IngenioId`, `Provider`, `ChannelInstanceId`, `RemoteAddress`, `TransportistaId?`, `CreatedAtUtc`, `LastMessageAtUtc` | Agrupar mensajes del ámbito autorizado por backend. `TransportistaId` sólo tras vínculo verificado; una dirección remota no prueba identidad. Enrutamiento y agrupación siguen pendientes, no se deducen del body. |
| `ConversationMessage` | `Id`, `ConversationId`, `Direction`, `ProviderMessageId?`, `ContentType`, `Text?`, `ProviderTimestampUtc?`, `PersistedAtUtc`, `InReplyToMessageId?`, `SendStatus?` | Texto primero; conservar `Text` depende de privacidad. ID del proveedor obligatorio para entrada admitida, ausente en salida antes de aceptación. Tiempo del proveedor separado de persistencia. Respuesta ligada a mensaje del mismo ámbito; estado de envío distinto del procesamiento. |
| `MessageProcessing` | `InboundMessageId` único, `Status`, `ClaimToken?`, `ClaimExpiresAtUtc?`, `CompletedAtUtc?`, `LastErrorCode?` | Una reserva por entrada: etiquetas propuestas `pending`/`processing`/`completed`/`failed`, no contrato vigente. Código de error opcional y acotado para recuperación, sin texto, identidad ni payload sensible. |

- Relación candidata: conversación → mensajes; mensaje entrante → un registro de procesamiento. La unicidad del evento entrante y la adquisición/renovación del claim necesitan operaciones atómicas del backend: leer y luego escribir sin protección no evita carreras.
- Vencer `ClaimExpiresAtUtc` **no autoriza** reejecutar herramientas ni reenviar: puede haber efectos confirmados o inciertos antes de la caída. Reconciliar evidencia y aplicar idempotencia específica antes de recuperar; `failed` tampoco implica ausencia de efectos. La expiración del claim no fija retención del historial.
- `SendStatus` podría distinguir intención pendiente, aceptación, falla o resultado incierto; entrega y lectura sólo con evidencia validada. No confundir `completed` del procesamiento con entrega de WhatsApp ni éxito de un turno.
- Mantener `MessageProcessing` separado o representarlo con columnas del mensaje es una decisión futura; no crear dos fuentes de verdad. Los IDs de evento/intención de los ejemplos necesitarían un mapeo explícito al contrato aprobado, no equivalencias de nombres asumidas.

### Historial para dashboard versus contexto del agente

El dashboard consultaría historial autorizado y paginado; el contexto del modelo sería una selección **acotada** de mensajes pertinentes recuperada desde la API antes de preparar el prompt, con límites y privacidad aprobados. No son la misma lectura ni exige enviar toda la transcripción. `Window Buffer Memory` actual no reemplaza el historial autoritativo ni confirma estado de negocio; éste siempre se consulta a la API. Esta alternativa no requiere Redis ni nueva infraestructura por defecto y todavía no define un nodo ni contrato de recuperación.

### Endpoints ilustrativos: no existen ni se pueden usar todavía

| Ruta propuesta | Finalidad |
|---|---|
| `POST /api/v1/conversation-events` | Ingesta entrante idempotente. |
| `POST /api/v1/conversation-reply-intents` | Reservar/persistir intención de salida ligada al evento autorizado. |
| `PATCH /api/v1/conversation-reply-intents/{id}/send-status` | Registrar evidencia de aceptación/falla/resultado incierto; actualizaciones necesitarían contrato idempotente. |
| `GET /api/v1/conversations?cursor=…&limit=…` | Lista acotada para usuario interno autorizado. |
| `GET /api/v1/conversations/{id}/events?cursor=…&limit=…` | Detalle paginado, con aislamiento backend. |

Respuesta candidata de ingesta: IDs persistidos y una disposición explícita `process` o `duplicate_stop`; semántica HTTP, conflictos, reservas de procesamiento y respuesta a reintentos quedan pendientes en OD-006/008. No asumir que cualquier `200` permite ejecutar el agente. Un replay debe detenerse **antes** del agente y sus herramientas; detenerlo no recupera automáticamente un original interrumpido.

## 4. Nodos e inserciones exactas propuestas

Estos nombres son **nuevos**, no nodos existentes. Preferir `Edit Fields (Set)`, HTTP Request e IF/Switch; Code sólo acotado si la validación/normalización no se puede expresar claramente, sin reglas de negocio ni acceso a bases.

Flujo candidato: `Webhook → validación de canal → Respond to Webhook (posición por decidir) → Filter → validación de texto → Edit Fields → normalización del evento → persistir entrada → decidir procesamiento → AI Agent1 → preparar salida → persistir intención → Send WhatsApp reply → validar evidencia → actualizar estado`.

| Inserción sobre el grafo real | Nodo candidato y responsabilidad |
|---|---|
| Entre `Webhook` y `Respond to Webhook` | `Validate Channel` (IF): autenticación/verificación del origen según OD-008. Sin ella, el teléfono declarado en el body no prueba al remitente. |
| Entre `Filter` y `Edit Fields` | `Validate Text Event` (IF): estructura, tipo, ID, instancia y timestamp antes de expresiones que pueden fallar; destino no grupal permitido. |
| Entre `Edit Fields` y `AI Agent1` | `Normalize History Event` (Edit Fields): payload mínimo, proveedor explícito, `providerTimestamp`, dirección y teléfono sólo si verificado. |
| A continuación, todavía antes de `AI Agent1` | `Persist Inbound Event` (HTTP Request): ingesta autenticada; preservar contexto por referencias explícitas, no pasar al agente el body de la respuesta HTTP como mensaje. |
| Entre persistencia y `AI Agent1` | `Route Ingest Result` (Switch): validar estado HTTP, esquema, IDs y disposición; sólo `process` continúa. `duplicate_stop`, conflicto, identidad no resuelta o error no invocan agente/herramientas. |
| Entre `AI Agent1` y `Send WhatsApp reply` | `Prepare Reply Intent` (Edit Fields): texto de salida no vacío/acotado, evento/conversación desde API, instancia/destinatario autorizados. `Persist Reply Intent` (HTTP Request) y `Route Intent Result` (Switch) validarían su confirmación. |
| En `Send WhatsApp reply`, cambio futuro explícito | Usar el texto de la intención confirmada, no `$json.output` del item HTTP. Conservar destinatario autorizado y desactivar reintentos ciegos del envío. |
| Después de `Send WhatsApp reply` | `Validate Send Result` (IF/Switch): validar respuesta real contra contrato del proveedor. `Update Reply Send Status` (HTTP Request): persistir ID/evidencia o resultado incierto; no asumir entrega por HTTP 2xx. |

Aceptación por Evolution, entrega y lectura son estados distintos. Una actualización por callback necesitaría una futura ruta autenticada separada, correlación validada y manejo de duplicados/desorden. El `Filter` actual descarta `fromMe`; no sirve como receptor universal de acuses. El `Respond to Webhook` es un acuse HTTP a quien invoca, no una confirmación al transportista.

### Orden y fallas: opciones pendientes, no garantías

- **Persistir antes de enviar** (candidato recomendado): deja intención registrada y permite explicar el intento; añade latencia y dependencia del historial. No hace atómico el envío externo con SQLite.
- **Enviar antes de persistir**: prioriza respuesta, pero puede dejar mensajes enviados sin historial si la escritura falla. Registrar luego no repara automáticamente esa brecha.
- **Historial caído**: recomendar fail-closed para la primera prueba sintética (no agente/herramientas/envío sin ingesta), porque permite demostrar deduplicación. Antes de uso operativo, decidir fail-closed versus continuidad degradada: bloquear funciones críticas tiene costo real y no está autorizado aquí. No confundir fallo de historial con fallo o reversión de un turno ya persistido.
- **Acuse temprano actual**: un éxito HTTP anterior a la escritura puede impedir redelivery si el proceso cae. Decidir mover el acuse después de aceptación durable o incorporar una entrada durable previa; ninguna existe demostrada hoy. No mantener solicitudes abiertas sin límites ni asumir reintentos del proveedor.
- **Durabilidad futura**: inbox/reserva de procesamiento y outbox/registro de intentos con estados recuperables, recuperación ensayada y reconciliación serían necesarios para sobrevivir caídas entre pasos. No se proponen colas adicionales por defecto para la demo ni se presenta una rama de error desacoplada de n8n como recuperación confiable.
- **Tres duplicaciones diferentes**: ingesta idempotente evita filas repetidas; control atómico de procesamiento evita dos agentes/herramientas concurrentes; envío externo necesita garantías propias del proveedor. La primera no implica las otras. Reintentar herramientas de turnos requiere la política OD-006, no sólo la clave de historial.
- **Timeout de envío**: el proveedor puede haber aceptado aunque se pierda la respuesta/callback. Marcar resultado incierto y reconciliar con evidencia; no reenviar ciegamente. No prometer exactamente una entrega externa.

## 5. Seguridad y eficiencia

- Credencial de servicio n8n en el almacén de credenciales, distinta de la clave de Evolution; backend autentica, autoriza instancias/ingenios y verifica procedencia del remitente. Firma/token, rotación y vínculo de identidad dependen de OD-008. No incluir secretos productivos en JSON, URLs ni documentación.
- No registrar texto, direcciones, teléfonos ni cuerpos completos en logs de API/n8n/proveedor; acordar redacción y acceso a ejecuciones. Observar códigos, tiempos e IDs opacos con acceso restringido. No exportar historial de ejecuciones real para pruebas.
- Un evento mínimo por entrada; una intención y actualización por salida. No mandar transcripción/webhook completos en cada llamada; no agregar llamadas LLM para identidad, resumen o persistencia.
- Proponer índices por ámbito/conversación/orden estable y unicidad del evento; cursor opaco con desempate por ID y límites de página. El dashboard sólo consume AgroFlow, nunca SQL de Evolution ni almacenamiento de n8n. Refresco incremental acotado, no carga de todas las conversaciones.
- HTTP Request con timeout finito, respuesta completa (status + body) y Switch que valide JSON antes de continuar. Manejar error de transporte aparte del HTTP; no basta `neverError` ni continuar incondicionalmente.
- `400`/`401`: detener y corregir formato/credencial, sin reintento automático. `429`: respetar espera informada. `5xx`/timeout: backoff y máximo de intentos sólo para lecturas o escrituras con contrato idempotente aprobado y la misma clave; confirmar resultado incierto antes de efectos posteriores.
- No agregar batch, broker ni infraestructura por anticipación a escala demo. Minimización no equivale a retención cero: **no se fija ninguna duración de conservación**; acceso, borrado, respaldo y privacidad requieren OD-009/011.

## 6. Pruebas propuestas, aún no ejecutadas

Sí: basarlas en el grafo y expresiones reales evita probar una integración imaginaria. No: usar mensajes actuales, exportar ejecuciones o llamar OpenAI/Evolution reales en la primera validación.

1. **Fixtures inventados**: construir envelopes con `body.instance`, `data.key`, `messageType`, `message.conversation`, `messageTimestamp`, usando IDs sintéticos. Guardar sólo fixtures nuevos, no capturas de WhatsApp. Validar expresiones sin activar servicios actuales.
2. **Workflow aislado + API/proveedor mock**: copia de prueba inactiva y sin credenciales reales, sustituir `AI Agent1` por salida fija y herramientas por contadores/dobles; simular envío y status HTTP. Esto verifica cableado, no calidad conversacional del LLM ni negocio real.
3. **Backend/dashboard futuros**: tras aprobar contrato, pruebas sobre almacenamiento de prueba separado y dos ingenios ficticios; adaptador frontend HTTP contra mock/entorno aislado. No tocar la SQLite histórica.

| Caso sintético | Evidencia esperada del contrato candidato |
|---|---|
| Entrada original / replay / replay simultáneo | Una fila; sólo un procesamiento autorizado; cero llamadas adicionales a agente/herramientas/envío por duplicado. |
| Evento fuera de orden | Timestamps del proveedor separados del orden de ingestión; paginación estable sin pérdida. |
| Misma clave, texto cambiado | Conflicto explícito; no sobrescritura ni efectos. |
| Mismo provider ID, instancias distintas | No colisión; ambos sólo dentro del ámbito autorizado correspondiente. |
| LID desconocido / body con otro ingenio / origen no probado | No inventar teléfono, no herramientas, no acceso cruzado; resultado de conservación/rechazo por decidir. |
| Timeout API antes/después de persistir | Reintento seguro con misma clave cuando esté aprobado; no segunda ejecución de negocio por incertidumbre. |
| API `400`, `401`, `429`, `5xx` o JSON inválido | Ramas y límites explícitos; no interpretar error como autorización de procesamiento. |
| Proveedor acepta, respuesta/callback perdido | Estado incierto, no reenvío ciego ni falsa entrega. |
| Persistencia de intención / actualización de salida fallida | Según política elegida, no envío sin intención o brecha visible; turno previamente confirmado no se revierte ni se niega ficticiamente. |
| Reinicio entre acuse, ingesta, agente y envío | Mostrar brechas actuales y necesidad de recuperación; no declarar durabilidad por una prueba feliz. |
| Dashboard: otro ingenio, ID directo y cursor manipulado | Sin exposición cruzada; páginas acotadas, sin duplicados/omisiones con nuevas entradas. |

Primera validación sin números reales, OpenAI ni llamadas de proveedor. Un E2E posterior de WhatsApp requiere consentimiento separado, identidad de prueba provisionada y autorizada, datos ficticios asociados, entorno preparado y decisión explícita sobre costo/privacidad. El saludo observado no reemplaza estas pruebas.

## 7. Decisiones y siguiente paso

Consultar [decisiones abiertas](open-decisions.md); este documento no las cierra. Fallas, acuses y recuperación cruzan OD-008/010/011: requieren una política coordinada, no decisiones aisladas de cada nodo.

**Trazabilidad:** propuesta vinculada al [epic aprobado #46](https://github.com/agustinvallante/AgroFlow/issues/46), con [#7](https://github.com/agustinvallante/AgroFlow/issues/7) y [#42](https://github.com/agustinvallante/AgroFlow/issues/42) como referencias relacionadas y PR #79 como evidencia de integración propuesta. Publicar este documento no completa el epic ni implementa esas issues.

| Decisión | Pregunta que necesita aprobación |
|---|---|
| OD-006 | Clave y respuesta de duplicados/conflictos, concurrencia, reserva/reanudación del procesamiento y relación con idempotencia de turnos. |
| OD-008 | Autenticación de servicio/origen, firma, instancia autorizada, enrutamiento de conversación, resolución LID/teléfono y correlación. |
| OD-009 | Texto conservable, auditoría versus historial conversacional, lectores autorizados, retención/borrado y tratamiento de remitentes no reconocidos. |
| OD-010 | Proveedor y plantillas, persistir antes/después de envío, estados y callbacks, reconciliación de incertidumbre, reintentos, fallas y garantías reales del proveedor. |
| OD-011 | Fail-closed versus continuidad, acuse durable, timeouts, recuperación, respaldo, observabilidad y datos personales. |

**Promoción propuesta:** revisar opciones con el equipo → aprobar decisiones y cambio en `openspec/changes/` → acordar una capacidad central y OpenAPI/pruebas → implementar backend, n8n y adaptador frontend → ejecutar pruebas sintéticas y luego E2E consentido → consolidar OpenSpec. Hoy no se crea una nueva spec ni se habilitan endpoints, políticas o nodos.
