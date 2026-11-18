# Delta: Consulta de turnos

## ADDED Requirements

### Requirement: Consulta de turnos activos sin fecha por defecto (CU-003)

La API SHALL admitir el filtro opcional `active=true` en el listado de turnos. Con ese filtro SHALL devolver sólo turnos en estados no terminales, excluyendo `FINALIZADO` y `CANCELADO`, y, cuando no se informa `date`, MUST NOT aplicar la fecha local actual por defecto: SHALL incluir turnos de hoy, de mañana, de fechas posteriores y turnos activos cuya ventana ya pasó. Con `date` SHALL restringirse a esa fecha. Sin `active`, el listado SHALL conservar la fecha local actual por defecto. El filtro MUST NOT admitir otro valor que `true` y la consulta MUST NOT modificar datos.

#### Scenario: Turno asignado para mañana

- **GIVEN** un transportista cuyo único turno activo tiene una ventana de mañana
- **WHEN** se lista con su `phone` y `active=true`, sin `date`
- **THEN** la API devuelve ese turno con su estado y ventana vigentes

#### Scenario: Turnos de varias fechas y terminales

- **GIVEN** un transportista con turnos activos de hoy, de mañana y de un día posterior, y con turnos `CANCELADO` y `FINALIZADO`
- **WHEN** se lista con su `phone` y `active=true`
- **THEN** la API devuelve sólo los turnos activos, ordenados por inicio de ventana ascendente

#### Scenario: Turno activo con ventana vencida

- **GIVEN** un turno `ASIGNADO` cuya ventana comenzó en un día anterior
- **WHEN** se lista con el `phone` de su transportista y `active=true`
- **THEN** la API lo devuelve, porque sigue activo para su camión

#### Scenario: Fecha explícita con turnos activos

- **GIVEN** turnos activos y terminales de un transportista en distintas fechas
- **WHEN** se lista con `active=true` y una `date`
- **THEN** la API devuelve sólo los turnos activos de esa fecha

#### Scenario: Listado sin el filtro

- **GIVEN** turnos de un transportista hoy y en otras fechas
- **WHEN** se lista con su `phone` sin `active` ni `date`
- **THEN** la API devuelve sólo los turnos de la fecha local actual, incluidos los terminales

#### Scenario: Transportista sin turnos activos

- **GIVEN** un `phone` sin turnos activos o inexistente
- **WHEN** se lista con ese `phone` y `active=true`
- **THEN** la API responde éxito con una colección vacía, distinta de una respuesta de error

#### Scenario: Valor de filtro inválido

- **GIVEN** `active` con un valor distinto de `true`, vacío o repetido
- **WHEN** se consulta el listado
- **THEN** la API responde un error de validación sin modificar turnos
