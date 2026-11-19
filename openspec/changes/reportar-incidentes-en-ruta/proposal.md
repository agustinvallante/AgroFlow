# Reportar incidentes en ruta por WhatsApp

> **Estado: propuesta para la demo local.** Base: `main` en `8ed3a25`.

## Problema

Durante la zafra, un camión en viaje puede detenerse por un pinchazo, una falla mecánica o un accidente. Hoy el ingenio se entera recién cuando el camión no llega: el transportista no tiene cómo avisarlo por el canal y el operador no lo ve en la cola ni en el mapa. El asistente de WhatsApp, además, tiene prohibido ofrecer reportes de incidentes porque no existe la capacidad.

## Alcance

- ZafraFlow API registra un incidente en ruta sobre un turno `EN_CAMINO`: tipo (`PINCHAZO`, `FALLA_MECANICA`, `ACCIDENTE`, `OTRO`), si el camión puede seguir viaje, demora estimada en minutos y un detalle opcional.
- Un turno tiene como máximo un incidente abierto. El transportista informa que lo resolvió (por ejemplo, reparó el neumático) y el incidente queda resuelto. Si el turno sale de `EN_CAMINO`, el incidente abierto se resuelve automáticamente.
- El incidente **no** cambia el estado ni la ventana del turno: la decisión operativa (esperar o cancelar) queda en manos del operador.
- La representación `Appointment` incluye `openIncident` (o `null`).
- El asistente de WhatsApp suma dos intenciones: reportar un problema en ruta y avisar que se resolvió. Ante un accidente con personas heridas, indica primero llamar al 911.
- El frontend muestra el incidente abierto en la cola de turnos y en el mapa (el camión figura con demora y el detalle explica el problema).

## Fuera de alcance

- Reprogramación automática de turnos (ver `interrupciones-operativas`, RN-034: los turnos `EN_CAMINO` no se reprograman solos).
- Notificaciones proactivas a otros destinatarios (`OD-010`).
- Historial de incidentes resueltos en la interfaz y gestión de incidentes desde el panel.
- Interrupciones del ingenio (CU-007).

## Contrato

| Operación | Ruta | Respuesta |
|---|---|---|
| Reportar | `POST /api/v1/appointments/{id}/incidents` | `201 Appointment` con `openIncident` |
| Resolver | `POST /api/v1/appointments/{id}/incidents/resolution` | `200 Appointment` con `openIncident: null` |

Errores: `400 VALIDATION_ERROR`, `404 APPOINTMENT_NOT_FOUND`, `409 INCIDENT_NOT_ALLOWED` (el turno no está `EN_CAMINO`), `409 OPEN_INCIDENT_EXISTS` y `409 NO_OPEN_INCIDENT`. La autorización es la misma que la de `POST /api/v1/appointments/{id}/transitions`.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `turnos-ciclo-de-vida` | Existente | [specs/turnos-ciclo-de-vida/spec.md](specs/turnos-ciclo-de-vida/spec.md) |
| `integracion-whatsapp-n8n` | Existente | [specs/integracion-whatsapp-n8n/spec.md](specs/integracion-whatsapp-n8n/spec.md) |
| `monitoreo-operativo` | Existente | [specs/monitoreo-operativo/spec.md](specs/monitoreo-operativo/spec.md) |
