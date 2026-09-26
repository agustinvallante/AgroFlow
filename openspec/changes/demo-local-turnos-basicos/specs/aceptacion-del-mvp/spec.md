# Delta: Aceptación del MVP

## ADDED Requirements

### Requirement: La demo local no redefine el MVP

AgroFlow SHALL tratar la demo local de turnos básicos como un incremento de integración y MUST NOT usar su aprobación como evidencia de aceptación del MVP completo. Las capacidades excluidas de la demo conservan íntegramente su obligatoriedad en la especificación vigente.

#### Scenario: Demo local aprobada

- **GIVEN** que el recorrido local de la issue #49 satisface sus criterios
- **WHEN** el equipo informa el resultado
- **THEN** lo identifica como demo local y no como MVP aceptado

#### Scenario: Evaluación posterior del MVP

- **GIVEN** una capacidad completa excluida de la demo local
- **WHEN** se evalúa un candidato a MVP
- **THEN** su ausencia sigue impidiendo la aceptación según la especificación vigente

### Requirement: Recorrido mínimo reproducible de la demo

La demo local SHALL permitir crear, listar, filtrar, consultar y avanzar un turno hasta `FINALIZADO`, y SHALL permitir cancelar otro turno desde un estado cancelable y observarlo como `CANCELADO`. Todas las operaciones MUST usar la API y persistencia reales del ambiente local.

#### Scenario: Recorrido normal

- **GIVEN** el ambiente local iniciado con el seed documentado
- **WHEN** se crea un turno con `carrierPhone`, `truckPlate` y `farmCode`, se consulta por el teléfono del remitente y se aplican en orden sus transiciones
- **THEN** alcanza `FINALIZADO` y cada lectura presenta el estado persistido vigente sin consultas de datos maestros ni UUIDs fijados en n8n

#### Scenario: Recorrido de cancelación

- **GIVEN** otro turno local en un estado cancelable
- **WHEN** se solicita la transición a `CANCELADO`
- **THEN** permanece consultable como cancelado y libera el cupo que utilizaba

### Requirement: Exclusiones visibles de la demo

La documentación de la demo MUST declarar que no incluye login ni registro, despliegue, CRUD de datos maestros, interrupciones, mapa o reportes. La interfaz y el chatbot MUST NOT presentar esas exclusiones como capacidades verificadas.

#### Scenario: Evidencia de la demo

- **GIVEN** una captura, video o ejecución de la demo local
- **WHEN** se describe su cobertura
- **THEN** se enumeran los casos incluidos y se mantienen explícitas las exclusiones
