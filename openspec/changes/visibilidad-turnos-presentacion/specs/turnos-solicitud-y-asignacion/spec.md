# Delta: Confirmación visible del alta

## ADDED Requirements

### Requirement: Acceso al turno confirmado desde el alta manual

El dashboard SHALL confirmar el alta únicamente después de recibir el turno
persistido de la API. SHALL mostrar patente, fecha, rango horario y
desplazamiento de su ventana, abrir su detalle y seleccionar la fecha de inicio
de esa ventana en la cola. SHALL limpiar estado, patente, teléfono y búsqueda
anteriores e informar el ajuste. MUST NOT calcular o predecir la asignación.

#### Scenario: Ventana asignada para hoy o mañana

- **GIVEN** una solicitud válida y filtros previos que podrían excluirla
- **WHEN** el POST devuelve un turno asignado para hoy o para otro día
- **THEN** se confirma la ventana recibida, se abre el detalle y la cola se consulta para su fecha, con los demás filtros limpios y un aviso del ajuste

#### Scenario: Alta rechazada

- **GIVEN** una solicitud que la API rechaza
- **WHEN** el formulario recibe el error
- **THEN** conserva sus datos y los filtros anteriores, muestra el error y no presenta una nueva confirmación

#### Scenario: Lectura anterior al alta

- **GIVEN** lecturas de lista o detalle pendientes anteriores a un alta confirmada
- **WHEN** esas respuestas llegan después de abrir el turno nuevo
- **THEN** no reemplazan el detalle confirmado ni la lista correspondiente a los filtros nuevos

#### Scenario: Recarga fallida después del alta

- **GIVEN** un POST exitoso con un turno persistido
- **WHEN** falla la consulta posterior de la cola
- **THEN** permanece la confirmación y el detalle del POST y se muestra el error de lectura sin anunciar un error de creación
