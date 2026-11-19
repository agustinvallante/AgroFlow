## MODIFIED Requirements

### Requirement: Reloj y aceleración de demostración

El mapa SHALL avanzar con el tiempo real a x1 y SHALL ofrecer x10 y x60 para acortar el ensayo. SHALL identificar la velocidad seleccionada y el adelanto acumulado. El tiempo transcurrido con la pestaña cerrada o inactiva SHALL reflejarse al volver a mostrar el viaje. Mientras exista adelanto, el mapa SHALL ofrecer volver a la hora real. Al hacerlo SHALL quitar el adelanto, seleccionar x1, borrar los viajes de prueba y conservar el ingenio y ese estado en el navegador. Un camión con problema en ruta MUST NOT quedar detenido en una hora posterior a la hora real.

#### Scenario: Ensayo acelerado

- **GIVEN** un viaje en curso con x60 seleccionado
- **WHEN** transcurre un segundo de tiempo real
- **THEN** su reloj simulado avanza sesenta segundos y muestra el adelanto acumulado

#### Scenario: Vuelta a la hora real

- **GIVEN** un mapa adelantado con un viaje de prueba y un turno de la cola que, por el adelanto, figura llegado
- **WHEN** el operador elige volver a la hora real
- **THEN** el mapa muestra tiempo real con x1, sin el viaje de prueba, y el turno con el estado que le corresponde a la hora real, también después de recargar

#### Scenario: Problema en ruta registrado con el reloj adelantado

- **GIVEN** un camión de la cola detenido por un problema en ruta informado mientras el mapa estaba adelantado
- **WHEN** el operador vuelve a la hora real y transcurre el tiempo a x1
- **THEN** el camión permanece en la posición que le corresponde a la hora real
