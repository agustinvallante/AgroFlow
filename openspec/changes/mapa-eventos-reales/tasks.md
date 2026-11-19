# Tareas

## Backend y contrato

- [x] Persistir la hora de partida al pasar a `EN_CAMINO` con la migración aditiva `AddAppointmentDeparture`.
- [x] Exponer `departedAt` y `stops` en `Appointment` (demo local) y `AppointmentSummary`, sin cambiar las lecturas MVP.
- [x] Documentar los campos en `docs/contracts/openapi.yaml` y subir la versión a `0.11.0`.
- [x] Probar la partida, su conservación en transiciones posteriores, las detenciones resueltas a mano y automáticamente, su orden, el listado, las lecturas MVP y la migración con datos existentes.

## Chatbot

- [x] Agregar la herramienta `Report Arrival` y las reglas del prompt en `chatbot/workflows/Chatbot.json`.
- [x] Actualizar el README del chatbot.

## Frontend

- [x] Mover los camiones de la cola según `status`, `departedAt`, `stops` y `openIncident`, con el registro del reloj de cada navegador.
- [x] Mostrar el estado "En la puerta" con su contador y leer la cola cada 3 segundos.
- [x] Actualizar `frontend/README.md` y `docs/development/mapa-simulado-presentacion.md`.

## Cierre

- [ ] Consolidar las capacidades después de aceptar la revisión.
