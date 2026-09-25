## ADDED Requirements

### Requirement: Asociación explícita entre transportistas y camiones

AgroFlow SHALL representar la autorización de uso mediante una asociación explícita muchos a muchos entre transportistas y camiones. Cada asociación SHALL conservar fecha de alta, estado activo y fecha de inhabilitación cuando corresponda, y MUST existir como máximo una asociación activa para el mismo par transportista-camión.

Solo una asociación activa entre entidades activas SHALL habilitar al transportista a solicitar o consultar turnos del camión. Inhabilitar una asociación MUST NOT alterar los turnos históricos.

#### Scenario: Múltiples asociaciones válidas

- **GIVEN** transportistas y camiones activos del mismo ingenio
- **WHEN** un usuario autorizado asocia varios camiones a un transportista o varios transportistas a un camión
- **THEN** AgroFlow conserva cada asociación activa como autorización independiente

#### Scenario: Asociación activa duplicada

- **GIVEN** una asociación activa entre un transportista y un camión
- **WHEN** un usuario autorizado intenta crear otra asociación activa para el mismo par
- **THEN** AgroFlow rechaza la operación e identifica el conflicto sin duplicar la autorización

#### Scenario: Asociación inactiva en una nueva solicitud

- **GIVEN** un transportista y un camión activos cuya asociación fue inhabilitada
- **WHEN** el transportista intenta solicitar un turno para ese camión
- **THEN** AgroFlow rechaza la solicitud sin consumir capacidad y conserva los turnos históricos

### Requirement: Gestión acotada de transportistas

AgroFlow SHALL permitir listar, consultar, crear, editar los campos `nombre`, `DNI` y `WhatsApp`, inhabilitar y reactivar transportistas. El DNI y el número de WhatsApp SHALL ser identificadores únicos globales de la plataforma. AgroFlow MUST NOT eliminar físicamente un transportista con historia y SHALL aplicar la unicidad global también al crear, editar o reactivar.

#### Scenario: Alta de transportista con campos válidos

- **GIVEN** un nombre, DNI y número de WhatsApp válidos y no utilizados
- **WHEN** un usuario autorizado registra al transportista
- **THEN** AgroFlow lo conserva activo y disponible para asociaciones de su ingenio

#### Scenario: Conflicto de transportista dentro del mismo ingenio

- **GIVEN** un transportista inactivo cuyo DNI o WhatsApp entra en conflicto con otro transportista del mismo ingenio
- **WHEN** un actor autorizado intenta reactivarlo
- **THEN** AgroFlow rechaza la reactivación e identifica el campo en conflicto

#### Scenario: Conflicto global de transportista con otro ingenio

- **GIVEN** un DNI o número de WhatsApp ya utilizado por un transportista de otro ingenio
- **WHEN** un actor autorizado intenta crear, editar o reactivar un transportista con ese identificador
- **THEN** AgroFlow rechaza la operación sin revelar la existencia, el ingenio ni los datos del otro transportista

### Requirement: Gestión acotada de camiones

AgroFlow SHALL permitir listar, consultar, crear, editar la patente y el tipo de flota, inhabilitar y reactivar camiones. La patente normalizada SHALL ser un identificador único global de la plataforma, el tipo de flota SHALL ser `PROPIA` o `TERCEROS`, y AgroFlow MUST NOT eliminar físicamente un camión con historia.

#### Scenario: Patente equivalente dentro del mismo ingenio

- **GIVEN** un camión registrado con una patente en un ingenio
- **WHEN** un actor autorizado del mismo ingenio intenta crear, editar o reactivar otro camión con una representación equivalente de esa patente
- **THEN** AgroFlow rechaza la operación e identifica la patente como campo en conflicto

#### Scenario: Conflicto global de patente con otro ingenio

- **GIVEN** una patente normalizada ya utilizada por un camión de otro ingenio
- **WHEN** un actor autorizado intenta crear, editar o reactivar un camión con una representación equivalente
- **THEN** AgroFlow rechaza la operación sin revelar la existencia, el ingenio ni los datos del otro camión

#### Scenario: Tipo de flota fuera del catálogo

- **GIVEN** datos de un camión con un tipo de flota distinto de `PROPIA` o `TERCEROS`
- **WHEN** un usuario autorizado intenta crearlo o editarlo
- **THEN** AgroFlow rechaza la operación sin modificar el catálogo

### Requirement: Gestión acotada de fincas sin geodatos

AgroFlow SHALL permitir listar, consultar, crear, editar `código`, `nombre` y `referencia textual de ubicación`, inhabilitar y reactivar fincas. El código MUST ser único dentro del ingenio y el MVP MUST NOT exigir ni gestionar coordenadas, polígonos, rutas ni otros geodatos.

#### Scenario: Código duplicado dentro del ingenio

- **GIVEN** una finca con un código vigente en un ingenio
- **WHEN** un usuario autorizado intenta crear o reactivar otra finca con el mismo código en ese ingenio
- **THEN** AgroFlow rechaza la operación e identifica el conflicto

#### Scenario: El mismo código en otro ingenio

- **GIVEN** una finca con un código en un ingenio
- **WHEN** un usuario autorizado de otro ingenio registra una finca con el mismo código
- **THEN** AgroFlow permite el alta si las demás validaciones se cumplen

### Requirement: Permisos y aislamiento de datos maestros

AgroFlow MUST permitir gestionar transportistas, camiones, fincas y asociaciones únicamente a un actor con el permiso definido por `acceso-y-autorizacion`, y MUST limitar toda consulta y mutación al ingenio efectivo del actor.

#### Scenario: Actor con permiso gestiona datos de su ingenio

- **GIVEN** un actor con el permiso de gestión definido por `acceso-y-autorizacion` y datos maestros pertenecientes a su ingenio
- **WHEN** ejecuta una operación admitida con datos válidos
- **THEN** AgroFlow aplica la operación y conserva su historia

#### Scenario: Actor sin permiso de gestión

- **GIVEN** un actor sin el permiso de gestión definido por `acceso-y-autorizacion`
- **WHEN** intenta crear, editar, inhabilitar, reactivar o asociar datos maestros
- **THEN** AgroFlow rechaza la operación sin modificar datos

#### Scenario: Identificador de otro ingenio

- **GIVEN** un usuario con permiso de gestión y un identificador perteneciente a otro ingenio
- **WHEN** intenta consultarlo o modificarlo
- **THEN** AgroFlow rechaza la operación sin revelar ni alterar el recurso
