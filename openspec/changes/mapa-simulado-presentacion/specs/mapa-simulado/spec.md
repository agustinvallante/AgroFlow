## ADDED Requirements

### Requirement: Simulación identificable e independiente

La pestaña Mapa SHALL identificar que usa datos cargados a mano y que su información no proviene de AgroFlow API. SHALL poder utilizarse sin conexión a esa API. Los viajes simulados MUST NOT crear ni modificar turnos, estados operativos, capacidad o conversaciones del sistema.

#### Scenario: Mapa con la API desconectada

- **GIVEN** el frontend iniciado con la API fuera de servicio
- **WHEN** se abre Mapa y se cargan datos válidos
- **THEN** se puede observar el viaje simulado y su identificación como dato manual

#### Scenario: Llegada simulada

- **GIVEN** un viaje ficticio que alcanza el ingenio en el reloj del mapa
- **WHEN** la simulación muestra Llegó
- **THEN** se actualiza la visualización local sin ejecutar una transición de un turno de la API

### Requirement: Carga manual y validaciones del escenario

El mapa SHALL iniciar sin ingenio, fincas ni viajes precargados en un navegador sin datos guardados. SHALL exigir nombre y coordenadas válidas de los lugares. La latitud MUST estar entre -90 y 90 y la longitud entre -180 y 180. Un viaje SHALL requerir un ingenio, una finca cargada, patente, velocidad mayor que cero y valores no negativos de salida relativa y retraso. Una patente normalizada MUST NOT tener dos viajes simulados sin terminar.

#### Scenario: Preparación incompleta

- **GIVEN** un mapa sin ingenio o sin fincas
- **WHEN** se intenta agregar un viaje
- **THEN** la vista explica el dato faltante y no agrega el viaje

#### Scenario: Entrada inválida o viaje duplicado

- **GIVEN** una coordenada inválida, velocidad cero o una patente con un viaje sin terminar
- **WHEN** se envía el formulario correspondiente
- **THEN** se muestra el error y se conserva el escenario previo

### Requirement: Recorrido estimado de finca a ingenio

Cada viaje SHALL representar un único recorrido desde su finca hasta el ingenio. Para este prototipo, la distancia SHALL calcularse mediante haversine entre sus coordenadas, multiplicada por 1,3, y la duración estimada SHALL resultar de esa distancia dividida por la velocidad cargada. El valor inicial de velocidad SHALL ser 40 km/h. Las trayectorias SHALL ser ilustrativas y el fondo decorativo.

#### Scenario: Viaje en horario

- **GIVEN** un viaje válido con retraso cero
- **WHEN** pasa su hora de salida y luego la hora estimada de llegada
- **THEN** pasa de Programado a En ruta y a Llegó, y permanece en Llegó sin reiniciar el recorrido

#### Scenario: Retraso simulado

- **GIVEN** un viaje con minutos de retraso cargados
- **WHEN** transcurre su duración estimada antes de su llegada simulada
- **THEN** aparece en Demora y permanece próximo al ingenio hasta completar el retraso, cuando pasa a Llegó

### Requirement: Reloj y aceleración de demostración

El mapa SHALL avanzar con el tiempo real a x1 y SHALL ofrecer x10 y x60 para acortar el ensayo. SHALL identificar la velocidad seleccionada y el adelanto acumulado. El tiempo transcurrido con la pestaña cerrada o inactiva SHALL reflejarse al volver a mostrar el viaje.

#### Scenario: Ensayo acelerado

- **GIVEN** un viaje en curso con x60 seleccionado
- **WHEN** transcurre un segundo de tiempo real
- **THEN** su reloj simulado avanza sesenta segundos y muestra el adelanto acumulado

### Requirement: Persistencia y limpieza local

El escenario SHALL conservarse en localStorage por navegador y origen, incluidos lugares, viajes y adelanto acumulado. Al regresar SHALL recuperar esos datos y continuar con el tiempo vigente. Al quitar una finca SHALL quitar también sus viajes. Si el almacenamiento no permite guardar, el escenario SHALL seguir disponible durante esa visita.

#### Scenario: Recarga y continuación

- **GIVEN** un viaje guardado en un navegador
- **WHEN** se recarga el mismo origen después de transcurrir tiempo
- **THEN** el viaje conserva sus datos y refleja la situación calculada para el nuevo instante

#### Scenario: Eliminar una finca

- **GIVEN** una finca con viajes cargados
- **WHEN** se quita esa finca
- **THEN** desaparecen tanto la finca como sus viajes de la simulación
