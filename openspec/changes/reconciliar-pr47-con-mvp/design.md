# Diseño documental de la reconciliación PR #47

## Enfoque

La reconciliación distingue capacidades aprobadas, fundación técnica reutilizable aún no habilitada y propuestas pendientes. No crea una capacidad duplicada ni trata la persistencia de demo como esquema completo del MVP.

## Base y compatibilidad

- La API operativa de turnos conserva `AgroFlowDbContext`, el esquema SQLite y las migraciones actuales. ADR-006 define SQLite académico; WAL depende de validación y PostgreSQL corresponde a una etapa futura antes de operar con un cliente real.
- Identity/JWT de main es una base existente, no Identity MVP ni contexto de ingenio completo.
- El frontend permanece en React/Vite por ADR-005. La cola de turnos tiene HTTP de demo; otras vistas siguen mock.
- Los contratos de datos maestros no se publican hasta resolver B00, B02, B03, B05 y las decisiones propietarias.
- Se conserva `[NonController]` y no se registra el servicio maestro en DI. La fundación permanece deshabilitada.

## Esquema maestro y metadatos

Las cuatro entidades maestras tienen propiedades CLR `CreatedAtUtc` e `InactivatedAtUtc`, pero las tablas SQLite actuales no tienen esas columnas. El modelo ignora explícitamente los ocho campos hasta que B02 acuerde y migre el esquema. No son persistentes y no pueden servir como evidencia de historial durable o auditoría. No se cambia el esquema ni el snapshot.

## Consultas corregidas

- La lista de asociaciones usa el `Id` persistido para su orden determinista; no establece cronología.
- La proyección de camiones filtra por `truck.IngenioId` además de respetar filtros existentes de asociación y transportista. La regresión SQLite migrada excluye un enlace inconsistente A→camión B.

Las pruebas cubren lista vacía, actividad, tenant, orden repetible y la proyección de la asociación inconsistente. No implican una garantía general de integridad de datos ni prueban el contrato HTTP.

## Seguridad, asociaciones y auditoría

La seguridad efectiva permanece en Identity/JWT/backend y requiere completar B03. No se selecciona arquitectura de sesión web. La asociación activa única por camión es regla vigente, pero su CRUD externo, DTO y permisos siguen pendientes. La entidad `AuditEvent` no resuelve política de auditoría, eventos, actor, conservación o acceso de OD-009.

La entidad/servicio de idempotencia no aprueba un encabezado público, semántica de reintentos ni clave obligatoria. OD-006 sigue abierto.

## Propuesta de normalización de WhatsApp

El delta de `datos-maestros` se conserva como propuesta revisable, no como requisito consolidado. OD-013 deja pendientes campos y validaciones; no aplicar normalización de WhatsApp como comportamiento normativo sin aprobación explícita. El acuerdo DNI solo dígitos sí está vigente en la spec canónica.

## Evidencia y procedencia

Verificador independiente `mvflj67n-9-ia7f`, 2026-10-19 18:37:12–18:40:16 UTC, comprobó el HEAD `efe8c7c8c00d8faad5c4c912c6adf5e57b3d1ea1` más el diff local que contenía las dos correcciones. Restore y build Release pasaron (build: 0 warnings/0 errors); backend 209/209 sin fallas ni omitidos; frontend 37/37, typecheck/lint/build pasaron. OpenSpec estricto 17/17; archived sin cambios; Redocly válido con warning de health sin 4XX; enlaces locales 107 archivos.

Ese código se consolidó después en `34f1c3ad624acdd244e96ca8e838aa3c920e98ff` (`fix(master-data): order associations by persisted identifier`) y `25784be056b0c94d932325e146ff2092eae419de` (`fix(master-data): enforce truck tenant scope in projections`). Los hashes de las tres fuentes siguieron idénticos hasta el cierre documental, pero la suite completa no se volvió a ejecutar en esos commits. El CI `38101125508` corresponde únicamente al SHA anterior `efe8c7c`; no hay CI nuevo para los fixes. Avisos no remediados: cinco warnings de lint y un advisory alto de npm. Ninguno de estos resultados acredita CRUD/MVP, B03/B38 ni G01–G12.

En la consulta posterior, main estaba en `a6b38e2400fab10be9fd7fefb1e1d07496ee3ca6` y GitHub reportó la PR en conflicto (`CONFLICTING`/`DIRTY`). No se afirma validación o integración contra ese main. Tras actualizar estos documentos, OpenSpec estricto pasó 17/17, archived no encontró cambios archivados, Redocly validó OpenAPI con un warning y enlaces locales pasaron en 108 archivos; whitespace también pasó. Esa comprobación documental no sustituye las suites backend/frontend ni resuelve el conflicto.
