# Listar camiones en LocalDemo

> **Estado: primera entrega incremental de [B15 #19](https://github.com/agustinvallante/AgroFlow/issues/19), acotada a `LocalDemo`.** Base: `main` en `8873a26`.

## Objetivo

Que el dashboard pueda consultar los camiones del ingenio (patente, tipo de flota y si está habilitado) con el backend como fuente de verdad. Es el paso previo para gestionar camiones (F03 [#69](https://github.com/agustinvallante/AgroFlow/issues/69)) y para consultar asociaciones (B38 [#43](https://github.com/agustinvallante/AgroFlow/issues/43)).

## Decisiones aplicadas

- [OD-007](../../../docs/planning/open-decisions.md): operador y supervisor pueden listar camiones de su ingenio. En LocalDemo esa identidad todavía no existe: se reutiliza la clave del dashboard (`DashboardKey`), igual que el listado de transportistas de [`persistir-conversaciones-whatsapp`](../persistir-conversaciones-whatsapp/proposal.md).
- [OD-013](../../../docs/planning/open-decisions.md): los camiones tienen listado, alta, edición e inhabilitación en el MVP. Este cambio sólo publica el listado, porque los campos editables siguen pendientes.

## Alcance

- `GET /api/v1/trucks`, exclusivo de LocalDemo y protegido por `DashboardKey`. Fuera de LocalDemo la ruta no existe.
- Filtro opcional `active` (`true` o `false`). Sin el filtro se listan habilitados e inhabilitados.
- Respuesta: `id`, `plate`, `fleetType` e `isActive`, ordenada por patente. Sin coincidencias, un arreglo vacío.
- Reutiliza `MasterDataService.Trucks` y `EfMasterDataUnitOfWork.ListCamiones`, que filtran por ingenio y actividad en SQL. No agrega reglas de negocio.
- OpenAPI `0.6.0` documenta la operación y el esquema `Truck`.

## Fuera de alcance

- Alta, edición, inhabilitación y detalle de camiones (B16–B18): campos editables pendientes en `OD-013`.
- Asociaciones transportista–camión (B23, B24, B38): gestión pendiente en `OD-007` y `OD-013`.
- Identidad JWT por ingenio, permisos por rol y paginación del MVP: dependen de B03 y del contrato de lecturas de [#106](https://github.com/agustinvallante/AgroFlow/pull/106).
- Consumo desde el frontend: el proxy del dashboard sólo reenvía las rutas que ya consume, y la pantalla corresponde a F03 #69.

## Cobertura de B15

| Criterio de #19 | Esta entrega | Pendiente |
|---|---|---|
| Consulta autorizada que devuelve sólo registros visibles; respuesta vacía estable y documentada | Sí, en LocalDemo: clave del dashboard, ingenio único sembrado y arreglo vacío documentado | Autorización por rol de operador y supervisor con JWT (B03) |
| Filtros y paginación de OD-013, aplicados antes de materializar | Filtro `active` en SQL | Paginación y filtros adicionales, que OD-013 todavía no define |
| Sin permiso o de otro ingenio no revela datos; ninguna consulta modifica estado | Sin clave responde `401`. Con más de un ingenio, LocalDemo falla con un error genérico, sin datos. El repositorio filtra por ingenio (prueba con SQLite real). La lectura no modifica datos | Prueba HTTP con dos ingenios autenticados, cuando exista la identidad del MVP |

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `datos-maestros` | Existente | [specs/datos-maestros/spec.md](specs/datos-maestros/spec.md) |
