## ADDED Requirements

### Requirement: Simulación identificable e independiente

La pestaña Mapa SHALL tomar el catálogo de fincas de AgroFlow API e identificar que las ubicaciones, rutas y viajes son simulados en el navegador, sin GPS. Lo que la API todavía no publica (coordenadas de ingenio y fincas, viajes y llegadas) SHALL resolverse en el frontend como medida temporal. Los viajes simulados MUST NOT crear ni modificar turnos, estados operativos, capacidad o conversaciones del sistema.

#### Scenario: Fincas no disponibles

- **GIVEN** el frontend iniciado con la API fuera de servicio
- **WHEN** se abre Mapa
- **THEN** la vista muestra el ingenio, avisa que no pudo cargar las fincas y permite reintentar sin perder los viajes guardados

#### Scenario: Llegada simulada

- **GIVEN** un viaje ficticio que alcanza el ingenio en el reloj del mapa
- **WHEN** la simulación muestra Llegó
- **THEN** se actualiza la visualización local sin ejecutar una transición de un turno de la API

### Requirement: Lugares y validaciones del escenario

El mapa SHALL mostrar las fincas activas que publica la API, ubicadas de forma determinista alrededor del ingenio a una distancia en línea recta de entre 14 y 30 km; si el nombre indica un rumbo (norte, sur, este, oeste o intermedios), la finca SHALL ubicarse hacia ese lado. El ingenio SHALL iniciar en una ubicación de referencia que el operador puede cambiar, con latitud entre -90 y 90 y longitud entre -180 y 180. El mapa SHALL iniciar sin viajes en un navegador sin datos guardados. Un viaje SHALL requerir una finca de la API, patente, una velocidad entre 1 y 120 km/h y una salida relativa y un retraso entre 0 y 1440 minutos. Los valores numéricos MUST ser finitos. Una patente normalizada MUST NOT tener dos viajes simulados sin terminar.

#### Scenario: Fincas de la API en el mapa

- **GIVEN** la API publica Finca Norte y Finca Sur
- **WHEN** se abre Mapa
- **THEN** ambas aparecen en el mapa, al norte y al sur del ingenio, y se pueden elegir como origen de un viaje

#### Scenario: Entrada inválida o viaje duplicado

- **GIVEN** una coordenada del ingenio fuera de rango, un valor no finito o fuera de los límites del viaje, o una patente con un viaje sin terminar
- **WHEN** se envía el formulario correspondiente
- **THEN** se muestra el error, se conserva el escenario previo y la vista sigue disponible

### Requirement: Recorrido estimado sobre una red vial simulada

Cada viaje SHALL representar un único recorrido desde su finca hasta el ingenio. Para este prototipo, la distancia SHALL calcularse mediante haversine entre sus coordenadas, multiplicada por 1,3, y la duración estimada SHALL resultar de esa distancia dividida por la velocidad cargada. El valor inicial de velocidad SHALL ser 40 km/h. El fondo SHALL representar sólo rutas, caminos de acceso y ríos simulados. Cada recorrido SHALL seguir el acceso de su finca y luego una ruta de ese fondo hasta el ingenio. El trazado SHALL ser ilustrativo y no la geografía real.

#### Scenario: Recorrido por la red

- **GIVEN** una finca de la API ubicada en el mapa
- **WHEN** un viaje desde esa finca está en curso
- **THEN** el camión avanza por el acceso de la finca y por una ruta dibujada en el fondo hasta el ingenio

#### Scenario: Viaje en horario

- **GIVEN** un viaje válido con retraso cero
- **WHEN** pasa su hora de salida y luego la hora estimada de llegada
- **THEN** pasa de Programado a En ruta y a Llegó, y permanece en Llegó sin reiniciar el recorrido

#### Scenario: Retraso simulado

- **GIVEN** un viaje con minutos de retraso cargados
- **WHEN** transcurre su duración estimada antes de su llegada simulada
- **THEN** aparece en Demora y permanece próximo al ingenio hasta completar el retraso, cuando pasa a Llegó

### Requirement: Detalle de un camión en el mapa

Los camiones en movimiento SHALL representarse como cuadrados. Al seleccionar uno con el puntero o el teclado, el mapa SHALL mostrar un recuadro junto a él con patente, estado, finca de salida, hora de salida, hora de llegada estimada o real, distancia del recorrido, distancia restante, velocidad y retraso simulado, e indicar que son estimaciones sin GPS. El recuadro SHALL cerrarse con su botón, con Escape o al tocar otro punto del mapa.

#### Scenario: Consultar un camión

- **GIVEN** un viaje en curso
- **WHEN** el operador toca su camión en el mapa
- **THEN** ve el recuadro con los datos del viaje y puede cerrarlo

### Requirement: Reloj y aceleración de demostración

El mapa SHALL avanzar con el tiempo real a x1 y SHALL ofrecer x10 y x60 para acortar el ensayo. SHALL identificar la velocidad seleccionada y el adelanto acumulado. El tiempo transcurrido con la pestaña cerrada o inactiva SHALL reflejarse al volver a mostrar el viaje.

#### Scenario: Ensayo acelerado

- **GIVEN** un viaje en curso con x60 seleccionado
- **WHEN** transcurre un segundo de tiempo real
- **THEN** su reloj simulado avanza sesenta segundos y muestra el adelanto acumulado

### Requirement: Persistencia y limpieza local

El escenario SHALL conservarse en localStorage por navegador y origen, incluidos la ubicación del ingenio, los viajes y el adelanto acumulado; las fincas SHALL volver a leerse de la API. Al regresar SHALL recuperar esos datos y continuar con el tiempo vigente. Un viaje cuya finca la API ya no publica MUST NOT mostrarse. Si el almacenamiento no permite guardar, el escenario SHALL seguir disponible durante esa visita.

#### Scenario: Recarga y continuación

- **GIVEN** un viaje guardado en un navegador
- **WHEN** se recarga el mismo origen después de transcurrir tiempo
- **THEN** el viaje conserva sus datos y refleja la situación calculada para el nuevo instante

#### Scenario: Datos guardados inválidos

- **GIVEN** un escenario guardado con ingenio, viajes o adelanto fuera de los límites admitidos
- **WHEN** se abre Mapa en ese navegador
- **THEN** la vista descarta los elementos inválidos, conserva los válidos y sigue disponible sin borrar el almacenamiento a mano

#### Scenario: Almacenamiento no disponible

- **GIVEN** un navegador que no permite leer ni guardar en localStorage
- **WHEN** se cargan viajes
- **THEN** la simulación funciona durante la visita aunque no se conserve al recargar
