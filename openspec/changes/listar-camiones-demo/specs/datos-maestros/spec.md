# Delta: Datos maestros

## ADDED Requirements

### Requirement: Listado de camiones en la demo local

En `LocalDemo`, AgroFlow SHALL publicar un listado de sólo lectura de los camiones del ingenio sembrado, protegido por la clave del dashboard. Cada camión SHALL incluir su identificador, su patente canónica, su tipo de flota y si está habilitado, y el listado SHALL ordenarse por patente. El filtro opcional `active` SHALL limitar el resultado a camiones habilitados o inhabilitados. El listado MUST NOT modificar datos, MUST NOT incluir camiones de otro ingenio y MUST NOT existir fuera de `LocalDemo`. El alta, la edición, la inhabilitación y las asociaciones de camiones MUST NOT publicarse en este cambio.

#### Scenario: Camiones sembrados

- **GIVEN** la demo local con sus camiones ficticios sembrados
- **WHEN** el dashboard lista camiones con su clave
- **THEN** la API devuelve todos los camiones del ingenio ordenados por patente, con su tipo de flota y su estado

#### Scenario: Filtro por actividad

- **GIVEN** un camión inhabilitado y otros habilitados
- **WHEN** el dashboard lista con `active=true` y luego con `active=false`
- **THEN** la primera respuesta excluye al camión inhabilitado y la segunda devuelve sólo ese camión

#### Scenario: Sin coincidencias

- **GIVEN** un ingenio sin camiones inhabilitados
- **WHEN** el dashboard lista con `active=false`
- **THEN** la API responde éxito con un arreglo vacío

#### Scenario: Sin clave del dashboard

- **GIVEN** una solicitud sin clave, con una clave incorrecta o con la clave del canal
- **WHEN** se lista camiones
- **THEN** la API responde `401` sin datos de camiones

#### Scenario: Parámetro inválido

- **GIVEN** `active` con un valor distinto de `true` o `false`, vacío o repetido, o un parámetro no documentado
- **WHEN** el dashboard lista camiones
- **THEN** la API responde `400 VALIDATION_ERROR` asociado al parámetro y sin modificar datos

#### Scenario: Fuera de la demo local

- **GIVEN** la API iniciada fuera del perfil `LocalDemo`
- **WHEN** se solicita el listado de camiones
- **THEN** la ruta no existe
