## MODIFIED Requirements

### Requirement: Estados cancelables (RN-050, RN-051)

AgroFlow SHALL permitir a operadores y supervisores autorizados del ingenio cancelar turnos solo desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA` y MUST rechazarla desde `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO`. Los demás permisos de cancelación continúan sujetos a `OD-007`.

#### Scenario: Cancelación permitida

- **GIVEN** un turno en `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`
- **WHEN** un actor autorizado solicita cancelarlo
- **THEN** AgroFlow persiste el estado `CANCELADO`

#### Scenario: Cancelación fuera de término

- **GIVEN** un turno en `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO`
- **WHEN** se solicita cancelarlo
- **THEN** AgroFlow rechaza la operación y conserva el estado
