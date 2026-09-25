## MODIFIED Requirements

### Requirement: Gestión autorizada y unicidad activa (RN-031, RN-032)

AgroFlow MUST permitir registrar o actualizar una interrupción únicamente a un actor con el permiso correspondiente definido por `acceso-y-autorizacion` y dentro de su ingenio efectivo. AgroFlow MUST impedir esas mutaciones a cualquier actor sin ese permiso y SHALL mantener como máximo una interrupción activa por ingenio en el MVP.

#### Scenario: Interrupción registrada por un actor autorizado

- **GIVEN** un actor con el permiso correspondiente definido por `acceso-y-autorizacion` y un ingenio sin interrupción activa
- **WHEN** registra una interrupción válida para su ingenio
- **THEN** AgroFlow persiste la interrupción y procesa sus efectos únicamente después de confirmarla

#### Scenario: Segunda interrupción activa

- **GIVEN** un ingenio con una interrupción activa
- **WHEN** un usuario autorizado intenta registrar otra interrupción activa
- **THEN** AgroFlow rechaza la duplicación o exige actualizar la existente

#### Scenario: Actor sin permiso de mutación

- **GIVEN** un actor sin el permiso correspondiente definido por `acceso-y-autorizacion`
- **WHEN** intenta registrar o actualizar una interrupción
- **THEN** AgroFlow rechaza la operación sin modificar turnos ni interrupciones

#### Scenario: Interrupción de otro ingenio

- **GIVEN** un usuario autorizado y una interrupción perteneciente a otro ingenio
- **WHEN** intenta consultarla por identificador o modificarla
- **THEN** AgroFlow rechaza la operación sin revelar ni alterar el recurso

### Requirement: Reprogramación de turnos asignados (RN-033)

AgroFlow SHALL reprogramar automáticamente los turnos `ASIGNADO` afectados por una interrupción usando únicamente ventanas posteriores compatibles, la capacidad vigente y el mismo orden lexicográfico estable definido para la asignación inicial. La reprogramación MUST NOT sobreasignar capacidad ni confirmar un cambio antes de persistirlo.

#### Scenario: Interrupción afecta turno asignado

- **GIVEN** una interrupción confirmada y un turno `ASIGNADO` cuya ventana queda afectada
- **WHEN** AgroFlow procesa sus efectos y existe capacidad posterior compatible
- **THEN** asigna al turno una ventana posterior conforme a capacidad y prioridad e informa el cambio persistido

#### Scenario: Varios turnos requieren reprogramación

- **GIVEN** varios turnos `ASIGNADO` afectados y capacidad posterior limitada
- **WHEN** AgroFlow determina el orden de reprogramación
- **THEN** aplica la misma prioridad lexicográfica estable usada en la asignación inicial

#### Scenario: Falla al persistir una reprogramación

- **GIVEN** un turno afectado y una ventana posterior candidata
- **WHEN** AgroFlow no puede persistir el cambio y su consumo de capacidad como una única operación
- **THEN** no comunica la reprogramación como confirmada ni deja capacidad consumida parcialmente
