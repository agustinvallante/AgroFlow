# Mapa basado en eventos reales y llegada por WhatsApp

> **Estado: propuesta para la demo local.** Base: `main` en `24e8068`.

## Problema

El mapa estima el recorrido de los turnos de la cola a partir de su ventana y no de lo que ocurrió. Por eso:

- un camión figura en ruta en el mapa mientras la cola de turnos lo informa `ASIGNADO`, porque el transportista todavía no avisó que salió;
- cuando un problema en ruta se resuelve varios minutos después de reportado, el punto del mapa pega un salto, porque el mapa no conoce cuándo empezó y terminó cada detención;
- el mapa muestra el camión llegado mientras la cola sigue en `EN_CAMINO`, porque el transportista no tiene cómo avisar su llegada;
- con x10, x60 o el adelanto de una hora, cada pantalla calcula posiciones distintas para el mismo turno.

## Alcance

- ZafraFlow API registra la hora de partida del turno al pasar a `EN_CAMINO` y la expone como `departedAt` en las representaciones de turnos de la demo local.
- ZafraFlow API expone `stops`: los incidentes en ruta resueltos del turno como detenciones `{ startedAt, endedAt }`, ordenadas por inicio, incluidos los resueltos automáticamente al salir de `EN_CAMINO`. No se agrega una tabla: los datos ya existen en los incidentes en ruta.
- El asistente de WhatsApp suma una intención: avisar la llegada al ingenio, que pasa el turno de `EN_CAMINO` a `EN_ESPERA`.
- El mapa sigue el estado y los instantes de la API: el camión de un turno `ASIGNADO` queda en su finca, el de uno `EN_CAMINO` avanza desde su partida y se detiene durante cada incidente, y espera en la puerta del ingenio hasta que la API informa la llegada. Un registro del reloj de cada navegador traduce los instantes reales a la hora del mapa para que x10, x60 y el adelanto de una hora no hagan retroceder a ningún camión.
- La cola de turnos se vuelve a leer cada 3 segundos.

## Fuera de alcance

- GPS o posiciones reales de los camiones.
- Cambios del contrato MVP (`openapi.mvp-appointment-reads.yaml`): sus lecturas no emiten `openIncident`, `departedAt` ni `stops`.
- Que el mapa cambie el estado de un turno (ADR-003).
- Que el transportista informe estados posteriores a `EN_ESPERA` (`INGRESADO`, `EN_DESCARGA`, `FINALIZADO`), que siguen reservados al operador.
- Hora de partida para los turnos que ya estaban `EN_CAMINO` antes de la migración: se informa `null`.

## Contrato

| Campo | Representaciones | Valor |
|---|---|---|
| `departedAt` | `Appointment` (demo local) y `AppointmentSummary` | Hora de la transición a `EN_CAMINO` con el desplazamiento del ingenio, o `null` |
| `stops` | `Appointment` (demo local) y `AppointmentSummary` | Arreglo, posiblemente vacío, de `{ startedAt, endedAt }` ordenado por `startedAt` |

`departedAt` y `stops` son opcionales en el esquema compartido `Appointment` y obligatorios en `AppointmentSummary`, igual que `openIncident`. No hay rutas nuevas: la llegada usa `POST /api/v1/appointments/{id}/transitions` con `{"newStatus":"EN_ESPERA"}`. La versión de `docs/contracts/openapi.yaml` pasa a `0.11.0`.

## Persistencia

Migración aditiva `AddAppointmentDeparture`: agrega la columna anulable `DepartedAtUtc` a `Turnos`. No modifica ni borra datos existentes.

## Decisiones

- El aviso de llegada por WhatsApp es una medida de la demo local para que el mapa y la cola coincidan. No modifica RN-027 para el MVP, que reserva `EN_ESPERA` al operador.
- Este cambio vincula el mapa con `EN_CAMINO`, las detenciones y la llegada operativa, que `docs/planning/open-decisions.md` dejaba para un cambio posterior compartido.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `turnos-ciclo-de-vida` | Existente | [specs/turnos-ciclo-de-vida/spec.md](specs/turnos-ciclo-de-vida/spec.md) |
| `integracion-whatsapp-n8n` | Existente | [specs/integracion-whatsapp-n8n/spec.md](specs/integracion-whatsapp-n8n/spec.md) |
| `monitoreo-operativo` | Existente | [specs/monitoreo-operativo/spec.md](specs/monitoreo-operativo/spec.md) |
