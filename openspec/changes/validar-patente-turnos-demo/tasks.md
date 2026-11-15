# Tareas: validar la patente de los turnos de LocalDemo

## 1. Backend

- [x] 1.1 Definir la regla única `AppointmentService.IsValidTruckPlate`, con su mensaje, y aplicarla en `CreateAsync` y `ListAsync`. Verificación: `AppointmentServiceTests` (regla y rechazo previo al acceso al store).
- [x] 1.2 Aplicar la misma regla en `AppointmentsController`, en el listado y en el alta. Verificación: `AppointmentsHttpTests` (`errors.truckPlate` y el servicio sin invocar).
- [x] 1.3 Probar de punta a punta con SQLite real en LocalDemo: `400` en listado y alta, sin cambios en turnos ni en la ocupación, y normalización vigente de `af-123 bc`. Verificación: `TruckPlateValidationLocalDemoTests`. Contra `main` sin la corrección, esas pruebas obtienen `500` en el listado.

## 2. Contrato

- [x] 2.1 Documentar la regla en `truckPlate` (consulta y alta) de `docs/contracts/openapi.yaml`. Verificación: Redocly válido.

## 3. Revisión

- [ ] 3.1 Revisar la PR y consolidar los deltas en `openspec/specs/`.
