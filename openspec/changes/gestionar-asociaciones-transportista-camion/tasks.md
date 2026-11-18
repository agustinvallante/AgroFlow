# Tareas: gestionar asociaciones transportista-camión en LocalDemo

## 1. Backend

- [x] 1.1 `AssociationsController` (`GET`, `POST /api/v1/associations` y `POST /api/v1/associations/{id}/inactivation`, `DashboardAccess`) sobre `MasterDataService`, con `Idempotency-Key` y validación estricta del cuerpo. Verificación: `AssociationsHttpTests`.
- [x] 1.2 Registrar el controlador como exclusivo de LocalDemo en `LocalDemoAppointmentsConvention`.
- [x] 1.3 `MasterDataService.CreateAssociation` reactiva la asociación inactiva del mismo par. Verificación: `AssociationsHttpTests.Re_associating_the_same_pair_reactivates_the_historical_association`.

## 2. Contrato

- [x] 2.1 Documentar `listarAsociaciones`, `asociarCamion` y `desasociarCamion`, los esquemas y los códigos nuevos en `docs/contracts/openapi.yaml` `0.8.0`. Verificación: Redocly válido.

## 3. Frontend

- [x] 3.1 Proxy del dashboard: `GET /api/v1/trucks` y `/api/v1/associations`. Verificación: `dashboardProxy.test.ts`.
- [x] 3.2 `HttpAsociacionRepository` y casos de uso `ObtenerFlotaAsociada`, `AsociarCamion` y `DesasociarCamion`. Verificación: `HttpAsociacionRepository.test.ts`.
- [x] 3.3 Vista Transportistas: asociar un camión libre y desasociar con confirmación. Verificación: `TransportistasView.test.tsx`.

## 4. Documentación

- [x] 4.1 Actualizar `OD-002`, `OD-013` y `frontend-pendientes-mvp.md`.
- [ ] 4.2 Revisar la PR y consolidar el delta en `openspec/specs/`.
