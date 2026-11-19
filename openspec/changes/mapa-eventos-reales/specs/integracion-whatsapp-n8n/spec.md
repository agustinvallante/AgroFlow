# Delta: Integración WhatsApp y n8n

## ADDED Requirements

### Requirement: Aviso de llegada al ingenio por WhatsApp

En la demo local, el asistente SHALL atender una intención adicional, con lo que su alcance pasa de cinco a seis intenciones: avisar la llegada al ingenio (por ejemplo, "Llegué" o "ya estoy en el ingenio"). SHALL identificar el turno `EN_CAMINO` del transportista con la consulta de turnos, confirmar la patente y sólo entonces solicitar la transición a `EN_ESPERA`. SHALL confirmar la llegada únicamente con una respuesta exitosa de ZafraFlow API, con un mensaje breve como "Registramos su llegada. Espere el llamado para ingresar.", y SHALL explicar el rechazo `INVALID_TRANSITION` sin presentarlo como éxito. Sin turno `EN_CAMINO`, MUST NOT solicitar la transición. Esta medida de la demo local no modifica la autoridad del MVP (RN-027), que reserva `EN_ESPERA` al operador.

#### Scenario: Transportista que llega

- **GIVEN** un transportista con un turno `EN_CAMINO`
- **WHEN** escribe "Llegué"
- **THEN** el asistente confirma la patente, solicita la transición a `EN_ESPERA` y confirma la llegada sólo después de una respuesta exitosa

#### Scenario: Sin turno en camino

- **GIVEN** un transportista sin turno `EN_CAMINO`
- **WHEN** escribe "ya estoy en el ingenio"
- **THEN** el asistente explica que sólo puede registrar la llegada de un viaje en camino y no solicita la transición

#### Scenario: Transición rechazada

- **GIVEN** un turno que ya no está `EN_CAMINO` cuando se solicita la llegada
- **WHEN** ZafraFlow API responde `409 INVALID_TRANSITION`
- **THEN** el asistente explica brevemente que no se pudo registrar la llegada y no la confirma
