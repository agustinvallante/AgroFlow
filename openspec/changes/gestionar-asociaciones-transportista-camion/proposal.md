# Gestionar asociaciones transportista-camión en LocalDemo

> **Estado: entrega acotada a `LocalDemo`.** Base: `main` en `78e3f8f`. Resuelve la gestión pendiente de `OD-002` y la publicación HTTP de asociaciones de `OD-013`. Desbloquea la tarea "Asociar con un camión" de [frontend-pendientes-mvp](../../../docs/planning/frontend-pendientes-mvp.md).

## Objetivo

Que un operador pueda asociar y desasociar camiones de un transportista desde el dashboard. Hoy un transportista dado de alta por la API no puede pedir turnos porque no hay forma de asociarle un camión.

## Decisiones aplicadas (aprobadas el 2026-11-16)

1. **Rutas**: `GET /api/v1/associations`, `POST /api/v1/associations` con `{ driverId, truckId }` y `POST /api/v1/associations/{id}/inactivation`. El listado hace falta para que el dashboard conozca el identificador que permite desasociar.
2. **Camión ya asociado**: se rechaza con `409 DUPLICATE_ASSOCIATION` sin cambiar la autorización vigente. Para reasignarlo, primero se desasocia.
3. **Varios camiones por transportista**: se permite. La regla limita sólo un transportista activo por camión.
4. **Desasociar**: inhabilita la asociación sin borrarla, como historial. Si el mismo par se vuelve a asociar, se reactiva esa fila (el esquema tiene un índice único por par).
5. **Permisos**: operador y supervisor (`OD-007`). En LocalDemo se usa la clave del dashboard, igual que en transportistas y camiones.
6. **Validaciones**: transportista y camión existentes en el ingenio y habilitados. Si no, `404 ASSOCIATION_REFERENCE_NOT_FOUND`.
7. **Turnos existentes**: no se modifican. Desasociar sólo impide pedir turnos nuevos con ese camión.
8. **Frontend**: en la vista Transportistas, cada fila permite asociar un camión libre y desasociar los camiones asociados, con confirmación. No hay una vista propia de camiones.
9. **Alcance**: sólo `LocalDemo`.

## Alcance

- `AssociationsController`, exclusivo de LocalDemo y protegido por `DashboardKey`, sobre `MasterDataService.CreateAssociation`, `SetAssociationActive` y `Associations`. Las mutaciones exigen `Idempotency-Key`.
- `MasterDataService.CreateAssociation` reactiva la asociación histórica del mismo par en vez de insertar otra.
- Respuesta del listado: `id`, `driverId`, `truckId` e `isActive`. Las fechas de alta y baja no se exponen porque todavía no se persisten (B02).
- OpenAPI `0.8.0` documenta las tres operaciones, los esquemas `Association` y `AssociationInput` y los códigos `ASSOCIATION_REFERENCE_NOT_FOUND` y `DUPLICATE_ASSOCIATION`.
- El proxy del dashboard reenvía `GET /api/v1/trucks` (sólo lectura) y `/api/v1/associations`.
- La vista Transportistas muestra los camiones de cada transportista, asocia un camión libre y desasocia con confirmación.

## Fuera de alcance

- Alta, edición e inhabilitación de camiones, y vista propia de camiones.
- Reactivación de transportistas.
- Identidad JWT por ingenio, permisos por rol y paginación del MVP.
- Persistir las fechas de alta y baja de asociaciones.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `datos-maestros` | Existente | [specs/datos-maestros/spec.md](specs/datos-maestros/spec.md) |
