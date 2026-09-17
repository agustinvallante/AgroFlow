# Monitoreo operativo

## Purpose

Define la información y el comportamiento del dashboard del ingenio para CU-008.

## Requirements

### Requirement: Aislamiento del dashboard (RN-038)

AgroFlow MUST mostrar en el dashboard únicamente información del ingenio asociado al usuario autenticado.

#### Scenario: Usuario de un ingenio

- **GIVEN** un usuario autenticado asociado a un ingenio
- **WHEN** consulta el dashboard
- **THEN** AgroFlow excluye información de cualquier otro ingenio

### Requirement: Indicadores basados en estado vigente (RN-039, RN-040)

AgroFlow SHALL calcular los indicadores a partir del estado actual almacenado por el backend y SHALL usar por defecto la fecha operativa actual.

#### Scenario: Dashboard sin fecha explícita

- **GIVEN** un usuario autorizado
- **WHEN** consulta el dashboard sin indicar fecha
- **THEN** AgroFlow calcula los indicadores del día actual con datos persistidos vigentes

### Requirement: Interrupción visible (RN-041)

AgroFlow SHALL indicar en el dashboard toda interrupción activa del ingenio consultado.

#### Scenario: Ingenio interrumpido

- **GIVEN** una interrupción activa para el ingenio del usuario
- **WHEN** se obtiene el dashboard
- **THEN** la respuesta incluye la interrupción activa para que la interfaz la destaque

### Requirement: Consulta sin efectos (RN-042)

Consultar el dashboard MUST NOT modificar turnos, interrupciones, indicadores base ni otros datos operativos.

#### Scenario: Actualización de la vista

- **GIVEN** un dashboard visible
- **WHEN** el frontend vuelve a consultar sus datos
- **THEN** AgroFlow devuelve información actual sin generar cambios operativos

### Requirement: Actualización periódica permitida (RN-042A)

El frontend SHALL poder actualizar el dashboard mediante consultas periódicas y AgroFlow MUST NOT exigir WebSockets para el MVP.

#### Scenario: Refresco periódico

- **GIVEN** un usuario con el dashboard abierto
- **WHEN** vence el intervalo de refresco configurado
- **THEN** el frontend puede obtener nuevamente el estado mediante la API sin depender de WebSockets
