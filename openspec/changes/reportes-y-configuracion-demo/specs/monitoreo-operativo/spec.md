## ADDED Requirements

### Requirement: Reporte diario de turnos y cupos de LocalDemo

El dashboard SHALL consultar una fecha de inicio de franja por vez y mostrar
los turnos asignados a ese día agrupados por estado vigente, incluidos los
finalizados y cancelados. SHALL sumar la capacidad, ocupación y disponibilidad
de las franjas tal como las informa la API. La fecha por defecto SHALL ser la
fecha local actual que publica la API para el ingenio efectivo; ambos listados
SHALL recibir la misma fecha explícita.

#### Scenario: Actualizar tras una cancelación

- **GIVEN** un turno cancelado cuya franja pertenece al día consultado
- **WHEN** el operador actualiza el reporte
- **THEN** el turno cuenta como cancelado y sigue incluido en el total de turnos
- **AND** los cupos reflejan la liberación según las franjas de la API

#### Scenario: Día vacío y error de lectura

- **GIVEN** un día sin turnos o sin franjas, o una solicitud de lectura fallida
- **WHEN** el operador abre, cambia de fecha o actualiza el reporte
- **THEN** indica qué datos están vacíos tras una respuesta válida
- **AND** si falla una lectura del reporte, muestra el error sin presentar cifras como si la consulta hubiera sido exitosa
- **AND** una respuesta anterior no sustituye la consulta vigente

### Requirement: Consulta de la configuración efectiva de LocalDemo

La API SHALL ofrecer `GET /api/v1/mill` exclusivamente en LocalDemo con
`DashboardKey`, sin parámetros, y devolver el nombre y la zona horaria
persistidos y la fecha local actual del ingenio efectivo. Configuración SHALL
mostrar el nombre, la zona horaria y, para el día consultado, el horario,
duración y cupo de cada franja persistida. La sección SHALL ser de solo lectura.

#### Scenario: Consultar el ingenio y sus franjas efectivas

- **GIVEN** un ingenio LocalDemo y sus franjas ya persistidas
- **WHEN** un operador local consulta Configuración
- **THEN** ve el nombre y la zona horaria del ingenio y las franjas por fecha
- **AND** las franjas vacías también se muestran con su duración y capacidad configurada

#### Scenario: Perfil y credenciales

- **GIVEN** un cliente sin la clave de dashboard o una API fuera de LocalDemo
- **WHEN** el cliente solicita datos a `GET /api/v1/mill`
- **THEN** la API de LocalDemo rechaza el acceso sin la clave de dashboard
- **AND** fuera de LocalDemo la ruta no se publica
