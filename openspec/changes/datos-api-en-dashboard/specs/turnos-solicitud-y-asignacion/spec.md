# Delta: Solicitud y asignación de turnos

## ADDED Requirements

### Requirement: Datos maestros en el alta manual del dashboard

El alta manual del dashboard SHALL ofrecer para elegir los transportistas que la API informa como activos y habilitados para solicitar turnos, las patentes de los camiones asociados al transportista elegido y las fincas activas del catálogo. Las opciones SHALL leerse de AgroFlow API al abrir el formulario. La carga estimada SHALL ser obligatoria y mayor que cero antes de enviar el alta. El dashboard MUST NOT inferir habilitaciones ni asociaciones por su cuenta, y la API SHALL seguir validando el alta.

#### Scenario: Transportista con un camión

- **GIVEN** un transportista habilitado con un único camión asociado
- **WHEN** el operador lo elige en el alta manual
- **THEN** su patente queda elegida y el alta se envía con su teléfono, esa patente y el código de la finca elegida

#### Scenario: Transportista no habilitado

- **GIVEN** un transportista que la API informa como no habilitado para solicitar turnos
- **WHEN** el operador abre el alta manual
- **THEN** ese transportista no aparece entre las opciones

#### Scenario: Carga estimada vacía

- **GIVEN** el alta manual sin carga estimada o con un valor que no es mayor que cero
- **WHEN** el operador guarda
- **THEN** el formulario señala la carga estimada y no envía el alta

#### Scenario: Opciones no disponibles

- **GIVEN** la API no responde al pedir transportistas o fincas
- **WHEN** el operador abre el alta manual
- **THEN** el formulario muestra el error y permite reintentar sin enviar un alta
