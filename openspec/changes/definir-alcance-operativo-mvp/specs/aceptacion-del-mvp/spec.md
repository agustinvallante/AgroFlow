## MODIFIED Requirements

### Requirement: Ambiente y datos reproducibles

El MVP SHALL ofrecer un procedimiento documentado y repetible para iniciar sus componentes, preparar datos controlados y restaurar el escenario de demostración sin editar manualmente la base de datos.

Los datos de aceptación MUST incluir los ingenios, actores, estados y relaciones necesarios para comprobar los recorridos obligatorios y el aislamiento entre ingenios.

La configuración inicial del ingenio de demostración SHALL definir ventanas de 30 minutos con cupo de dos camiones por ventana; esos valores MUST NOT tratarse como constantes globales de AgroFlow. La preparación SHALL permitir configurar otros valores por ingenio sin editar reglas de negocio.

#### Scenario: Preparación desde una instalación limpia

- **GIVEN** una instalación limpia y las dependencias documentadas
- **WHEN** un integrante sigue el procedimiento de preparación
- **THEN** obtiene un ambiente utilizable para repetir todos los recorridos de aceptación

#### Scenario: Capacidad de demostración y otro ingenio

- **GIVEN** una preparación reproducible con dos ingenios y configuraciones iniciales distintas
- **WHEN** se consultan sus ventanas
- **THEN** el ingenio de demostración presenta franjas de 30 minutos y dos cupos, mientras el otro respeta sus propios valores configurados

### Requirement: Recorrido de acceso, segregación y preparación operativa

El MVP SHALL demostrar que un usuario interno activo puede autenticarse, operar dentro del ingenio y permisos que le correspondan y preparar o utilizar los datos maestros necesarios para la gestión de turnos.

El backend MUST impedir que ese usuario consulte o modifique recursos de otro ingenio.

La evidencia SHALL incluir listado, alta, edición e inhabilitación de transportistas, camiones y fincas con los permisos de operador y supervisor acordados para el MVP.

#### Scenario: Preparación autorizada

- **GIVEN** un usuario activo y datos acordes con la matriz de permisos aprobada
- **WHEN** se autentica y prepara los datos maestros de una operación
- **THEN** los datos quedan persistidos y disponibles para los casos de uso autorizados de su ingenio

#### Scenario: Intento de acceso a otro ingenio

- **GIVEN** un usuario autenticado de un ingenio y un recurso existente de otro
- **WHEN** intenta consultarlo o modificarlo
- **THEN** la API rechaza la operación y la interfaz no expone sus datos

#### Scenario: Catálogos gestionables

- **GIVEN** un operador o supervisor autorizado y registros de prueba de su ingenio
- **WHEN** lista, registra, edita e inhabilita transportistas, camiones y fincas
- **THEN** los cambios permitidos persisten y los registros inactivos dejan de habilitar turnos nuevos sin desaparecer del historial

### Requirement: Recorrido principal de un turno

El MVP SHALL demostrar que un transportista identificado por el canal habilitado puede solicitar un turno con momento de corte y carga estimada, recibir automáticamente la próxima ventana compatible y persistida sin elegir una franja, consultar su estado e informar `EN_CAMINO`.

Un operador o supervisor autorizado SHALL poder crear un turno por la interfaz interna para su ingenio; un usuario interno autorizado SHALL poder continuar el ciclo normal definido en `turnos-ciclo-de-vida`. El listado, el detalle, el dashboard y la cola de ventanas con cupos MUST reflejar el estado vigente del mismo turno.

#### Scenario: Turno completado de extremo a extremo

- **GIVEN** datos maestros válidos, una asociación autorizada y capacidad conforme a las decisiones aprobadas
- **WHEN** el transportista solicita y consulta el turno, informa `EN_CAMINO` y el operador completa el ciclo normal
- **THEN** el turno alcanza `FINALIZADO` mediante estados persistidos y las vistas operativas reflejan el resultado vigente

#### Scenario: Turno creado internamente y visible en la cola

- **GIVEN** un operador o supervisor autorizado, datos válidos y una franja con cupo
- **WHEN** crea un turno desde la interfaz interna y vuelve a consultar la cola
- **THEN** el turno persistido ocupa un cupo en la ventana asignada y las franjas vacías también siguen visibles

### Requirement: Recorrido alternativo de cancelación

El MVP SHALL demostrar, con un turno distinto del utilizado en el recorrido principal, una cancelación solicitada por un operador o supervisor autorizado desde un estado cancelable.

La aceptación MUST comprobar que el turno permanece consultable como `CANCELADO` y que su capacidad queda nuevamente disponible.

#### Scenario: Cancelación con liberación de capacidad

- **GIVEN** un turno persistido en un estado cancelable
- **WHEN** un actor autorizado solicita cancelarlo
- **THEN** el turno permanece como `CANCELADO` y la capacidad liberada puede utilizarse conforme a la política vigente
