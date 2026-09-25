## MODIFIED Requirements

### Requirement: Autorización por rol (RN-005)

AgroFlow MUST reconocer para usuarios internos únicamente los roles `OPERADOR`, `SUPERVISOR`, `GERENTE` y `ADMINISTRADOR`, MUST asociar cada sesión interna a un único ingenio efectivo y MUST verificar en el backend que el rol autorice cada operación solicitada.

Los permisos del MVP SHALL ser:

- `OPERADOR`: consultar dashboard, listado y detalle; avanzar o cancelar turnos; y registrar o actualizar interrupciones;
- `SUPERVISOR`: todos los permisos del operador, más gestionar datos maestros y asociaciones, configurar ventanas y capacidad, reintentar manualmente notificaciones fallidas y consultar auditoría;
- `GERENTE`: consultar dashboard, listado, detalle y auditoría, sin ejecutar mutaciones funcionales;
- `ADMINISTRADOR`: todos los permisos internos anteriores, más gestionar usuarios y roles internos.

#### Scenario: Operación permitida para el rol

- **GIVEN** un usuario interno autenticado con un rol válido y asociado a un ingenio
- **WHEN** solicita una operación habilitada para ese rol sobre datos de su ingenio
- **THEN** AgroFlow permite continuar con las validaciones propias de la operación

#### Scenario: Operación fuera del rol

- **GIVEN** un usuario autenticado cuyo rol no autoriza una operación
- **WHEN** intenta ejecutarla
- **THEN** AgroFlow rechaza la solicitud sin modificar datos

#### Scenario: Gerente intenta una mutación funcional

- **GIVEN** un usuario autenticado con rol `GERENTE`
- **WHEN** intenta modificar datos maestros, turnos o interrupciones
- **THEN** AgroFlow rechaza la operación sin modificar datos

#### Scenario: Configuración de ventanas y capacidad

- **GIVEN** un usuario autenticado con rol `SUPERVISOR` o `ADMINISTRADOR`
- **WHEN** configura ventanas o capacidad válidas para su ingenio
- **THEN** AgroFlow permite continuar con las validaciones y persistencia de la configuración

#### Scenario: Reintento manual de una notificación fallida

- **GIVEN** un usuario autenticado con rol `SUPERVISOR` o `ADMINISTRADOR` y una notificación fallida de su ingenio
- **WHEN** solicita reintentarla manualmente
- **THEN** AgroFlow permite iniciar el reintento sujeto a las validaciones de entrega

#### Scenario: Operación sobre otro ingenio

- **GIVEN** un usuario interno autenticado con un rol que permitiría una operación en su ingenio
- **WHEN** intenta aplicarla sobre un recurso de otro ingenio
- **THEN** AgroFlow rechaza la solicitud sin revelar ni modificar el recurso

## ADDED Requirements

### Requirement: Administración de acceso reservada

AgroFlow MUST permitir la gestión de usuarios y roles internos únicamente al rol `ADMINISTRADOR` y MUST volver a autorizar cada operación en el backend.

#### Scenario: Administrador gestiona un usuario interno

- **GIVEN** un usuario autenticado con rol `ADMINISTRADOR`
- **WHEN** crea, activa, inhabilita o modifica el rol de un usuario de su ingenio
- **THEN** AgroFlow aplica la operación después de validar sus datos y alcance

#### Scenario: Otro rol intenta gestionar acceso

- **GIVEN** un usuario autenticado con rol `OPERADOR`, `SUPERVISOR` o `GERENTE`
- **WHEN** intenta gestionar un usuario o su rol
- **THEN** AgroFlow rechaza la operación sin modificar el acceso
