## MODIFIED Requirements

### Requirement: Indicadores basados en estado vigente (RN-039, RN-040)

AgroFlow SHALL calcular en el backend y sobre una misma lectura consistente los indicadores del ingenio y la fecha operativa solicitados. Sin fecha explícita, SHALL usar la fecha operativa actual en `America/Argentina/Tucuman`.

La respuesta SHALL incluir: total de turnos, cantidad en `ASIGNADO`, `EN_CAMINO`, `EN_ESPERA`, en proceso como suma de `INGRESADO` y `EN_DESCARGA`, `FINALIZADO` y `CANCELADO`; además de la zona operativa, la fecha, el instante de cálculo y la interrupción activa si existe. Un turno SHALL pertenecer a la fecha de su ventana asignada. Los consumidores MUST NOT derivar estos conteos desde filas parciales.

#### Scenario: Dashboard sin fecha explícita

- **GIVEN** un usuario autorizado
- **WHEN** consulta el dashboard sin indicar fecha
- **THEN** AgroFlow calcula todos los indicadores para la fecha operativa actual con datos persistidos vigentes

#### Scenario: Conteos de un dataset conocido

- **GIVEN** turnos del ingenio distribuidos entre todos los estados del MVP en una fecha operativa
- **WHEN** un usuario autorizado consulta el dashboard para esa fecha
- **THEN** AgroFlow devuelve el total, cada conteo definido, la suma en proceso, la zona y el instante de cálculo de forma consistente

#### Scenario: Turno fuera de la fecha operativa

- **GIVEN** un turno cuya ventana pertenece a otra fecha operativa
- **WHEN** se calculan los indicadores de la fecha consultada
- **THEN** AgroFlow excluye ese turno de todos los conteos

#### Scenario: Datos de otro ingenio

- **GIVEN** turnos de dos ingenios en la misma fecha operativa
- **WHEN** un usuario consulta el dashboard de su ingenio
- **THEN** AgroFlow calcula los indicadores sin incluir datos del otro ingenio

### Requirement: Actualización periódica permitida (RN-042A)

El frontend SHALL poder consultar nuevamente el dashboard cada 30 segundos mientras la vista esté visible, SHALL permitir un refresco manual y SHALL poder actualizar al recuperar el foco. AgroFlow MUST NOT exigir WebSockets para el MVP y cada consulta MUST obtener los indicadores nuevamente desde el backend.

#### Scenario: Refresco periódico

- **GIVEN** un usuario con el dashboard visible
- **WHEN** vence el intervalo de refresco configurado de 30 segundos
- **THEN** el frontend puede obtener nuevamente el estado vigente mediante la API sin depender de WebSockets

#### Scenario: Refresco periódico con vista visible

- **GIVEN** un usuario con el dashboard visible
- **WHEN** transcurren 30 segundos desde la consulta anterior
- **THEN** el frontend puede solicitar nuevamente al backend el estado vigente

#### Scenario: Vista no visible

- **GIVEN** un dashboard abierto en una vista no visible
- **WHEN** transcurre el intervalo de 30 segundos
- **THEN** el frontend puede pausar el refresco periódico sin modificar datos operativos

#### Scenario: Refresco manual

- **GIVEN** un usuario autorizado con el dashboard abierto
- **WHEN** solicita actualizar la información manualmente
- **THEN** el frontend vuelve a consultar al backend y muestra el instante de cálculo recibido
