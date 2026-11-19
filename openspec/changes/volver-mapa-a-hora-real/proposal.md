# Volver el mapa a la hora real

> **Estado: propuesta para la demo local.** Base: `main` en `997f4cd`. Amplía `mapa-simulado-presentacion`.

## Problema

Con x10 o x60 el reloj del mapa acumula un adelanto que se conserva en el navegador y no tiene forma de volver a cero: x1 sólo deja de sumarlo. Un turno pedido por WhatsApp usa la hora real, así que en un mapa adelantado figura como si fuera del pasado (por ejemplo, llegado en lugar de programado).

## Alcance

- Junto al adelanto acumulado, el mapa ofrece un botón **Hora real**, visible sólo cuando hay adelanto.
- Al usarlo, el adelanto vuelve a cero, la velocidad vuelve a x1 y el cambio se conserva en el navegador.
- Se borran los viajes de prueba: su salida se calculó con el reloj adelantado y quedarían en el futuro.
- Un camión con problema en ruta queda detenido en la posición que le corresponde a la hora real, no en la hora adelantada en que se registró el problema.
- El ingenio cargado se conserva.

## Fuera de alcance

- Cambios en la API, en el chatbot o en el estado de los turnos.
- Descartar el adelanto automáticamente al recargar.
