# Validar la patente de los turnos de LocalDemo

> **Estado: corrección acotada a `LocalDemo`.** Resuelve [#112](https://github.com/agustinvallante/AgroFlow/issues/112), hallazgo A05 de la auditoría de presentación del 2026-11-14. Base: `main` en `8873a26`.

## Problema

`GET /api/v1/appointments?truckPlate=------` respondía `500 INTERNAL_ERROR`. El controlador y el servicio sólo validaban la longitud (6 a 10 caracteres), y la normalización de la patente (`MasterDataValidation.NormalizeLettersAndDigits`) lanza una excepción cuando el valor queda vacío tras quitar los separadores. El alta `POST /api/v1/appointments` tenía el mismo hueco.

El cambio [`demo-local-turnos-basicos`](../demo-local-turnos-basicos/specs/turnos-consulta/spec.md) ya exige un error de validación para una patente fuera del formato de OpenAPI. Este cambio precisa ese formato y lo aplica en las dos operaciones.

## Alcance

- Una única regla de entrada para la patente, `AppointmentService.IsValidTruckPlate`: de 6 a 10 caracteres y al menos una letra o dígito. La aplican el controlador y el servicio, en el listado y en el alta.
- Una patente inválida responde `400 VALIDATION_ERROR` con `errors.truckPlate`, antes de consultar o escribir en SQLite.
- La normalización vigente no cambia: `af-123 bc` y `AF123BC` siguen siendo equivalentes.
- Las fallas inesperadas siguen respondiendo `500 INTERNAL_ERROR` sin detalles internos.
- OpenAPI documenta la regla en el parámetro `truckPlate` y en `CreateAppointmentRequest.truckPlate`. Las rutas, los esquemas y la versión del contrato no cambian.

## Fuera de alcance

- Las lecturas internas del MVP con JWT ([#106](https://github.com/agustinvallante/AgroFlow/pull/106)), que validan la patente en su propio controlador.
- La política temporal de corte de `OD-005`.
- El formato oficial de patentes argentinas. La regla sólo evita una normalización vacía.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `turnos-consulta` | Existente | [specs/turnos-consulta/spec.md](specs/turnos-consulta/spec.md) |
| `turnos-solicitud-y-asignacion` | Existente | [specs/turnos-solicitud-y-asignacion/spec.md](specs/turnos-solicitud-y-asignacion/spec.md) |
