# Listar ventanas y cupos en LocalDemo

> **Estado: primera entrega incremental de [B39 #44](https://github.com/agustinvallante/AgroFlow/issues/44), acotada a `LocalDemo`.** Base: `main` en `75d8b27`. Autorizada por Agustín en la [confirmación de la issue #44](https://github.com/agustinvallante/AgroFlow/issues/44#issuecomment-6905858031), que prevalece sobre la propuesta previa.

## Objetivo

Que el dashboard pueda consultar las ventanas de recepción del ingenio para una fecha, incluidas las franjas vacías, con su capacidad, su ocupación y los cupos restantes calculados por el backend. Alimenta la cola visual de F07 [#73](https://github.com/agustinvallante/AgroFlow/issues/73) sin que el frontend calcule disponibilidad.

## Decisiones aplicadas

Las cuatro decisiones fueron propuestas y confirmadas en la issue; ninguna cierra [OD-004 ni OD-012](../../../docs/planning/open-decisions.md), que siguen parcialmente resueltas.

1. **Ruta:** `GET /api/v1/windows`, coherente con `/api/v1/trucks`, `/api/v1/drivers` y `/api/v1/farms`.
2. **Fecha:** `date=YYYY-MM-DD` opcional. Sin ella se usa el día local actual del ingenio, de la medianoche local incluida a la siguiente excluida, igual que el listado de turnos. Se consultan las ventanas persistidas de esa fecha; sin ventanas, `200` con `[]`.
3. **Calendario:** se reutilizan las ventanas existentes y el seed de demo (hoy y mañana, 08:00–18:00, intervalos de 30 minutos, cupo 2). Son datos de arranque, no reglas del dominio. No hay generación dinámica, configuración editable ni horizonte nuevo.
4. **Interrupciones:** la respuesta no informa indisponibilidad porque depende de B33 [#37](https://github.com/agustinvallante/AgroFlow/issues/37). Se documenta como limitación, sin simular el comportamiento.

## Alcance

- `GET /api/v1/windows`, exclusivo de LocalDemo y protegido por `DashboardKey` (`DashboardAccess`). Fuera de LocalDemo la ruta no existe. La clave del canal de n8n no sirve como clave del dashboard.
- Parámetro opcional `date`. Se rechazan fechas inválidas y parámetros desconocidos, repetidos o vacíos con `400 VALIDATION_ERROR`.
- Respuesta: `id`, `startAt`, `endAt`, `capacity`, `occupied` y `available`, ordenada por inicio, con las fechas en el desplazamiento horario del ingenio, como el contrato de turnos.
- `occupied` es `Ventana.Occupied`, el contador persistido que ya se incrementa al asignar un turno y se decrementa al cancelarlo. `available = capacity - occupied` se calcula en el backend.
- Una única consulta de lectura en `EfAppointmentStore`, que expone `IWindowReader`, igual que `IAppointmentReader` para las lecturas del MVP. Sin servicio ni repositorio nuevos, sin migraciones y sin escrituras.
- OpenAPI `0.7.0` documenta la operación `listarVentanas` y el esquema `Window`. El contrato separado del perfil MVP (`openapi.mvp-appointment-reads.yaml`) no cambia.

## Fuera de alcance

- Alta, edición o baja de ventanas, generación dinámica de calendario y cambios de capacidad desde la interfaz.
- Indisponibilidad por interrupciones (B33 #37) e indicadores del dashboard (B35).
- Identidad JWT, roles por ingenio y paginación del MVP.
- Cálculo de cupos en el frontend, la grilla de la cola y el proxy del dashboard: se coordinan con F07 #73.

## Cobertura de B39

| Criterio de #44 | Esta entrega | Pendiente |
|---|---|---|
| Franjas vacías con capacidad completa y ocupadas con cupo restante exacto | Sí, sobre SQLite real | — |
| Solicitar, cancelar o reprogramar modifica la ocupación sin mezclar ingenios | Solicitar y cancelar se reflejan en la lectura siguiente. El filtro por ingenio se prueba con SQLite real a nivel repositorio | Reprogramación (todavía no existe en LocalDemo) y prueba HTTP con dos ingenios autenticados |
| Una interrupción identifica ventanas indisponibles; la lectura no modifica datos | La lectura no modifica datos | Indisponibilidad por interrupción (B33 #37) |

Siguen pendientes para el MVP: JWT y roles por ingenio, calendario configurable, indisponibilidad por interrupciones y pruebas HTTP con dos ingenios.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `monitoreo-operativo` | Existente | [specs/monitoreo-operativo/spec.md](specs/monitoreo-operativo/spec.md) |
