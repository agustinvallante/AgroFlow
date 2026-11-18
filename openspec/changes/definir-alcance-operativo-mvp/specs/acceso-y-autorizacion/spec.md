## ADDED Requirements

### Requirement: Permisos internos acordados para el MVP

ZafraFlow SHALL autorizar a los roles operador y supervisor a listar, registrar, editar e inhabilitar transportistas, camiones y fincas de su ámbito; y a crear y cancelar turnos de su ingenio según las reglas vigentes. Esta autorización MUST NOT permitir acceder a recursos de otro ingenio. Los permisos de los demás roles y operaciones continúan sujetos a `OD-007`.

#### Scenario: Operación del ingenio por operador o supervisor

- **GIVEN** un operador o supervisor autenticado y asociado a un ingenio
- **WHEN** realiza una operación acordada de datos maestros o turnos dentro de ese ingenio
- **THEN** ZafraFlow evalúa las reglas de la operación y no la rechaza por el rol

#### Scenario: Mismo rol, otro ingenio

- **GIVEN** un operador o supervisor autenticado de un ingenio
- **WHEN** intenta modificar un dato maestro o turno de otro ingenio
- **THEN** ZafraFlow rechaza la operación sin alterar ni exponer el recurso
