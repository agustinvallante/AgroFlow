# Delta: Integración WhatsApp y n8n

## ADDED Requirements

### Requirement: Reporte y resolución de incidentes en ruta por WhatsApp

El asistente SHALL atender dos intenciones adicionales: reportar un problema en ruta (pinchazo, falla mecánica, accidente u otro) y avisar que el problema se resolvió. Para reportar, SHALL identificar el turno `EN_CAMINO` del transportista con la consulta de turnos, confirmar la patente, preguntar si puede seguir viaje y la demora estimada, leer los datos para confirmación y sólo entonces llamar a la herramienta de reporte. Ante un accidente, SHALL indicar primero que, si hay personas heridas, llame al 911. SHALL confirmar el reporte o la resolución únicamente con una respuesta exitosa de ZafraFlow API y MUST NOT prometer reprogramaciones ni cambios del turno.

#### Scenario: Transportista con un pinchazo

- **GIVEN** un transportista con un turno `EN_CAMINO`
- **WHEN** escribe que se le pinchó una goma
- **THEN** el asistente confirma la patente, pregunta si puede seguir y cuánto se demora, y registra el incidente después de la confirmación

#### Scenario: Problema resuelto

- **GIVEN** un transportista con un incidente abierto
- **WHEN** escribe que ya lo arregló y sigue viaje
- **THEN** el asistente informa la resolución a ZafraFlow API y confirma sólo después de una respuesta exitosa

#### Scenario: Sin turno en camino

- **GIVEN** un transportista sin turno `EN_CAMINO`
- **WHEN** reporta un problema en ruta
- **THEN** el asistente explica que sólo puede registrar problemas de un viaje en camino y no llama a la herramienta de reporte
