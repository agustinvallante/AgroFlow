# Delta: Monitoreo operativo

## ADDED Requirements

### Requirement: Listado de ventanas y cupos en la demo local

En `LocalDemo`, ZafraFlow SHALL publicar un listado de sólo lectura de las ventanas persistidas del ingenio sembrado cuyo inicio cae en la fecha local pedida, protegido por la clave del dashboard. La fecha, `YYYY-MM-DD`, es opcional y por defecto es el día local actual del ingenio, de la medianoche local incluida a la siguiente excluida, según su zona horaria. El listado SHALL incluir las ventanas sin turnos y las ocupadas, ordenarse por inicio y emitir, por cada ventana, su identificador, inicio y fin con el desplazamiento horario del ingenio, su capacidad, sus cupos ocupados y sus cupos disponibles. Los cupos ocupados SHALL ser los persistidos de la ventana y los disponibles SHALL calcularse en el backend como capacidad menos ocupados. Una fecha sin ventanas SHALL responder éxito con un arreglo vacío. El listado MUST NOT modificar datos, MUST NOT incluir ventanas de otro ingenio, MUST NOT generar ventanas y MUST NOT existir fuera de `LocalDemo`. El alta, la edición y la baja de ventanas MUST NOT publicarse en este cambio. Esta entrega MUST NOT informar indisponibilidad por interrupciones, que depende de B33 (#37). No cierra `OD-004` ni `OD-012`.

#### Scenario: Franjas vacías

- **GIVEN** ventanas sembradas para la fecha sin turnos que consuman capacidad
- **WHEN** el dashboard lista las ventanas de esa fecha con su clave
- **THEN** cada ventana aparece con ocupación cero y todos sus cupos disponibles, ordenadas por inicio

#### Scenario: Ventanas ocupadas

- **GIVEN** una ventana con capacidad 2 y un turno asignado, y otra con ambos cupos tomados
- **WHEN** el dashboard lista las ventanas de la fecha
- **THEN** la primera informa un cupo ocupado y uno disponible, la segunda informa cero disponibles y ambas siguen listadas

#### Scenario: Liberación por cancelación

- **GIVEN** una ventana con un turno asignado
- **WHEN** el turno se cancela y el dashboard vuelve a listar las ventanas
- **THEN** la ventana informa el cupo liberado en esa lectura, sin que el frontend calcule la disponibilidad

#### Scenario: Fecha sin ventanas

- **GIVEN** una fecha para la que no existen ventanas persistidas
- **WHEN** el dashboard lista las ventanas con esa fecha
- **THEN** la API responde éxito con un arreglo vacío

#### Scenario: Fecha local del ingenio

- **GIVEN** una ventana al final del día local del ingenio, que ya es el día siguiente en UTC
- **WHEN** el dashboard lista las ventanas del día local, o no indica fecha cerca de la medianoche
- **THEN** la ventana pertenece al día local del ingenio y se informa con su desplazamiento horario

#### Scenario: Sin clave del dashboard

- **GIVEN** una solicitud sin clave, con una clave incorrecta o con la clave del canal
- **WHEN** se listan las ventanas
- **THEN** la API responde `401` sin datos de ventanas

#### Scenario: Parámetro inválido

- **GIVEN** una fecha con formato inválido, vacía o repetida, o un parámetro no documentado
- **WHEN** el dashboard lista las ventanas
- **THEN** la API responde `400 VALIDATION_ERROR` asociado al parámetro y sin modificar datos

#### Scenario: Aislamiento por ingenio

- **GIVEN** ventanas de dos ingenios en la misma base
- **WHEN** se consultan las ventanas de uno de ellos
- **THEN** el resultado contiene sólo las ventanas de ese ingenio

#### Scenario: Fuera de la demo local

- **GIVEN** el perfil MVP
- **WHEN** se solicita `GET /api/v1/windows`
- **THEN** la ruta no existe y la API responde `404`

#### Scenario: Sin indisponibilidad por interrupciones

- **GIVEN** que las interrupciones operativas (B33) todavía no existen
- **WHEN** el dashboard lista las ventanas
- **THEN** la respuesta no incluye ningún indicador de indisponibilidad ni simula uno
