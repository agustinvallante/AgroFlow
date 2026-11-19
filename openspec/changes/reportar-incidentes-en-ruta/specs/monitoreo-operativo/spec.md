# Delta: Monitoreo operativo

## ADDED Requirements

### Requirement: Incidentes en ruta visibles para el operador

El frontend SHALL mostrar el incidente abierto que informa ZafraFlow API en la cola de turnos (tipo, si puede seguir y demora estimada) y en el mapa, donde el camión del turno SHALL figurar con demora y su detalle SHALL indicar el problema. Al resolverse el incidente, ambas vistas SHALL volver a mostrar el turno sin incidente en la siguiente lectura. El frontend MUST NOT deducir ni modificar el estado del turno a partir del incidente.

#### Scenario: Camión con pinchazo en el mapa

- **GIVEN** un turno `EN_CAMINO` con un incidente `PINCHAZO` abierto
- **WHEN** el operador abre la cola de turnos o el mapa
- **THEN** ve el incidente en la fila del turno y el camión con demora en el mapa, con el problema en su detalle

#### Scenario: Incidente resuelto

- **GIVEN** un turno cuyo incidente se resolvió
- **WHEN** el frontend vuelve a leer los turnos
- **THEN** el turno se muestra sin incidente
