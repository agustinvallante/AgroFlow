# Traspaso de Persona 1: contrato congelado de la demo local

Este documento registra el cierre del contrato HTTP de la demo local (issue #49). **No redefine el contrato:** la fuente de verdad sigue siendo [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml) junto con el cambio [`demo-local-turnos-basicos`](../../openspec/changes/demo-local-turnos-basicos/). Si este resumen difiere de OpenAPI, prevalece OpenAPI. Conforme a [`docs/contracts/README.md`](../contracts/README.md), aquí se nombran esquemas y precisiones; los tipos, patrones y validaciones exactos se leen en OpenAPI.

## Estado

- Contrato congelado: OpenAPI `0.3.0`, 2026-09-27.
- Auditado contra OpenSpec (`openspec/specs/` y el cambio de la demo) y contra la rama `feat/demo-appointments-api` de Persona 3 (modelos, controlador, servicio, puerto de almacenamiento, errores y pruebas).
- Todo cambio observable posterior vuelve a Persona 1 y se refleja primero en OpenAPI y OpenSpec.

## Endpoints

| Operación | Uso | Respuestas |
|---|---|---|
| `GET /health` | Salud técnica, fuera de `/api/v1`. | `200` `HealthResponse`; `503` Problem Details con `SERVICE_UNAVAILABLE`. |
| `POST /api/v1/appointments` | Alta y asignación (CU-002). | `201` `Appointment` + `Location`; `400`; `404`; `409`; `500`. |
| `GET /api/v1/appointments` | Listado y filtros (CU-003, CU-004). | `200` arreglo de `AppointmentSummary` (puede ser `[]`); `400`; `500`. |
| `GET /api/v1/appointments/{id}` | Detalle vigente (CU-005). | `200` `Appointment`; `400`; `404`; `500`. |
| `POST /api/v1/appointments/{id}/transitions` | Avance y cancelación (CU-006, CU-011). | `200` `Appointment`; `400`; `404`; `409`; `500`. |

Sin autenticación ni selección de ingenio: es un perfil exclusivamente local.

## DTOs principales

| Esquema | Rol |
|---|---|
| `CreateAppointmentRequest` | Alta con `carrierPhone`, `truckPlate`, `farmCode`, `cutAt` y `estimatedLoadTons`, todos obligatorios. Sin UUIDs ni ventana preferida; propiedades adicionales se rechazan. |
| `TransitionAppointmentRequest` | Sólo `newStatus`, el estado destino. |
| `Appointment` | Detalle: referencias a transportista, camión y finca, corte, carga, ventana, estado y creación. |
| `AppointmentSummary` | Elemento del listado: `id`, camión, ventana y estado. |
| `ProblemDetails` | Error en `application/problem+json` con `code` estable, `traceId` y `errors` opcional. |

## Estados

`ASIGNADO -> EN_CAMINO -> EN_ESPERA -> INGRESADO -> EN_DESCARGA -> FINALIZADO`, sólo de a un paso. `CANCELADO` se acepta únicamente desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`. `FINALIZADO` y `CANCELADO` son terminales. Cualquier otra combinación, incluido repetir el estado vigente o pedir `ASIGNADO`, responde `409 INVALID_TRANSITION`. Un turno activo es uno en estado no terminal.

## Semántica de filtros

- Sin `date`, **siempre** se usa la fecha local actual del ingenio sembrado, aunque se informen `status`, `truckPlate` o `phone`. `date` refiere a la fecha local del inicio de la ventana.
- Todos los filtros se combinan con AND; orden por inicio de ventana ascendente.
- Sin coincidencias, incluido un `phone` o patente inexistente: `200` con `[]`.
- `phone` usa E.164 con `+` y filtra por el transportista registrado en el turno. En la query string el `+` va como `%2B`.
- `truckPlate` ignora mayúsculas y separadores: sólo se comparan letras y dígitos.
- Parámetro no documentado, repetido o vacío: `400 VALIDATION_ERROR`.

| Solicitud | Resultado |
|---|---|
| `GET /api/v1/appointments` | Turnos de hoy. |
| `GET /api/v1/appointments?phone=%2B5493815550101` | Turnos de hoy de ese teléfono. |
| `GET /api/v1/appointments?status=EN_CAMINO` | Turnos de hoy en `EN_CAMINO`. |
| `GET /api/v1/appointments?date=2026-09-28&phone=%2B5493815550101` | Turnos de esa fecha de ese teléfono. |

## Errores

| `code` | HTTP | Cuándo |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Cuerpo, parámetro o filtro fuera de contrato. |
| `REFERENCE_NOT_FOUND` | 404 | Teléfono, patente o finca inexistente, inactivo o sin asociación. No indica cuál falló. |
| `APPOINTMENT_NOT_FOUND` | 404 | UUID de turno inexistente. |
| `ACTIVE_APPOINTMENT_EXISTS` | 409 | El camión ya tiene un turno no terminal. |
| `NO_CAPACITY` | 409 | No hay ventana futura sembrada con cupo. |
| `INVALID_TRANSITION` | 409 | Transición no permitida o estado cambiado concurrentemente. |
| `INTERNAL_ERROR` | 500 | Error inesperado, sin detalles internos. |
| `SERVICE_UNAVAILABLE` | 503 | Sólo `/health`: la API no puede atender la demo. |

`errors` es opcional; sus claves nombran propiedades del cuerpo o parámetros, y va ausente para errores no asociados a campos.

## Decisiones cerradas para la demo

- Fecha por defecto, colección vacía y parámetros extra, según la sección de filtros.
- Patente: resolución y filtro ignoran mayúsculas y separadores; la respuesta devuelve la forma canónica del seed.
- `farmCode`: resolución sin distinguir mayúsculas, sin otra normalización.
- `carrierPhone` y `phone`: E.164 con `+`, comparación exacta; no se relaja el formato.
- Fechas observables (`window`, `cutAt`, `createdAt`): RFC 3339 con el desplazamiento explícito del ingenio sembrado, `America/Argentina/Tucuman` en la demo. No es una regla del MVP completo.
- `estimatedLoadTons`: obligatorio, mayor que cero y en toneladas. Está alineado entre OpenAPI, OpenSpec, la API de Persona 3, la guía del chatbot y la del dashboard.
- Patrones verificados sobre el archivo real: `farmCode` acepta `FINCA-NORTE` y `F-01`; `carrierPhone` exige `+`. No se modificaron.

## Decisiones que siguen abiertas

No se implementan ni se infieren desde la demo; están en [decisiones abiertas](../planning/open-decisions.md):

- límites de `cutAt` futuro o antiguo y rangos o unidad definitiva de la carga (`OD-005`); la demo sólo valida formato, offset y carga positiva;
- prioridad (`OD-001`): la demo asigna la primera ventana futura con cupo;
- horizonte general de búsqueda y calendario (`OD-004`);
- idempotencia, concurrencia y reintentos (`OD-006`): reintentar un alta ya persistida responde `409 ACTIVE_APPOINTMENT_EXISTS` y repetir un estado responde `409`, sin que eso constituya una política;
- seguridad del canal n8n (`OD-008`) y fecha operativa y zona horaria del MVP (`OD-012`).

## Qué debe respetar Persona 2

- Resolver en el único ingenio sembrado: teléfono exacto en E.164, patente comparando sólo letras y dígitos sin distinguir mayúsculas, `farmCode` sin distinguir mayúsculas; exigir actividad y asociación camión-transportista.
- Sembrar teléfonos ficticios con `+`, patentes únicas también una vez normalizadas, códigos de finca únicos sin distinguir mayúsculas y compatibles con el patrón, y nombres no vacíos.
- Listado: fecha efectiva = `date` o, si falta, la fecha local actual del ingenio **siempre**, aun con otros filtros; comparar contra la fecha local del inicio de la ventana; AND; orden ascendente.
- Entregar fechas con el desplazamiento de la zona del ingenio sembrado, tomada del seed y no de constantes.
- No exponer controladores HTTP ni formas propias; los estados observables son los del enum de OpenAPI.

## Qué debe respetar Persona 3

Respuestas a los puntos de `persona3-api-handoff.md` (rama `feat/demo-appointments-api`):

- Fecha por defecto: sin `date` se usa hoy aun con otros filtros. Actualizar los comentarios de `AppointmentQuery` e `IAppointmentStore.ListAsync`, que hoy dicen "sin ningún filtro", y cubrirlo con una prueba.
- `cutAt` futuro o antiguo, horizonte, idempotencia y concurrencia siguen abiertos: no agregar límites.

Alineaciones pendientes con el contrato:

- Implementar `/health`: `200 {"status":"Healthy"}` en `application/json` y `503` en Problem Details con `SERVICE_UNAVAILABLE`. El `/healthcheck` heredado responde texto plano y no cumple.
- Para JSON mal formado o cuerpo ausente, omitir `errors` en lugar de usar la clave `body`; una propiedad no admitida puede informarse por su nombre.
- Verificar que un POST sin `Content-Type: application/json` responda en Problem Details.
- Escuchar en el puerto `5000` (el `launchSettings` heredado usa 5142) y permitir CORS para `http://localhost:5173`.
- Mantener el mapeo explícito de estados a texto; el `JsonStringEnumConverter` heredado no produce el enum del contrato.

## Qué deben respetar el dashboard (Persona 4) y n8n (Persona 5)

- Usar sólo estas operaciones con URL base configurable; desde Docker, `http://host.docker.internal:5000`.
- Enviar `carrierPhone` en E.164 con `+` y, en la query, `phone=%2B...`. **Persona 5 debe adaptar el chatbot:** la guía `docs/API_CONTRACT.md` de `agroflow-chatbot` usa hoy `5493815550123` sin `+`, que la API rechaza con `400`.
- Sin `date`, el listado muestra sólo hoy; un turno asignado para otra fecha requiere `date` o el detalle por `id`. No calcular fechas ni ventanas en el cliente.
- `cutAt` en RFC 3339 con segundos y desplazamiento; un `datetime-local` de HTML debe completarse antes de enviar. `estimatedLoadTons` como número positivo en toneladas.
- Mostrar horas según el desplazamiento recibido, sin asumir uno fijo.
- Ramificar por `code`, no por `title`; tratar claves desconocidas de `errors` como error genérico; ante `REFERENCE_NOT_FOUND`, pedir confirmar teléfono, patente y finca.
- Transiciones: enviar sólo el destino; ante `409`, volver a consultar el detalle y mostrar el estado de la API. Conservar el estado canónico aunque la vista agrupe estados.
- Tras un timeout de alta, consultar antes de informar falla: el reintento puede responder `409 ACTIVE_APPOINTMENT_EXISTS`.
- No agregar parámetros de consulta extra (por ejemplo, cache-busters en el polling).
- Usar el `id` del cuerpo del `201`: `Location` puede ser relativa y el navegador no la lee si CORS no la expone.
- No copiar los patrones al atributo HTML `pattern`: el de `farmCode` no compila con el flag `v` que usan los navegadores; la validación autoritativa es la de la API.
