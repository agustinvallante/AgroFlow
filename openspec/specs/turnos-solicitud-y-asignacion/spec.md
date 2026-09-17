# Solicitud y asignación de turnos

## Purpose

Define el comportamiento conocido para solicitar, priorizar, asignar y confirmar turnos en CU-002. Los detalles aún no aprobados se mantienen en `docs/planning/open-decisions.md`.

## Requirements

### Requirement: Asignación autoritativa en el backend (RN-008, RN-013)

El backend de AgroFlow SHALL ser el único responsable de asignar turnos y MUST NOT informar un turno como confirmado antes de persistirlo correctamente.

#### Scenario: Asignación persistida

- **GIVEN** una solicitud válida con capacidad disponible
- **WHEN** el backend asigna y persiste el turno
- **THEN** AgroFlow devuelve la confirmación con el estado persistido

#### Scenario: Falla de persistencia

- **GIVEN** una solicitud que superó las validaciones
- **WHEN** no puede persistirse la asignación
- **THEN** AgroFlow no comunica el turno como confirmado ni mantiene una reserva aparente

### Requirement: Ventana horaria obligatoria (RN-009)

Todo turno confirmado SHALL poseer una ventana horaria de llegada.

#### Scenario: Confirmación sin ventana

- **GIVEN** una solicitud de turno válida
- **WHEN** no existe una ventana que pueda asignarse
- **THEN** AgroFlow no confirma un turno incompleto

### Requirement: Factores de prioridad (RN-010, RN-011)

La evaluación de prioridad SHALL considerar el tiempo transcurrido desde el corte de la caña y SHALL distinguir entre flota propia y flota de terceros. La fórmula, los pesos y los desempates no se consideran aprobados hasta resolver `OD-001`.

#### Scenario: Datos disponibles para priorizar

- **GIVEN** dos solicitudes elegibles para capacidad limitada
- **WHEN** el backend evalúa su prioridad
- **THEN** toma en cuenta el tiempo desde el corte y el tipo de flota de cada solicitud

### Requirement: Exclusión de turnos activos por camión (RN-012)

AgroFlow MUST impedir que un mismo camión posea simultáneamente más de un turno activo.

#### Scenario: Segundo turno activo

- **GIVEN** un camión que ya posee un turno activo
- **WHEN** se intenta confirmar otro turno activo para la misma patente
- **THEN** AgroFlow rechaza la asignación sin consumir capacidad adicional

### Requirement: Camión único y activo (RN-015)

AgroFlow SHALL identificar al camión por una patente única y MUST verificar que esté activo antes de asignarle un turno.

#### Scenario: Patente inexistente o inactiva

- **GIVEN** una solicitud asociada a una patente inexistente o a un camión inactivo
- **WHEN** el backend valida la solicitud
- **THEN** AgroFlow la rechaza sin asignar una ventana

### Requirement: Alternativa ante falta de disponibilidad (RN-016)

Cuando una solicitud contenga una ventana preferida sin disponibilidad, AgroFlow SHALL asignar la próxima ventana disponible compatible con las reglas de prioridad. La forma en que se obtiene la preferencia se mantiene pendiente en `OD-003`.

#### Scenario: Ventana preferida completa

- **GIVEN** una solicitud elegible cuya ventana preferida no tiene capacidad
- **WHEN** existe una ventana posterior compatible
- **THEN** AgroFlow asigna la próxima ventana disponible de acuerdo con la política de prioridad vigente
