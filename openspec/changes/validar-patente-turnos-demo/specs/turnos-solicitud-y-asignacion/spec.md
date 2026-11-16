# Delta: Solicitud y asignación de turnos

## ADDED Requirements

### Requirement: Patente válida en el alta de la demo local

En `LocalDemo`, el campo `truckPlate` del alta de turnos SHALL tener entre 6 y 10 caracteres y al menos una letra o dígito. Un valor sin letras ni dígitos MUST responder `400 VALIDATION_ERROR` con el error asociado al campo `truckPlate`, antes de resolver referencias. MUST NOT crear turnos, ocupar cupos ni responder un error interno.

#### Scenario: Alta con patente formada sólo por separadores

- **GIVEN** un transportista, una finca y ventanas con cupo en la demo local
- **WHEN** se solicita un turno con `truckPlate` igual a `------` y los demás campos válidos
- **THEN** la API responde `400 VALIDATION_ERROR` con `errors.truckPlate`, y la cantidad de turnos y la ocupación de las ventanas no cambian
