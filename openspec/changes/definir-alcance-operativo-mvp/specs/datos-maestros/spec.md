## MODIFIED Requirements

### Requirement: Identidad única del transportista (RN-043, RN-044)

AgroFlow SHALL exigir que el DNI y el número de WhatsApp de cada transportista sean únicos dentro del sistema. El DNI SHALL normalizarse a su representación de solo dígitos para comparar identidad, y MUST rechazarse un valor sin dígitos.

#### Scenario: Alta con identificador duplicado

- **GIVEN** un transportista existente con un DNI o número de WhatsApp determinado
- **WHEN** un usuario autorizado intenta registrar otro transportista con el mismo valor
- **THEN** AgroFlow rechaza el alta e identifica el campo en conflicto

#### Scenario: Normalización de DNI

- **GIVEN** un DNI que contiene dígitos y separadores
- **WHEN** AgroFlow crea o actualiza el transportista
- **THEN** AgroFlow usa únicamente los dígitos del DNI para comparar su identidad y rechaza un DNI sin dígitos

## ADDED Requirements

### Requirement: Gestión de catálogos del MVP

AgroFlow SHALL permitir listar, registrar, editar e inhabilitar transportistas, camiones y fincas a operadores y supervisores autorizados, respetando el aislamiento por ingenio. La inhabilitación MUST conservar las referencias históricas y un registro inactivo MUST NOT habilitar nuevas solicitudes de turno. Los campos editables exactos continúan sujetos a `OD-013`.

#### Scenario: Gestión autorizada de un catálogo

- **GIVEN** un operador o supervisor autorizado del ingenio
- **WHEN** lista, registra, edita o inhabilita un transportista, camión o finca dentro de su alcance
- **THEN** AgroFlow persiste el cambio permitido y conserva la información histórica relacionada

#### Scenario: Gestión fuera del alcance del usuario

- **GIVEN** un usuario sin permiso o un recurso fuera de su ingenio
- **WHEN** intenta registrar, editar o inhabilitar datos maestros
- **THEN** AgroFlow rechaza la operación sin modificar ni exponer datos ajenos

### Requirement: Asociación activa única por camión

AgroFlow SHALL mantener una asociación explícita entre un camión y su transportista autorizado, y MUST impedir que un camión tenga más de un transportista activo a la vez. Una nueva solicitud de turno MUST usar una asociación activa; las asociaciones históricas SHALL conservarse para interpretar operaciones anteriores.

#### Scenario: Segunda asociación activa para el mismo camión

- **GIVEN** un camión con una asociación activa a un transportista
- **WHEN** se intenta asociarlo simultáneamente a otro transportista
- **THEN** AgroFlow rechaza la segunda asociación sin cambiar la autorización vigente

#### Scenario: Solicitud sin asociación activa

- **GIVEN** un camión y un transportista sin asociación activa entre sí
- **WHEN** el transportista solicita un turno para ese camión
- **THEN** AgroFlow rechaza la solicitud sin reservar capacidad
