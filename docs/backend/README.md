# Guías del backend

Aquí se documentarán aspectos exclusivos de la implementación de la API: ejecución local, persistencia, migraciones, estructura interna, configuración y despliegue.

Las reglas de negocio no se duplican aquí. Deben consultarse en `../../openspec/specs/`; el contrato HTTP vive en `../contracts/openapi.yaml` y las decisiones arquitectónicas en `../architecture/decisions/`.

## Estado actual

La solución .NET 8 aún conserva módulos de comercio electrónico (`Product`, `Order`, `Customer`) y su configuración SQL Server/Identity. También contiene una porción nueva de AgroFlow: entidades de turnos, ventanas y catálogos, migraciones/seed SQLite y las cuatro operaciones HTTP de turnos de la demo, incorporadas por las PR [#50](https://github.com/agustinvallante/AgroFlow/pull/50) y [#61](https://github.com/agustinvallante/AgroFlow/pull/61). En `Program.cs`, `LocalDemo:Enabled` activa esa API y su persistencia **sólo para la demo local**, sin autenticación. No es el perfil MVP ni habilita por sí mismo segregación entre ingenios.

La [hoja de ruta](../planning/implementation-roadmap.md) y la [replanificación tras la demo](../planning/mvp-replan-2026-09-29.md) detallan el trabajo restante. `B01` retira el dominio heredado; `B02` extiende modelo/persistencia sin perder lo reutilizable; `B03`–`B05` agregan identidad, errores, pruebas, concurrencia, idempotencia y auditoría; `B25`–`B29` amplían las operaciones de turnos existentes. No crear endpoints paralelos por los nombres antiguos de las issues: las rutas de demo aprobadas están en OpenAPI y cualquier cambio se tramita con OpenSpec.

La [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) elige SQLite para el MVP, pero el código existente sólo lo utiliza en el perfil `LocalDemo`. `B02` debe preparar la persistencia MVP con migraciones, datos por ingenio y pruebas; WAL se habilitará únicamente si la evaluación de concurrencia lo justifica, y respaldo/restauración deben ensayarse. SQL Server pertenece al código heredado y no es el motor objetivo del MVP. La [ADR-003](../architecture/decisions/ADR-003-backend-fuente-de-verdad.md) mantiene las reglas operativas sólo en el backend.
