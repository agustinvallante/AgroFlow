# Delta: Consulta de turnos

## ADDED Requirements

### Requirement: Patente válida en el filtro del listado de la demo local

En `LocalDemo`, el filtro `truckPlate` del listado de turnos SHALL tener entre 6 y 10 caracteres y al menos una letra o dígito. La comparación SHALL seguir ignorando mayúsculas y separadores. Un valor sin letras ni dígitos MUST responder `400 VALIDATION_ERROR` con el error asociado al campo `truckPlate`, MUST NOT consultar turnos y MUST NOT responder un error interno.

#### Scenario: Patente formada sólo por separadores

- **GIVEN** la demo local en ejecución
- **WHEN** se lista con `truckPlate=------`
- **THEN** la API responde `400 VALIDATION_ERROR` con `errors.truckPlate` y sin datos de turnos

#### Scenario: Patente con separadores y minúsculas

- **GIVEN** un turno del camión `AF123BC`
- **WHEN** se lista con `truckPlate=af-123 bc` en la fecha del turno
- **THEN** la API responde éxito e incluye ese turno, igual que con `AF123BC`
