# Consulta de turnos

## Purpose

Define las consultas de transportistas y usuarios internos para CU-003, CU-004 y CU-005.

## Requirements

### Requirement: Consulta autorizada del transportista (RN-017)

AgroFlow MUST permitir que un transportista consulte únicamente turnos de camiones que esté autorizado a utilizar, sin fijar aquí la cardinalidad de esa asociación.

#### Scenario: Camión no autorizado

- **GIVEN** un transportista identificado y un camión sin asociación autorizada con él
- **WHEN** solicita consultar sus turnos
- **THEN** AgroFlow rechaza la consulta sin exponer información del vehículo

### Requirement: Estado vigente y consulta inmutable (RN-019, RN-020)

AgroFlow SHALL devolver el estado actual persistido por el backend y la consulta MUST NOT alterar estado, horario, prioridad ni ningún otro dato del turno.

#### Scenario: Consulta repetida

- **GIVEN** un turno existente y sin cambios concurrentes
- **WHEN** un actor autorizado lo consulta varias veces
- **THEN** AgroFlow devuelve el mismo estado vigente sin generar modificaciones

### Requirement: Listado aislado por ingenio (RN-021, RN-022)

AgroFlow MUST limitar el listado de un operador a turnos de su ingenio y la operación de listado MUST NOT modificar datos.

#### Scenario: Listado del ingenio

- **GIVEN** un operador asociado a un ingenio
- **WHEN** consulta el listado de turnos
- **THEN** recibe únicamente turnos de ese ingenio y la consulta no altera ninguno

### Requirement: Valores por defecto y filtros del listado (RN-022A, RN-022B)

AgroFlow SHALL listar por defecto los turnos de la fecha operativa actual ordenados por horario ascendente y SHALL admitir filtros por fecha, estado y patente.

#### Scenario: Listado sin filtros explícitos

- **GIVEN** un operador autorizado
- **WHEN** solicita el listado sin filtros
- **THEN** AgroFlow devuelve los turnos del día actual ordenados por horario ascendente

#### Scenario: Listado filtrado

- **GIVEN** filtros válidos de fecha, estado o patente
- **WHEN** un operador autorizado consulta el listado
- **THEN** AgroFlow devuelve solo los turnos que coinciden dentro de su ingenio

### Requirement: Detalle aislado, vigente e inmutable (RN-023, RN-024, RN-025)

AgroFlow MUST limitar el detalle a turnos del ingenio del usuario interno, SHALL obtener toda la información desde el estado vigente del backend y la consulta MUST NOT modificar dato alguno.

#### Scenario: Detalle autorizado

- **GIVEN** un usuario interno y un turno perteneciente a su ingenio
- **WHEN** consulta el detalle
- **THEN** AgroFlow devuelve la información vigente sin alterar el turno

#### Scenario: Detalle de otro ingenio

- **GIVEN** un usuario interno y un turno perteneciente a otro ingenio
- **WHEN** intenta consultar el detalle
- **THEN** AgroFlow rechaza la solicitud sin exponer los datos del turno
