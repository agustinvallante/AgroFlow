# Delta: Ciclo de vida de turnos

## ADDED Requirements

### Requirement: Hora de partida de un turno

ZafraFlow SHALL registrar la hora de partida de un turno en la misma operación que persiste su transición a `EN_CAMINO`, y las transiciones posteriores MUST NOT modificarla. En la demo local, la representación del turno y su resumen en el listado SHALL incluir `departedAt` en RFC 3339 con el desplazamiento del ingenio, o `null` si el turno nunca partió. Los turnos que ya estaban `EN_CAMINO` antes de registrarse este dato SHALL informar `null`. Las lecturas del perfil MVP MUST NOT cambiar su forma.

#### Scenario: Salida informada

- **GIVEN** un turno `ASIGNADO` con `departedAt` en `null`
- **WHEN** el turno pasa a `EN_CAMINO`
- **THEN** ZafraFlow responde con `departedAt` igual a la hora de la transición, también en el detalle y en el listado

#### Scenario: Llegada posterior

- **GIVEN** un turno `EN_CAMINO` con su hora de partida registrada
- **WHEN** el turno pasa a `EN_ESPERA` y luego a `INGRESADO`
- **THEN** `departedAt` conserva la hora de partida original

#### Scenario: Turno en camino anterior al cambio

- **GIVEN** un turno que ya estaba `EN_CAMINO` antes de existir la hora de partida
- **WHEN** se consulta después de la migración
- **THEN** ZafraFlow conserva el turno y lo informa con `departedAt` en `null`

### Requirement: Detenciones de un turno

En la demo local, la representación del turno y su resumen en el listado SHALL incluir `stops`: los incidentes en ruta resueltos del turno, cada uno como `{ startedAt, endedAt }` con la hora del reporte y la de su resolución en RFC 3339 con el desplazamiento del ingenio, ordenados por `startedAt`. SHALL incluir los incidentes resueltos automáticamente al salir de `EN_CAMINO`. El incidente abierto MUST NOT figurar en `stops`: se informa en `openIncident`. Sin incidentes resueltos, `stops` SHALL ser un arreglo vacío.

#### Scenario: Pinchazo resuelto

- **GIVEN** un turno `EN_CAMINO` con un incidente abierto
- **WHEN** se informa su resolución
- **THEN** ZafraFlow responde con `openIncident` en `null` y una detención cuyo `startedAt` es la hora del reporte y cuyo `endedAt` es la de la resolución

#### Scenario: Dos detenciones

- **GIVEN** un turno `EN_CAMINO` que tuvo dos incidentes resueltos
- **WHEN** se consulta el turno o el listado
- **THEN** `stops` contiene ambas detenciones ordenadas por `startedAt`

#### Scenario: Llegada con incidente abierto

- **GIVEN** un turno `EN_CAMINO` con un incidente abierto
- **WHEN** el turno pasa a `EN_ESPERA`
- **THEN** el incidente figura en `stops` con `endedAt` igual a la hora de la transición

#### Scenario: Incidente todavía abierto

- **GIVEN** un turno `EN_CAMINO` con un incidente abierto y sin incidentes resueltos
- **WHEN** se consulta el turno
- **THEN** `stops` es un arreglo vacío y el incidente se informa en `openIncident`
