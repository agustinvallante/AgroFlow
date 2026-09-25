## MODIFIED Requirements

### Requirement: Autoridad para actualizar (RN-027, RN-030)

AgroFlow MUST permitir avanzar o cancelar turnos únicamente a un actor con el permiso correspondiente definido por `acceso-y-autorizacion` y solo dentro de su ingenio efectivo. AgroFlow MUST impedir esas mutaciones a cualquier actor sin ese permiso.

AgroFlow SHALL aceptar `EN_CAMINO` informado por el transportista asociado mediante WhatsApp y n8n. Las transiciones a `EN_ESPERA`, `INGRESADO`, `EN_DESCARGA` y `FINALIZADO` SHALL quedar reservadas a usuarios internos autorizados en el MVP. Toda transición SHALL volver a autorizarse en el backend.

#### Scenario: Transportista informa viaje iniciado

- **GIVEN** un turno `ASIGNADO` asociado a un transportista identificado y autorizado para el camión
- **WHEN** el transportista informa por el canal habilitado que está en camino
- **THEN** AgroFlow puede persistir el turno como `EN_CAMINO`

#### Scenario: Actor autorizado avanza el turno

- **GIVEN** un turno del ingenio de un actor con el permiso correspondiente definido por `acceso-y-autorizacion`
- **WHEN** solicita el estado inmediatamente siguiente permitido
- **THEN** AgroFlow aplica la transición después de validar el estado vigente

#### Scenario: Actor sin permiso intenta modificar el turno

- **GIVEN** un actor autenticado sin el permiso correspondiente definido por `acceso-y-autorizacion`
- **WHEN** intenta avanzar o cancelar un turno
- **THEN** AgroFlow rechaza la operación sin modificar el turno

#### Scenario: Actor sin autoridad para el nuevo estado

- **GIVEN** un actor identificado que no está autorizado para una transición o cuyo ingenio no coincide con el turno
- **WHEN** solicita actualizar el estado
- **THEN** AgroFlow rechaza la operación sin modificar ni revelar datos ajenos
