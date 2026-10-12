# Alineación documental de PR #47 con la guía MVP

> **Alcance:** T3/T5, documentación únicamente. Registra lo que los documentos
> afirman y los límites conocidos; no certifica backend, habilita CRUD ni declara
> aceptado el MVP. La fundación elegida es **compatible, todavía no habilitada**.

## Guía 1–9: resumen de reconciliación

| Guía | Alineación documental | Estado y límite |
|---|---|---|
| 1. Estructura | Priorizar `AgroFlow.sln`, proyectos `AgroFlow.*`, API/persistencia y retiro ecommerce de main. Conservar menciones históricas intencionales en detector/runbook/tests; no restaurarlas como dominio activo. | B01 de main ya retiró rutas ecommerce; este recorte no cambia backend ni CI. |
| 2. Acuerdos | Una asociación activa por camión; ventana automática compatible, sin preferencia; corte y carga obligatorios; capacidad/duración por ingenio desde datos de arranque; catálogos para operador/supervisor conforme a specs. | Demo 30 min/cupo 2 son datos de seed, no constantes. Prioridad aprobada no redondea por minuto. Campos, gestión externa y permisos adicionales pendientes. |
| 3. Persistencia | SQLite académico conforme a ADR-006, PostgreSQL como destino futuro y WAL condicionado a prueba. Un único modelo operativo para turnos. | Las propiedades `CreatedAtUtc`/`InactivatedAtUtc` de cuatro entidades maestras no existen como columnas en migraciones SQLite main; corrección backend las ignora explícitamente (ocho campos). No son persistentes ni evidencia de auditoría/historial durable. B02 debe definir migración antes de habilitar. |
| 4. Asociación | Se conserva cardinalidad de una asociación activa por camión y su historial conceptual. | Alta/reactivación, pertenencia, DTO/contrato externo y reglas de concurrencia dependen de OD-007/OD-013/B00; este trabajo no las publica. |
| 5. Seguridad | Mantener Identity/JWT seguro de main y backend como autoridad. El perfil `LocalDemo` sin autenticación es solo demo de turnos. | No se agrega bypass de maestros ni cliente público; no se decide sesión web, permisos de asociación/capacidad o matriz completa. |
| 6. SQLite | Diferenciar restricciones, FK, concurrencia y bloqueo; WAL no resuelve transacciones ni idempotencia. | OD-006 permanece abierto; la existencia de `IdempotencyRecord` no aprueba `Idempotency-Key`. Una entidad de auditoría no completa OD-009. Sin afirmación de recorridos de carrera/restauración probados. |
| 7. Contrato | OpenAPI conserva versión `0.3.0`, salud y operaciones demo de appointments, esquemas/status/filtros/errores, `AppointmentProblemDetails` y `security: []` por operación demo. | Se retiran paths/DTO/headers propuestos de maestros del contrato activo; el borrador se conserva en [`master-data-foundation-proposal.md`](../contracts/master-data-foundation-proposal.md). B38, CRUD HTTP y MVP no se declaran completos. |
| 8. Frontend | React/Vite se mantiene por ADR-005. Alta/lista/filtros/detalle/transiciones de turnos pueden usar HTTP de demo; otras vistas permanecen mock. | Conservar la composición, ApiClient, DTO/mappers, repositorio HTTP y pruebas existentes. No se exige completar todas las pantallas ni adoptar Next/BFF/TanStack/Zustand. |
| 9. Evidencia | Preservar suites existentes y distinguir check documental del estado general del merge. | La evidencia independiente comunicada por el padre: 177/205 tests backend ejecutados, 28 fallas y una prueba de lock excluida por colgar. No es resultado de esta unidad ni evidencia de backend passing. G01–G12 y MVP completo no se declaran. |

## Decisiones y dependencias que siguen abiertas

- **B00/B02/B03/B05:** decisiones de alcance/API, esquema y migraciones de datos
  maestros, Identity/contexto segregado y habilitación de la fundación.
- **OD-006:** concurrencia, idempotencia y reintentos; no se hace obligatorio un
  encabezado o DTO por la implementación interna existente.
- **OD-007/OD-013:** gestión externa de asociación, campos/validaciones y
  permisos restantes; normalización WhatsApp se conserva como propuesta pendiente.
- **OD-009:** qué eventos, actor, conservación y acceso constituyen auditoría.
  `AuditEvent` por sí solo no acredita una política completa.
- **OD-011/OD-012:** operación/recuperación; fórmulas, fecha/huso, horizonte y
  cadencia de métricas/dashboard.
- **Sesión web:** no se selecciona BFF, cookie opaca, token en navegador ni sesión
  directa del backend. Requiere decisión explícita de custodia, amenazas,
  contrato y operación.
- **Rutas, DTO y paginación:** no aparecen en OpenAPI activo hasta aprobación.

El DNI solo dígitos sí es un acuerdo vigente de la spec canónica de datos
maestros y queda preservado en OD-013. El alcance de esta unidad no modifica
especificaciones canónicas ni resuelve otras decisiones pendientes.

## Revisión y verificación

Orden de revisión recomendado: OpenAPI y su propuesta no normativa; README/docs
de estado actual; ADR-005/006 y propuestas frontend supersedidas; OpenSpec del
cambio documental; luego los restantes conflictos documentales.

Los enlaces, OpenSpec, OpenAPI y checks de whitespace se reportan por resultado
observado en el handoff. La verificación de esta unidad no sustituye la suite
backend independiente ni certifica el candidato completo del merge.
