# Tareas: listar camiones en LocalDemo

## 1. Backend

- [x] 1.1 `TrucksController` (`GET /api/v1/trucks`, `DashboardAccess`) sobre `MasterDataService.Trucks`, con el filtro `active` y el rechazo de parámetros no documentados, repetidos o vacíos. Verificación: `TrucksHttpTests`.
- [x] 1.2 Registrar el controlador como exclusivo de LocalDemo en `LocalDemoAppointmentsConvention`. Verificación: `MasterDataHttpTests` (404 fuera de LocalDemo) y `ConversationsHttpTests` (Swagger de LocalDemo con sólo `GET /api/v1/trucks`).
- [x] 1.3 Probar sobre SQLite real: camiones sembrados, filtro `active`, arreglo vacío, `401` sin clave, `400` por parámetro, sin mutaciones publicadas, falla cerrada con más de un ingenio y lectura sin efectos. Verificación: `TrucksHttpTests`.
- [x] 1.4 Probar el filtro por ingenio y actividad del repositorio. Verificación: `EfMasterDataUnitOfWorkTests`.

## 2. Contrato

- [x] 2.1 Documentar la operación `listarCamiones` y el esquema `Truck` en `docs/contracts/openapi.yaml` `0.6.0`. Verificación: Redocly válido.

## 3. Pendiente de B15 (#19)

- [ ] 3.1 Versión del MVP con JWT, permisos de operador y supervisor y paginación, después de B03 y del contrato de lecturas de #106.
- [ ] 3.2 Revisar la PR y consolidar el delta en `openspec/specs/`.
