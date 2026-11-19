# Delta: Monitoreo operativo

## ADDED Requirements

### Requirement: Recorrido de los turnos de la cola según eventos de la API

En el mapa, el viaje de cada turno de la cola SHALL seguir el estado y los instantes que informa ZafraFlow API en lugar de estimarse desde la ventana. Un turno `ASIGNADO` SHALL figurar como programado en su finca, sin moverse. Un turno `EN_CAMINO` SHALL avanzar desde su partida; con un incidente abierto SHALL figurar con demora y detenido donde estaba, y al resolverse SHALL retomar desde ese mismo punto y llegar más tarde. Cada navegador SHALL conservar un registro acotado de los tramos de su reloj del mapa `{realStartMs, adelantoStartMs, multiplier}`, con un tramo nuevo en cada cambio de velocidad y en cada adelanto de una hora; volver a la hora real SHALL borrarlo. Un instante real T de la API SHALL traducirse a la hora del mapa `T + adelanto(T)`, donde `adelanto(T)` surge del tramo que cubre T (antes del primer tramo, el adelanto inicial del primer tramo; con el registro vacío, el adelanto actual). La partida en el mapa SHALL ser la traducción de `departedAt` o, si es `null`, la hora del mapa en que el turno se vio por primera vez `EN_CAMINO`. Cada detención de `stops` y el inicio del incidente abierto SHALL traducirse del mismo modo; para el incidente abierto SHALL usarse la hora del mapa en que se vio por primera vez cuando sea anterior a la traducción de `reportedAt`, de modo que ningún camión retroceda. El tiempo en movimiento SHALL ser la hora del mapa menos la partida, menos la suma de las detenciones traducidas y, con un incidente abierto, menos el tiempo transcurrido desde su inicio; la llegada estimada SHALL ser la partida más la duración del viaje más el tiempo detenido total.

#### Scenario: Turno asignado

- **GIVEN** un turno `ASIGNADO` de una finca del catálogo
- **WHEN** el mapa lo muestra
- **THEN** el camión figura programado en su finca y no se mueve

#### Scenario: Problema resuelto tarde

- **GIVEN** un turno `EN_CAMINO` detenido por un incidente abierto durante varios minutos
- **WHEN** se informa la resolución
- **THEN** el camión retoma desde el punto donde se detuvo, sin saltos, y su llegada estimada se demora el tiempo detenido

#### Scenario: Velocidad cambiada durante el viaje

- **GIVEN** un turno `EN_CAMINO` en un mapa que pasa de x1 a x60
- **WHEN** el mapa vuelve a leer la cola
- **THEN** el camión sigue avanzando desde su posición, sin retroceder

### Requirement: Camión en la puerta del ingenio

Cuando se alcanza la llegada estimada de un turno que ZafraFlow API todavía informa `EN_CAMINO`, el mapa SHALL mostrar el camión esperando en la puerta del ingenio con el estado "En la puerta" y el detalle "Esperando ingreso", y SHALL contar estos camiones en un indicador propio. El viaje MUST NOT figurar como llegado hasta que la API informe el turno `EN_ESPERA`, `INGRESADO` o `EN_DESCARGA`. El mapa MUST NOT cambiar el estado del turno.

#### Scenario: Llegada estimada sin aviso

- **GIVEN** un turno `EN_CAMINO` cuya llegada estimada ya pasó
- **WHEN** el mapa lo muestra
- **THEN** el camión figura "En la puerta" con el detalle "Esperando ingreso" y suma en su indicador

#### Scenario: Llegada informada

- **GIVEN** un camión "En la puerta"
- **WHEN** la API informa el turno `EN_ESPERA`
- **THEN** el mapa lo muestra como llegado

### Requirement: Coherencia entre pantallas del mapa

El mapa SHALL volver a leer los turnos cada 3 segundos. Los viajes de prueba de la demo, que existen sólo en el navegador, SHALL conservar su comportamiento. A x1 y sin adelanto, todas las pantallas SHALL calcular las mismas posiciones a partir de los instantes de la API; con x10, x60 o el adelanto de una hora, cada pantalla SHALL mantenerse continua y coherente con su propio reloj, sin retrocesos. El adelanto de una hora SHALL mover todos los viajes una hora hacia adelante.

#### Scenario: Dos pantallas a x1

- **GIVEN** dos navegadores con el mapa a x1 y sin adelanto
- **WHEN** ambos leen el mismo turno `EN_CAMINO`
- **THEN** muestran el camión en la misma posición

#### Scenario: Adelanto de una hora

- **GIVEN** un turno `EN_CAMINO` en el mapa
- **WHEN** el operador adelanta una hora
- **THEN** el camión avanza lo que corresponde a una hora de viaje y no retrocede después
