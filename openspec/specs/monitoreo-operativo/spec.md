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

### Requirement: Cola de ventanas y cupos del MVP

AgroFlow SHALL ofrecer al dashboard las ventanas de recepción del ingenio consultado para la fecha operativa, incluidas las franjas sin turnos, junto con su cupo configurado, ocupación vigente y cupos restantes. El frontend SHALL representar esos datos recibidos del backend sin recalcular la disponibilidad como una regla independiente. La interpretación exacta de la fecha operativa y el calendario continúa sujeta a `OD-004` y `OD-012`.

#### Scenario: Franja vacía

- **GIVEN** una ventana configurada para el ingenio sin turnos que consuman capacidad
- **WHEN** un usuario autorizado consulta la cola visual
- **THEN** la ventana aparece con ocupación cero y todos sus cupos disponibles

#### Scenario: Capacidad liberada por cancelación

- **GIVEN** una ventana cuyo turno fue cancelado y cuya cancelación ya está persistida
- **WHEN** el dashboard vuelve a consultar la cola
- **THEN** el backend informa el cupo liberado y el frontend muestra la disponibilidad vigente

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
