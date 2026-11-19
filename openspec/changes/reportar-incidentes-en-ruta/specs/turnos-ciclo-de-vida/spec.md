# Delta: Ciclo de vida de turnos

## ADDED Requirements

### Requirement: Incidente en ruta de un turno en camino

ZafraFlow SHALL permitir registrar un incidente en ruta sobre un turno `EN_CAMINO`, con tipo `PINCHAZO`, `FALLA_MECANICA`, `ACCIDENTE` u `OTRO`, si el camión puede seguir viaje, una demora estimada entera entre 0 y 1440 minutos y un detalle opcional de hasta 500 caracteres. Un turno MUST NOT tener más de un incidente abierto. Registrar un incidente MUST NOT cambiar el estado ni la ventana del turno. La representación del turno SHALL incluir el incidente abierto en `openIncident`, o `null` si no hay ninguno.

#### Scenario: Pinchazo durante el viaje

- **GIVEN** un turno `EN_CAMINO` sin incidente abierto
- **WHEN** se reporta un incidente `PINCHAZO`, que puede seguir viaje, con 30 minutos de demora estimada
- **THEN** ZafraFlow responde `201` con el turno, su mismo estado y ventana, y `openIncident` con esos datos y la hora del reporte

#### Scenario: Turno que no está en camino

- **GIVEN** un turno `ASIGNADO`
- **WHEN** se reporta un incidente
- **THEN** ZafraFlow responde `409 INCIDENT_NOT_ALLOWED` y no registra nada

#### Scenario: Segundo incidente abierto

- **GIVEN** un turno `EN_CAMINO` con un incidente abierto
- **WHEN** se reporta otro incidente
- **THEN** ZafraFlow responde `409 OPEN_INCIDENT_EXISTS` y conserva el incidente existente

#### Scenario: Datos inválidos

- **GIVEN** un turno `EN_CAMINO`
- **WHEN** se reporta un incidente con un tipo desconocido o una demora fuera de rango
- **THEN** ZafraFlow responde `400 VALIDATION_ERROR` con el campo afectado

### Requirement: Resolución de un incidente en ruta

ZafraFlow SHALL marcar como resuelto el incidente abierto de un turno cuando se informa su resolución, conservando el registro con la hora de resolución. Si el turno no tiene incidente abierto, MUST responder `409 NO_OPEN_INCIDENT`. Cuando un turno sale de `EN_CAMINO` por cualquier transición, su incidente abierto SHALL quedar resuelto automáticamente.

#### Scenario: Neumático reparado

- **GIVEN** un turno `EN_CAMINO` con un incidente `PINCHAZO` abierto
- **WHEN** se informa la resolución
- **THEN** ZafraFlow responde `200` con el turno y `openIncident` en `null`

#### Scenario: Llegada con incidente abierto

- **GIVEN** un turno `EN_CAMINO` con un incidente abierto
- **WHEN** el turno pasa a `EN_ESPERA`
- **THEN** el incidente queda resuelto y el turno se muestra con `openIncident` en `null`
