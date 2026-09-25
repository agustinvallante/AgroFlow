## MODIFIED Requirements

### Requirement: Consulta autorizada del transportista (RN-017)

AgroFlow MUST permitir que un transportista consulte únicamente turnos de camiones para los que posea la asociación autorizada pertinente. Cada camión MUST tener como máximo un transportista activo, de acuerdo con `datos-maestros`.

#### Scenario: Camión no autorizado

- **GIVEN** un transportista identificado y un camión sin asociación autorizada con él
- **WHEN** solicita consultar sus turnos
- **THEN** AgroFlow rechaza la consulta sin exponer información del vehículo
