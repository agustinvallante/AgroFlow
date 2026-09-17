# Ciclo de vida de turnos

## Purpose

Define las transiciones operativas y la cancelación de turnos para CU-006 y CU-011.

## Requirements

### Requirement: Secuencia operativa del MVP (RN-026)

AgroFlow SHALL aplicar la secuencia `ASIGNADO -> EN_CAMINO -> EN_ESPERA -> INGRESADO -> EN_DESCARGA -> FINALIZADO` como ciclo operativo normal del MVP.

#### Scenario: Avance al estado siguiente

- **GIVEN** un turno en un estado operativo no terminal
- **WHEN** un actor autorizado solicita el estado inmediatamente siguiente
- **THEN** AgroFlow valida y aplica la transición correspondiente

#### Scenario: Salto de estado no especificado

- **GIVEN** un turno en un estado operativo
- **WHEN** se solicita saltar uno o más estados de la secuencia
- **THEN** AgroFlow no aplica la transición mientras no exista una especificación que la autorice

### Requirement: Autoridad para actualizar (RN-027, RN-030)

AgroFlow MUST permitir que un operador modifique únicamente turnos de su ingenio; SHALL aceptar `EN_CAMINO` informado por el transportista mediante WhatsApp y n8n; y SHALL reservar `EN_ESPERA`, `INGRESADO`, `EN_DESCARGA` y `FINALIZADO` al operador en el MVP.

#### Scenario: Transportista informa viaje iniciado

- **GIVEN** un turno `ASIGNADO` asociado a un transportista identificado
- **WHEN** el transportista informa por el canal habilitado que está en camino
- **THEN** AgroFlow puede persistir el turno como `EN_CAMINO`

#### Scenario: Actor sin autoridad para el nuevo estado

- **GIVEN** un actor identificado que no está autorizado para una transición
- **WHEN** solicita actualizar el estado
- **THEN** AgroFlow rechaza la operación sin modificar el turno

### Requirement: Estados terminales (RN-028, RN-029)

AgroFlow MUST impedir que un turno `FINALIZADO` vuelva a un estado operativo anterior y MUST impedir que un turno `CANCELADO` continúe el ciclo operativo.

#### Scenario: Reactivación de estado terminal

- **GIVEN** un turno `FINALIZADO` o `CANCELADO`
- **WHEN** se solicita una transición operativa posterior o anterior
- **THEN** AgroFlow rechaza la transición y conserva el estado terminal

### Requirement: Persistencia antes de efectividad (RN-030A)

AgroFlow MUST considerar un nuevo estado efectivo únicamente después de persistirlo correctamente en el backend.

#### Scenario: Error al persistir transición

- **GIVEN** una transición válida en memoria
- **WHEN** la persistencia falla
- **THEN** AgroFlow no confirma ni publica el nuevo estado como efectivo

### Requirement: Estados cancelables (RN-050, RN-051)

AgroFlow SHALL permitir la cancelación solo desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA` y MUST rechazarla desde `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO`.

#### Scenario: Cancelación permitida

- **GIVEN** un turno en `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`
- **WHEN** un actor autorizado solicita cancelarlo
- **THEN** AgroFlow persiste el estado `CANCELADO`

#### Scenario: Cancelación fuera de término

- **GIVEN** un turno en `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO`
- **WHEN** se solicita cancelarlo
- **THEN** AgroFlow rechaza la operación y conserva el estado

### Requirement: Liberación de capacidad y conservación (RN-052, RN-052A)

Al cancelar un turno, AgroFlow SHALL liberar la capacidad que utilizaba, SHALL conservar el turno sin borrarlo físicamente y MUST tratar `CANCELADO` como estado terminal.

#### Scenario: Cancelación confirmada

- **GIVEN** un turno cancelable que consume capacidad
- **WHEN** la cancelación se persiste correctamente
- **THEN** la capacidad queda disponible y el turno permanece consultable como `CANCELADO`
