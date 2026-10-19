# Reconciliar la base de PR #47 con el MVP vigente

## Objetivo y estado

Este cambio documenta la integración de ideas útiles de PR #47 con la estructura AgroFlow de main y la guía actualizada. El alcance elegido es una **fundación compatible, todavía no habilitada**: se conserva una base técnica revisada, pero no se publica CRUD de datos maestros ni se declara lista una API MVP.

Las especificaciones canónicas y ADR-004/005/006 mantienen su autoridad. Las decisiones pendientes permanecen como propuestas o dependencias; no se convierten en política operativa por este cambio.

## Fundamento recuperable y límites

Se preservan servicios y pruebas compatibles con AgroFlow, la persistencia académica SQLite y el frontend React/Vite. El `AgroFlowDbContext` conserva su esquema y migraciones actuales. Identity/JWT de main continúa como base, pero B00, B02, B03 y B05 siguen siendo dependencias para habilitar datos maestros y su segregación.

Las propiedades `CreatedAtUtc` e `InactivatedAtUtc` de cuatro entidades maestras no existen en las migraciones SQLite actuales. El modelo EF ignora explícitamente esos ocho campos no migrados: no son persistentes ni evidencian historial durable. B02 debe definir y migrar el esquema antes de habilitar esos datos.

La normalización de WhatsApp en el delta de datos maestros sigue pendiente de revisión OD-013. El acuerdo de normalizar DNI a solo dígitos sí permanece vigente en la spec canónica.

## Correcciones verificadas y evidencia

Dos consultas se corrigieron y se consolidaron en commits de trabajo:

1. `34f1c3ad624acdd244e96ca8e838aa3c920e98ff` (`fix(master-data): order associations by persisted identifier`) ordena `ListAssociations` por el `Id` persistido, sin prometer cronología ni modificar el esquema. Incluye las regresiones de UoW.
2. `25784be056b0c94d932325e146ff2092eae419de` (`fix(master-data): enforce truck tenant scope in projections`) comprueba `truck.IngenioId` además de conservar los filtros de asociación/transportista, excluyendo el vínculo inconsistente desde una asociación del ingenio A hacia un camión del ingenio B. Incluye las regresiones de proyección.

La verificación independiente del 2026-10-19 se ejecutó antes de esos commits, sobre HEAD `efe8c7c8c00d8faad5c4c912c6adf5e57b3d1ea1` más el diff local que contenía exactamente estos cambios: backend 209/209, frontend 37/37, OpenSpec 17/17 y OpenAPI válido. Los tres archivos fuente mantuvieron sus hashes verificados hasta el cierre de documentación (108 archivos con enlaces locales). No se volvió a ejecutar la suite completa sobre los SHAs de los commits. El build Release previo tuvo cero warnings y errores; Redocly emitió un warning sobre health sin 4XX, frontend lint cinco warnings y `npm ci` reportó un advisory alto.

El CI `38101125508` corresponde al SHA previo `efe8c7c`, no a estos commits. No hay CI nuevo. Main avanzó a `a6b38e2400fab10be9fd7fefb1e1d07496ee3ca6`; la PR figura `CONFLICTING`/`DIRTY` respecto de main. No se afirma integración ni validación contra el nuevo main; se conserva Draft y no se autoriza resolver el conflicto en esta etapa.

## Fuera de alcance

- Exponer rutas, DTO, seguridad, paginación o CRUD de catálogos en OpenAPI.
- Aprobar una política pública de `Idempotency-Key`, auditoría completa o permisos de asociación/capacidad.
- Completar el MVP, declarar B03/B38/G01–G12 terminados o atribuir persistencia durable a metadatos no migrados.
- Modificar ADRs, specs canónicas o decisiones abiertas por inferencia; archivar OD-013.
- Cambiar frontend de React/Vite, esquema SQLite, DI de servicios maestros o habilitar su registro.

## Aceptación documental

- [x] Documentar la fundación como compatible pero todavía no habilitada.
- [x] Mantener maestras fuera del OpenAPI activo hasta resolver B00/B02/B03/B05.
- [x] Distinguir ocho propiedades no persistentes de columnas migradas.
- [x] Mantener la normalización de WhatsApp como propuesta pendiente y el DNI como acuerdo vigente.
- [x] Revalidar documentación: OpenSpec 17/17, archived sin cambios, OpenAPI válido con 1 warning y enlaces locales válidos en 108 archivos.
- [x] Registrar resultados locales y distinguirlos del CI del SHA publicado.
