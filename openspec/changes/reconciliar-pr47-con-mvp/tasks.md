# Tareas documentales de reconciliación PR #47

## Alineación

- [x] Registrar la fundación compatible pero todavía no habilitada; conservar dependencias B00/B02/B03/B05.
- [x] Mantener rutas y DTO de maestros fuera del contrato OpenAPI activo.
- [x] Distinguir ocho propiedades CLR no migradas de datos persistentes y aclarar que no prueban auditoría durable.
- [x] Conservar DNI de solo dígitos como acuerdo canónico y SQLite académico según OD-014/ADR-006.
- [x] Marcar normalización de WhatsApp como propuesta pendiente, no decisión aprobada.
- [x] Alinear React/Vite y separar HTTP de turnos demo de vistas mock.

## Lista de asociaciones

- [x] Ordenar `ListAssociations` por `Id` persistido, sin promesa cronológica ni cambio de esquema.
- [x] Añadir regresiones en SQLite migrada para lista vacía, actividad, aislamiento por ingenio, orden y repetición.
- [x] Observar RED de la consulta anterior: SQLite rechazó ordenar por `CreatedAtUtc` no mapeado.
- [x] Verificación focalizada previa: 8 pasaron, 0 fallaron, 0 omitidos. La verificación completa posterior se registra abajo.

## Proyección de camiones por ingenio

- [x] Filtrar también por el ingenio propio del camión y conservar filtros de asociación, transportista y asociación activa.
- [x] Añadir regresión SQLite migrada para excluir el enlace inconsistente de asociación A a camión B.
- [x] Verificación focalizada previa: 11 pasaron, 0 fallaron, 0 omitidos. La suite completa posterior se registra abajo.

## Estado y validación observados

- [x] Verificación independiente del 2026-10-19 sobre HEAD `efe8c7c` más diff local: backend 209/209, frontend 37/37, OpenSpec 17/17, Redocly válido y enlaces locales en 107 archivos.
- [x] Consolidar las correcciones probadas en commits `34f1c3ad624acdd244e96ca8e838aa3c920e98ff` (orden de asociaciones + pruebas UoW) y `25784be056b0c94d932325e146ff2092eae419de` (tenant en proyecciones + pruebas de proyección); los hashes fuente se mantuvieron idénticos, sin afirmar rerun completo sobre esos commits.
- [x] Registrar que el CI disponible corresponde al SHA publicado `efe8c7c`, anterior a los commits de corrección; no hay CI nuevo.
- [x] Reconciliar propuesta, diseño y matriz de guía sin modificar specs canónicas ni archivar OD-013; main actual avanzó a `a6b38e2400fab10be9fd7fefb1e1d07496ee3ca6` y la PR reporta conflicto `CONFLICTING`/`DIRTY`, sin integración ni verificación contra ese main.

## Dependencias que siguen pendientes

- [ ] Resolver B00/B02/B03/B05 antes de habilitar una API maestra.
- [ ] Aprobar o rechazar normalización de WhatsApp en OD-013 antes de consolidarla en la spec canónica.
- [ ] Resolver dependencias funcionales de B38 y evidencia de aceptación G01–G12; no se declaran completas.
- [ ] Decidir tamaño de PR (recorte, división o excepción) con mantenedor antes de Ready; no hay excepción aprobada.
- [ ] Publicar solo con autorización posterior; actualizar SHA y CI únicamente después de publicación autorizada.
