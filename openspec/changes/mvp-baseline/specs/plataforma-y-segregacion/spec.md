## ADDED Requirements

### Requirement: Idempotencia persistida de mutaciones

Toda operación de creación y toda mutación crítica del baseline SHALL incluir una clave idempotente estable dentro del alcance del actor y la operación. Este alcance SHALL incluir, como mínimo, crear, editar, inhabilitar y reactivar datos maestros; crear, inhabilitar o reactivar asociaciones; crear solicitudes y asignar turnos; aplicar transiciones del ciclo de vida; cancelar turnos; crear o actualizar interrupciones; y reintentar notificaciones, de forma automática o manual, cuando corresponda.

AgroFlow MUST persistir la clave, el actor, la operación, el hash de la entrada y el resultado, y MUST conservar esa deduplicación a través de reinicios. Repetir la misma clave con la misma entrada SHALL devolver el resultado original sin aplicar nuevamente la mutación. Reutilizarla con una entrada diferente MUST producir un conflicto y MUST NOT alterar el resultado previo. La correlación SHALL acompañar la operación, pero MUST NOT sustituir la clave idempotente.

#### Scenario: Operación crítica con alcance idempotente

- **GIVEN** una operación de creación o mutación crítica del baseline
- **WHEN** AgroFlow la procesa por primera vez
- **THEN** persiste la clave, el actor, la operación, el hash de entrada y el resultado dentro del mismo alcance idempotente

#### Scenario: Reintento idéntico

- **GIVEN** una mutación confirmada con una clave idempotente
- **WHEN** el mismo actor repite la misma operación, clave y entrada
- **THEN** AgroFlow devuelve el resultado original sin duplicar efectos

#### Scenario: Clave reutilizada con otra entrada

- **GIVEN** una clave idempotente asociada a una entrada procesada
- **WHEN** el mismo alcance reutiliza la clave con una entrada diferente
- **THEN** AgroFlow rechaza la solicitud como conflicto y conserva el resultado original

#### Scenario: Reintento tras reinicio

- **GIVEN** una mutación procesada y un reinicio del backend
- **WHEN** se repite la misma intención con su clave original
- **THEN** AgroFlow recupera el resultado persistido y no reaplica la mutación

#### Scenario: Misma clave en otro alcance

- **GIVEN** una clave usada por un actor para una operación
- **WHEN** otro actor u otra operación presenta el mismo valor de clave
- **THEN** AgroFlow la evalúa dentro de su alcance propio sin exponer el resultado ajeno

### Requirement: Consistencia transaccional y concurrencia optimista

AgroFlow MUST aplicar cada mutación crítica y sus efectos obligatorios como una única transacción y SHALL usar una versión del estado persistido para detectar actualizaciones concurrentes. Una operación basada en una versión obsoleta MUST NOT sobrescribir cambios confirmados y SHALL devolver un conflicto recuperable con el estado vigente permitido para el actor.

#### Scenario: Mutaciones concurrentes sobre la misma versión

- **GIVEN** dos actores autorizados que intentan modificar el mismo recurso desde la misma versión
- **WHEN** una operación se confirma antes que la otra
- **THEN** AgroFlow conserva la primera y rechaza la segunda como conflicto sin sobrescribirla

#### Scenario: Falla parcial de una mutación crítica

- **GIVEN** una mutación que requiere actualizar estado, capacidad o evidencia asociada
- **WHEN** uno de sus efectos obligatorios no puede persistirse
- **THEN** AgroFlow no confirma ninguno de los cambios como resultado efectivo

#### Scenario: Conflicto sobre un recurso no visible

- **GIVEN** un actor sin acceso al recurso vigente
- **WHEN** su solicitud entra en conflicto con ese recurso
- **THEN** AgroFlow rechaza la operación sin revelar información fuera de su ingenio o permisos

### Requirement: Auditoría append-only de mutaciones críticas

AgroFlow SHALL registrar de forma append-only las altas, ediciones e inhabilitaciones de datos maestros, cambios de asociaciones, asignaciones, transiciones, cancelaciones, reprogramaciones, interrupciones y cambios de acceso.

Cada evento SHALL conservar ingenio, actor o servicio, instante UTC, acción, entidad, resultado, motivo cuando sea obligatorio, correlación y referencias mínimas suficientes al cambio. El registro MUST excluir contraseñas, credenciales, secretos y cuerpos completos sensibles; MUST NOT poder editarse ni borrarse mediante operaciones normales. Su consulta SHALL ser de solo lectura y respetar los permisos y el ingenio efectivo. Para el MVP académico, AgroFlow MUST conservar estos eventos durante todo el ciclo de la entrega.

#### Scenario: Mutación crítica confirmada

- **GIVEN** una mutación crítica válida
- **WHEN** AgroFlow la confirma
- **THEN** conserva en la misma unidad transaccional un evento con actor o servicio, instante, resultado, correlación e ingenio

#### Scenario: Mutación crítica rechazada

- **GIVEN** una mutación crítica que resulta rechazada
- **WHEN** la política de auditoría exige registrar el intento
- **THEN** AgroFlow conserva el resultado rechazado sin registrar secretos ni datos sensibles innecesarios

#### Scenario: Consulta autorizada de auditoría

- **GIVEN** un actor con el permiso de consulta de auditoría definido por `acceso-y-autorizacion`
- **WHEN** consulta eventos permitidos de su ingenio
- **THEN** AgroFlow devuelve evidencia de solo lectura sin eventos de otros ingenios

#### Scenario: Intento de alterar auditoría

- **GIVEN** un evento de auditoría existente
- **WHEN** un consumidor intenta editarlo o eliminarlo mediante una operación normal
- **THEN** AgroFlow rechaza la operación y conserva el evento original

### Requirement: Operación académica reproducible y recuperable

AgroFlow SHALL ofrecer un procedimiento documentado y repetible para configurar el ambiente académico, aplicar su esquema, cargar datos ficticios controlados, iniciar sus componentes, verificar su salud, reiniciarlos y restaurar un respaldo sin editar manualmente la base de datos.

El estado confirmado MUST sobrevivir reinicios y el procedimiento de recuperación SHALL restaurar las relaciones necesarias para repetir los recorridos del MVP. La configuración MUST NOT requerir secretos ni datos personales reales versionados. El baseline MUST NOT presentar esta evidencia académica como demostración de un SLO productivo.

#### Scenario: Preparación desde un ambiente limpio

- **GIVEN** las dependencias y la configuración documentadas sin secretos versionados
- **WHEN** un integrante ejecuta el procedimiento de preparación
- **THEN** obtiene un ambiente saludable con datos ficticios aptos para los recorridos del MVP

#### Scenario: Reinicio con estado persistido

- **GIVEN** operaciones confirmadas en el ambiente académico
- **WHEN** se reinician los componentes que administran la aplicación
- **THEN** AgroFlow conserva el estado persistido y las deduplicaciones requeridas

#### Scenario: Restauración reproducible

- **GIVEN** un respaldo generado por el procedimiento documentado
- **WHEN** el equipo lo restaura en un ambiente controlado
- **THEN** recupera entidades, relaciones, turnos, auditoría y metadatos necesarios para repetir la aceptación

#### Scenario: Falla de restauración

- **GIVEN** un respaldo inválido o una restauración incompleta
- **WHEN** se verifica el ambiente recuperado
- **THEN** AgroFlow no lo presenta como restauración exitosa y conserva evidencia accionable de la falla sin exponer secretos
