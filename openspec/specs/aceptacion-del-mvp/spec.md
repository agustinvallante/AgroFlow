# Aceptación del MVP

## Purpose

Define el umbral mínimo, observable y verificable para declarar que AgroFlow posee un MVP funcional. Esta capacidad integra los resultados de las demás especificaciones sin redefinir sus reglas de negocio.

La aceptación del MVP demuestra una solución funcional y reproducible; no acredita por sí sola preparación para producción ni el cumplimiento de objetivos que requieren medición en operación real.

## Requirements

### Requirement: Cobertura funcional mínima

Un candidato a MVP SHALL integrar las capacidades vigentes de acceso y autorización, datos maestros, solicitud y asignación de turnos, consulta de turnos, ciclo de vida, interrupciones operativas, monitoreo operativo, integración WhatsApp/n8n y plataforma y segregación.

Una capacidad MUST NOT omitirse silenciosamente. Toda exclusión del MVP deberá aprobarse mediante un cambio OpenSpec que actualice esta especificación y el alcance del producto.

#### Scenario: Falta una capacidad obligatoria

- **GIVEN** un candidato a MVP que no implementa una de las capacidades obligatorias
- **WHEN** el equipo evalúa su cobertura funcional
- **THEN** el candidato no se considera un MVP aceptado

#### Scenario: Capacidades presentes

- **GIVEN** un candidato que incluye todas las capacidades obligatorias
- **WHEN** el equipo inicia la evaluación
- **THEN** cada capacidad se vincula con evidencia verificable, sin que su mera presencia implique aceptación automática

### Requirement: Decisiones necesarias resueltas

Un candidato a MVP MUST NOT considerarse aceptado mientras una decisión pendiente bloquee alguno de los recorridos obligatorios. El equipo SHALL resolverla o modificar formalmente el alcance mediante OpenSpec, sin asumir implícitamente una alternativa.

#### Scenario: Decisión pendiente bloqueante

- **GIVEN** una decisión marcada como pendiente que afecta un recorrido obligatorio
- **WHEN** se evalúa el candidato
- **THEN** el recorrido y el MVP permanecen sin aceptar hasta que exista una decisión aprobada

### Requirement: Ejecución vertical con estado real

Los recorridos obligatorios SHALL atravesar el punto de entrada correspondiente, la API real, las reglas del backend y la persistencia configurada para el ambiente de aceptación.

Los datos simulados MAY utilizarse como datos iniciales controlados, pero el frontend y n8n MUST NOT presentar respuestas simuladas como resultado vigente de una operación.

#### Scenario: Interfaz conectada a datos simulados

- **GIVEN** una pantalla o flujo n8n que muestra un resultado sin consultar la API real
- **WHEN** se ejecuta un recorrido obligatorio
- **THEN** ese recorrido no satisface la aceptación del MVP

#### Scenario: Operación persistida de extremo a extremo

- **GIVEN** una operación iniciada desde su punto de entrada previsto
- **WHEN** el backend la acepta y persiste
- **THEN** todos los consumidores presentan el resultado confirmado por la API

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

### Requirement: Recorrido de interrupción operativa

El MVP SHALL demostrar que una interrupción autorizada produce los efectos establecidos en `interrupciones-operativas`, que el backend identifica los destinatarios de cualquier comunicación necesaria y que el dashboard muestra la interrupción activa.

#### Scenario: Interrupción con turnos en etapas diferentes

- **GIVEN** una interrupción que afecta turnos `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA` y turnos que ya ingresaron al proceso
- **WHEN** el backend procesa la interrupción
- **THEN** cada turno recibe únicamente el tratamiento definido para su estado y el dashboard refleja la interrupción activa

### Requirement: Integración conversacional controlada

El MVP SHALL demostrar la solicitud, consulta y el aviso `EN_CAMINO` a través de n8n y del canal de WhatsApp elegido. También SHALL demostrar al menos una notificación de interrupción dirigida a un destinatario determinado por el backend.

#### Scenario: Operación mínima por el canal

- **GIVEN** un transportista y un camión reconocidos en los datos de aceptación
- **WHEN** el transportista solicita o consulta un turno, o informa `EN_CAMINO`
- **THEN** n8n comunica únicamente el resultado vigente aceptado por la API

#### Scenario: Notificación de interrupción por el canal

- **GIVEN** una interrupción procesada y destinatarios afectados determinados por el backend
- **WHEN** n8n recibe la instrucción de notificación
- **THEN** el adaptador del canal entrega el contenido indicado sin recalcular destinatarios ni cambios de turno

### Requirement: Límite de canal reproducible y autoritativo

El límite de WhatsApp MAY utilizar un proveedor de pruebas o un adaptador reproducible que emule el contrato de entrada y salida del proveedor. MUST NOT sustituirse el canal por una invocación manual a un paso interno de n8n.

n8n MUST invocar la API real, el backend MUST seguir siendo autoritativo y los reintentos MUST NOT duplicar mutaciones.

Una captura estática, una respuesta simulada o una ejecución aislada de n8n MUST NOT considerarse evidencia suficiente.

#### Scenario: Canal de pruebas conectado al backend

- **GIVEN** un transportista y un camión reconocidos en los datos de aceptación
- **WHEN** un evento ingresa por el adaptador del canal y el transportista solicita o consulta un turno, o informa `EN_CAMINO`
- **THEN** n8n comunica únicamente el resultado vigente aceptado por la API y un reintento no duplica la operación

### Requirement: Evidencia trazable de aceptación

Cada requisito vigente que forme parte de las nueve capacidades enumeradas en "Cobertura funcional mínima" SHALL estar vinculado con evidencia de prueba automatizada o con un paso manual reproducible. Los requisitos de esta especificación y los recorridos obligatorios MUST contar, como mínimo, con evidencia integrada de extremo a extremo.

La matriz de aceptación SHALL identificar la capacidad, el requisito, el escenario, la evidencia y el resultado observado.

#### Scenario: Evidencia incompleta

- **GIVEN** un requisito vigente sin prueba ni paso de verificación reproducible
- **WHEN** se revisa la matriz de aceptación
- **THEN** el candidato permanece sin aceptar

### Requirement: Trazabilidad y recuperación mínimas

El candidato SHALL implementar los criterios aprobados de auditoría y conservación para operaciones críticas y el subconjunto de recuperación definido para el ambiente académico. Como mínimo, una mutación crítica MUST conservar evidencia de actor, momento y resultado, y SHALL existir un procedimiento probado para respaldar y restaurar los datos controlados de aceptación.

La aceptación del MVP MUST NOT exigir la demostración de un SLO productivo del 99,9 por ciento.

#### Scenario: Mutación crítica trazable

- **GIVEN** una cancelación, reprogramación o transición de estado aceptada
- **WHEN** un usuario autorizado revisa la evidencia según la política aprobada
- **THEN** puede identificar el actor, el momento y el resultado sin exponer información sensible

#### Scenario: Restauración de datos de aceptación

- **GIVEN** un respaldo generado mediante el procedimiento documentado
- **WHEN** el equipo lo restaura en el ambiente de aceptación
- **THEN** recupera los datos y relaciones necesarios para ejecutar nuevamente los recorridos obligatorios

### Requirement: Verificaciones técnicas mínimas

El candidato SHALL validar estrictamente las especificaciones, validar el contrato publicado, compilar frontend y backend y ejecutar satisfactoriamente las verificaciones documentadas del repositorio.

El MVP MUST NOT considerarse aceptado si alguna verificación obligatoria falla, si posee un defecto crítico o alto que impide un recorrido obligatorio, o si contiene secretos o datos personales reales versionados.

#### Scenario: Verificación obligatoria fallida

- **GIVEN** un candidato funcional en una demostración manual
- **WHEN** falla una validación, compilación o prueba declarada como obligatoria
- **THEN** el candidato permanece sin aceptar hasta corregir la falla o aprobar formalmente un cambio de criterio

### Requirement: Demostración repetible y persistente

El equipo SHALL ejecutar el guion completo de aceptación al menos dos veces sobre datos controlados, sin correcciones manuales en la base de datos entre pasos. El estado aceptado MUST conservarse después de reiniciar los componentes que administran la persistencia.

#### Scenario: Segunda ejecución del guion

- **GIVEN** un ambiente preparado mediante el procedimiento documentado
- **WHEN** el equipo ejecuta nuevamente el guion completo y reinicia la aplicación
- **THEN** los recorridos producen resultados consistentes y los estados persistidos siguen disponibles

### Requirement: Protección frente a ampliaciones informales

La aceptación del MVP MUST NOT exigir funcionalidades declaradas fuera del alcance vigente. Una nueva expectativa SHALL incorporarse mediante un cambio OpenSpec antes de convertirse en criterio obligatorio.

#### Scenario: Se solicita una funcionalidad fuera del MVP

- **GIVEN** una funcionalidad registrada como fuera del alcance
- **WHEN** se evalúa el candidato sin que exista un cambio OpenSpec aprobado
- **THEN** su ausencia no impide aceptar el MVP
