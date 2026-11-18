# Interrupciones operativas

## Purpose

Define el registro de interrupciones del ingenio y su efecto sobre los turnos para CU-007.

## Requirements

### Requirement: Gestión autorizada y unicidad activa (RN-031, RN-032)

ZafraFlow MUST permitir registrar o actualizar una interrupción solo a usuarios autorizados del ingenio y SHALL mantener como máximo una interrupción activa por ingenio en el MVP.

#### Scenario: Segunda interrupción activa

- **GIVEN** un ingenio con una interrupción activa
- **WHEN** un usuario autorizado intenta registrar otra interrupción activa
- **THEN** ZafraFlow rechaza la duplicación o exige actualizar la existente

### Requirement: Reprogramación de turnos asignados (RN-033)

ZafraFlow SHALL reprogramar automáticamente los turnos `ASIGNADO` afectados por una interrupción, respetando la disponibilidad y la política de prioridad vigente.

#### Scenario: Interrupción afecta turno asignado

- **GIVEN** una interrupción confirmada y un turno `ASIGNADO` cuya ventana queda afectada
- **WHEN** ZafraFlow procesa sus efectos
- **THEN** asigna al turno una ventana compatible posterior según capacidad y prioridad

### Requirement: Identificación sin reprogramación automática (RN-034)

ZafraFlow MUST NOT reprogramar automáticamente turnos `EN_CAMINO` o `EN_ESPERA` afectados y SHALL identificarlos para notificación.

#### Scenario: Camión próximo al ingenio

- **GIVEN** una interrupción que afecta un turno `EN_CAMINO` o `EN_ESPERA`
- **WHEN** ZafraFlow procesa sus efectos
- **THEN** conserva su ventana y estado y lo incluye entre los turnos que deben notificarse

### Requirement: Estados no afectados (RN-035)

ZafraFlow MUST NOT modificar turnos `INGRESADO`, `EN_DESCARGA` o `FINALIZADO` como consecuencia de una interrupción de recepción.

#### Scenario: Turno dentro del proceso

- **GIVEN** una interrupción activa y un turno `INGRESADO`, `EN_DESCARGA` o `FINALIZADO`
- **WHEN** ZafraFlow procesa la interrupción
- **THEN** conserva sin cambios el turno

### Requirement: Nuevas solicitudes durante la interrupción (RN-036)

ZafraFlow SHALL aceptar nuevas solicitudes durante una interrupción, pero MUST asignar únicamente ventanas posteriores al período estimado de indisponibilidad.

#### Scenario: Solicitud durante indisponibilidad

- **GIVEN** una interrupción activa con un fin estimado
- **WHEN** una solicitud elegible requiere un turno
- **THEN** ZafraFlow considera solamente ventanas posteriores al período estimado
