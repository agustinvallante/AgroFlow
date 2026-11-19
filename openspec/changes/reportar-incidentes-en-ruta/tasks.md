# Tareas

## Backend y contrato

- [x] Persistir incidentes en ruta con migración y exponer `openIncident` en `Appointment`.
- [x] Agregar `POST /api/v1/appointments/{id}/incidents` y `POST /api/v1/appointments/{id}/incidents/resolution`.
- [x] Resolver automáticamente el incidente abierto cuando el turno sale de `EN_CAMINO`.
- [x] Documentar las operaciones, el esquema y los errores en `docs/contracts/openapi.yaml`.
- [x] Probar reporte, duplicado, estado inválido, validación, resolución y resolución automática.

## Chatbot

- [x] Agregar las herramientas de reporte y resolución y las reglas del prompt en `chatbot/workflows/Chatbot.json`.
- [x] Actualizar el README del chatbot.

## Frontend

- [x] Mapear `openIncident` y mostrarlo en la cola de turnos y en el mapa.
- [x] Probar la cola y el mapa con un incidente abierto.

## Cierre

- [ ] Consolidar las capacidades después de aceptar la revisión.
