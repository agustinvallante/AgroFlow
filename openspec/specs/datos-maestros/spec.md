# Datos maestros

## Purpose

Define la gestión de transportistas, camiones y fincas del MVP para CU-009, CU-010 y CU-012.

## Requirements

### Requirement: Identidad única del transportista (RN-043, RN-044)

AgroFlow SHALL exigir que el DNI y el número de WhatsApp de cada transportista sean únicos dentro del sistema.

#### Scenario: Alta con identificador duplicado

- **GIVEN** un transportista existente con un DNI o número de WhatsApp determinado
- **WHEN** un usuario autorizado intenta registrar otro transportista con el mismo valor
- **THEN** AgroFlow rechaza el alta e identifica el campo en conflicto

### Requirement: Inactividad e historial del transportista (RN-045, RN-046)

AgroFlow MUST impedir que un transportista inactivo solicite nuevos turnos y SHALL conservar mediante inhabilitación a todo transportista que posea información operativa asociada.

#### Scenario: Solicitud de un transportista inactivo

- **GIVEN** un transportista inactivo
- **WHEN** intenta solicitar un nuevo turno
- **THEN** AgroFlow rechaza la solicitud sin reservar capacidad

#### Scenario: Baja con historial operativo

- **GIVEN** un transportista con información operativa asociada
- **WHEN** un usuario autorizado solicita retirarlo de la operación
- **THEN** AgroFlow lo inhabilita y conserva sus referencias históricas

### Requirement: Identidad, actividad y tipo de flota del camión (RN-047, RN-048, RN-049)

AgroFlow SHALL exigir una patente única por camión, MUST impedir que un camión inactivo participe en nuevas solicitudes y SHALL aceptar únicamente tipos de flota definidos en el catálogo vigente.

#### Scenario: Camión habilitado con datos válidos

- **GIVEN** una patente no registrada y un tipo de flota admitido
- **WHEN** un usuario autorizado registra un camión activo
- **THEN** AgroFlow guarda el camión como disponible para asociaciones autorizadas

#### Scenario: Camión inválido para una solicitud

- **GIVEN** un camión inactivo, con patente duplicada o con un tipo de flota no admitido
- **WHEN** se intenta utilizar o registrar según corresponda
- **THEN** AgroFlow rechaza la operación e informa la validación incumplida

### Requirement: Conservación del historial del camión (RN-049A)

AgroFlow SHALL inhabilitar, en lugar de eliminar físicamente, todo camión con historial operativo.

#### Scenario: Retiro de un camión con historial

- **GIVEN** un camión asociado a operaciones históricas
- **WHEN** un usuario autorizado solicita retirarlo
- **THEN** AgroFlow lo marca como inactivo y mantiene las referencias existentes

### Requirement: Actividad y alcance de las fincas (RN-053, RN-054, RN-054A)

AgroFlow MUST impedir que una finca inactiva se use en nuevas solicitudes, SHALL conservarla como referencia en turnos históricos y SHALL limitar el MVP a un catálogo sin coordenadas, polígonos ni funciones PostGIS.

#### Scenario: Nueva solicitud con finca inactiva

- **GIVEN** una finca inactiva
- **WHEN** se intenta crear una solicitud de turno que la referencia
- **THEN** AgroFlow rechaza la solicitud sin eliminar la finca de los turnos históricos

#### Scenario: Consulta histórica tras inhabilitación

- **GIVEN** un turno histórico cuya finca fue inhabilitada posteriormente
- **WHEN** un usuario autorizado consulta ese turno
- **THEN** AgroFlow conserva y presenta la referencia histórica de la finca
