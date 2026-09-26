# Delta: Ciclo de vida de turnos

## ADDED Requirements

### Requirement: Transiciones de la demo local (CU-006)

La demo local SHALL usar una única operación de transición para aplicar la secuencia `ASIGNADO -> EN_CAMINO -> EN_ESPERA -> INGRESADO -> EN_DESCARGA -> FINALIZADO`. El backend MUST validar el estado vigente y MUST persistir el nuevo estado antes de responder.

#### Scenario: Avance inmediato

- **GIVEN** un turno local en un estado operativo no terminal
- **WHEN** se solicita el estado inmediatamente siguiente
- **THEN** la API devuelve el turno con el nuevo estado persistido

#### Scenario: Salto o retroceso

- **GIVEN** un turno local en un estado operativo
- **WHEN** se solicita saltar un estado o volver a uno anterior
- **THEN** la API responde un conflicto y conserva el estado vigente

#### Scenario: Transición desde estado terminal

- **GIVEN** un turno `FINALIZADO` o `CANCELADO`
- **WHEN** se solicita otra transición
- **THEN** la API responde un conflicto y conserva el estado terminal

### Requirement: Cancelación como transición (CU-011)

La demo local SHALL aceptar `CANCELADO` mediante la operación canónica de transición únicamente desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`. Al persistir la cancelación, el backend SHALL liberar la capacidad y MUST conservar el turno consultable.

#### Scenario: Cancelación permitida

- **GIVEN** un turno local en `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA`
- **WHEN** se solicita `CANCELADO`
- **THEN** la API devuelve el turno cancelado y su ventana recupera el cupo

#### Scenario: Cancelación tardía

- **GIVEN** un turno local en `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO`
- **WHEN** se solicita `CANCELADO`
- **THEN** la API responde un conflicto y no modifica el turno
