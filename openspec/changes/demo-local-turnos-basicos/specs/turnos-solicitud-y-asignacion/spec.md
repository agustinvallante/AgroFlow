# Delta: Solicitud y asignación de turnos

## ADDED Requirements

### Requirement: Solicitud mínima de la demo local (CU-002)

La demo local SHALL aceptar solicitudes con `carrierPhone`, `truckPlate` y `farmCode` provistos por el seed, el momento de corte y la carga estimada en toneladas. El backend SHALL resolver esas referencias naturales, validar que estén activas y asociadas, y MUST NOT requerir que el consumidor consulte datos maestros ni envíe UUIDs. La solicitud MUST NOT aceptar una ventana preferida y el backend SHALL asignar y persistir la primera ventana futura con cupo antes de responder.

#### Scenario: Solicitud local válida

- **GIVEN** un teléfono de transportista, una patente y un código de finca activos y asociados en el seed, y una ventana futura con cupo
- **WHEN** un consumidor crea un turno mediante la operación canónica usando esas referencias naturales
- **THEN** la API responde el turno `ASIGNADO` con su ventana persistida

#### Scenario: Referencia ajena al seed

- **GIVEN** un `carrierPhone`, `truckPlate` o `farmCode` inexistente o inactivo
- **WHEN** se solicita un turno
- **THEN** la API rechaza la solicitud sin consumir capacidad

### Requirement: Conflictos mínimos de asignación

La demo local MUST impedir un segundo turno activo para el mismo camión y MUST rechazar una solicitud cuando no exista una ventana sembrada con cupo. El rechazo SHALL usar el error definido en OpenAPI y no confirmará una asignación aparente.

#### Scenario: Camión con turno activo

- **GIVEN** un camión del seed con un turno no terminal
- **WHEN** se solicita otro turno para ese camión
- **THEN** la API responde un conflicto y conserva sin cambios la capacidad

#### Scenario: Ventanas sin cupo

- **GIVEN** que todas las ventanas locales aplicables alcanzaron su cupo
- **WHEN** se solicita un turno elegible
- **THEN** la API responde un conflicto sin crear un turno incompleto

### Requirement: Supuestos de capacidad confinados al seed

El seed de la demo SHALL preparar un único ingenio con zona horaria `America/Argentina/Tucuman`, ventanas de 30 minutos, cupo de dos camiones y calendario para la fecha local de ejecución. La implementación MUST NOT convertir esos valores en constantes generales del dominio.

#### Scenario: Preparación para la fecha de ejecución

- **GIVEN** una inicialización local mediante el procedimiento documentado
- **WHEN** se consultan los datos preparados
- **THEN** existen referencias ficticias y ventanas suficientes para ejecutar los recorridos de alta y cancelación
