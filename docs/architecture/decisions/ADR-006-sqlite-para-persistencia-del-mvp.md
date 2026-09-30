# ADR-006: SQLite como persistencia del MVP

- Estado: aceptada para SQLite en el MVP académico y PostgreSQL como destino comercial; modo WAL condicionado a validación
- Fecha: 2026-09-29
- Actualización: 2026-09-30
- Alcance: almacenamiento y operación local del MVP académico; estrategia de transición previa al primer cliente real

## Contexto

La API de la demo usa un archivo SQLite sólo cuando `LocalDemo:Enabled`; el código heredado usa SQL Server para su dominio anterior. El MVP necesita una base persistente, reproducible y restaurable, pero no exige despliegue distribuido. El equipo eligió **SQLite para el MVP**, aprovechando la experiencia y las migraciones de la demo sin aceptar por ello su perfil sin autenticación ni sus supuestos de un solo ingenio. Se consultaron las referencias oficiales de [WAL](https://sqlite.org/wal.html), [copias en línea](https://sqlite.org/backup.html), [cadenas de conexión de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) y [limitaciones del proveedor EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations).

## Decisión

1. El perfil MVP persistirá datos AgroFlow en SQLite hasta la presentación académica, con datos ficticios o simulados, esquema y migraciones versionados y datos de arranque repetibles. La configuración no dependerá de editar manualmente la base. El perfil `LocalDemo` existente sigue siendo una implementación parcial; `B02` prepara la persistencia del MVP y `B03` integra identidad y segregación de ingenios. Usar datos simulados no reduce los criterios funcionales, de seguridad ni de recuperación aprobados para el MVP.
2. Se evaluará **WAL** para lecturas del dashboard mientras se crean o transicionan turnos. Se habilitará sólo si las pruebas del ambiente objetivo muestran un beneficio y los procedimientos de arranque, cierre, checkpoint y respaldo quedan documentados. El diario tradicional de SQLite es la alternativa válida si WAL no es necesario o no supera esas pruebas. Esta ADR no afirma que WAL ya esté habilitado.
3. El ambiente académico del MVP operará con un único host de base de datos y archivo en almacenamiento local. No se compartirá el archivo SQLite por un sistema de archivos de red entre hosts: [WAL requiere memoria compartida para sus lectores](https://sqlite.org/wal.html). SQLite permite lectores concurrentes con una escritura, pero mantiene **un solo escritor a la vez**. Para el primer cliente real se adopta PostgreSQL como destino previsto: la migración se realizará y validará **antes de iniciar su operación con datos reales**, no como parte de la presentación académica. Versión, alojamiento y procedimientos productivos se definirán al preparar esa entrega.
4. Las operaciones críticas de cupo y transición deben conservar transacciones, restricciones e índices, control de carreras y política de idempotencia aprobada en `OD-006`. WAL no resuelve esos requisitos. Configurar tiempos de espera de bloqueo y conexiones conforme al [proveedor .NET](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings); no combinar `Cache=Shared` con WAL sin una justificación y pruebas, porque la documentación del proveedor desaconseja esa combinación.
5. El respaldo de una base en uso debe usar la [Online Backup API](https://sqlite.org/backup.html) o `VACUUM INTO`, no una copia ingenua del archivo principal mientras puede existir un diario WAL activo. El procedimiento debe incluir restauración ensayada, protección de datos sensibles y limpieza de archivos temporales. La puerta `G11` sigue pendiente hasta ejecutar y registrar el ejercicio.
6. Durante el MVP no se mantendrán dos implementaciones completas de persistencia ni dos juegos activos de migraciones/pruebas por proveedor. Se centralizarán las dependencias específicas de SQLite y se mantendrán pruebas del comportamiento para facilitar el cambio futuro. Esta decisión no habilita PostgreSQL ni agrega alcance a las issues actuales.

## Validación antes de aceptar el perfil MVP

- Probar migraciones y seed desde un clon/base limpia, repetición sin duplicados, reinicio con datos conservados y aislamiento de al menos dos ingenios (`G01`, `G02`).
- Probar competencia por último cupo, múltiples lectores durante escritura, cancelación y reintentos, tanto con el modo de diario elegido como con el tiempo de espera configurado; ninguna carrera puede sobreasignar o crear duplicados (`G04`, `G05`).
- Comparar WAL con el diario tradicional bajo la carga local esperada; documentar si se habilita, el checkpoint, los archivos auxiliares y el efecto real observado. No activarlo sólo por recomendación genérica.
- Revisar las [limitaciones EF Core SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) que afectan migraciones y traducción de tipos, especialmente fechas con zona, decimales y operaciones de esquema. Conservar pruebas de consultas/filtros sobre el proveedor real.
- Ejecutar respaldo y restauración mientras la aplicación haya procesado turnos, y demostrar que el estado persistido coincide después de reiniciar (`G11`, `G12`).

## Transición a PostgreSQL antes del primer cliente real

El cambio de modo de diario SQLite (tradicional ↔ WAL) no es una migración de motor ni de modelo. En cambio, pasar de SQLite a PostgreSQL **no consiste en sustituir la cadena de conexión**. Las migraciones de EF Core se generan para el proveedor activo; si se mantienen varios proveedores, requieren [conjuntos separados](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers). Las migraciones SQLite actuales no se aplicarán directamente sobre PostgreSQL. SQL Server Express deja de ser el destino previsto en esta ADR; elegir otro motor requerirá revisar explícitamente la decisión.

El plan detallado se preparará contra el estado final del MVP y el entorno del cliente. Como mínimo incluirá:

1. Elegir una versión soportada de PostgreSQL y el alojamiento; configurar credenciales sin versionarlas, arranque local y entorno de pruebas/CI.
2. Configurar [Npgsql para EF Core](https://www.npgsql.org/efcore/) en una versión compatible con el stack del proyecto y generar/revisar las migraciones PostgreSQL de AgroFlow e Identity. Se conserva Identity; cambiar el motor no reemplaza la autenticación.
3. Adaptar SQL, índices filtrados, restricciones y manejo de errores específicos de SQLite. Revisar las consultas de fechas y los campos derivados usados como adaptación al proveedor anterior.
4. Persistir los instantes en UTC y conservar la zona horaria del ingenio para calendario y presentación. Npgsql permite escribir `DateTimeOffset` en `timestamp with time zone` sólo con desplazamiento cero; verificar corte, prioridad, filtros y ventanas con la [documentación de fechas del proveedor](https://www.npgsql.org/doc/types/datetime.html).
5. Ejecutar sobre PostgreSQL real las pruebas de migraciones, seed repetible, roles/segregación, consultas, último cupo concurrente, duplicados, transiciones, cancelación e idempotencia. Las pruebas que usan SQLite o dobles no demuestran por sí solas compatibilidad con PostgreSQL.
6. Probar los recorridos completos de dashboard y chatbot, respaldo/restauración y recuperación. Mantener el contrato HTTP y las reglas del dominio salvo un cambio explícitamente aprobado; no rehacer frontend o n8n por cambiar el motor.

Si sólo hay datos ficticios recreables, se puede preparar una base PostgreSQL limpia y cargar los datos iniciales del cliente sin importar los turnos simulados. Esto no autoriza borrar archivos existentes sin verificar su contenido y respaldarlos. Si hay datos que deban conservarse, se requiere exportación, transformación e importación verificadas: identificadores, relaciones, usuarios e historial, fechas, recuentos y coherencia de cupos. Se ensayará un corte controlado con respaldo y reversión antes de cambiar el entorno activo; no se traslada el archivo SQLite directamente a PostgreSQL.

En el corte inicial de esta ADR, `AgroFlowDbContext` y sus migraciones son SQLite; la presencia de código SQL Server heredado no ofrece un esquema AgroFlow listo para usar. Hay detalles concretos que revisar al migrar: `datetime(StartAt)` en una migración, la captura de `SqliteException` para conflictos y las consultas de fecha adaptadas a limitaciones del proveedor. La dificultad será moderada mientras los datos sean ficticios y mayor si deben conservarse datos reales.

### Estimación orientativa, no compromiso de entrega

Para un MVP terminado, con datos ficticios recreables y migración previa a la operación del cliente, la estimación inicial es **5–10 días hábiles de una persona dedicada al backend, con apoyo de QA**, reservando **2–3 semanas calendario** para adaptación, pruebas, correcciones y preparación del entorno. No incluye implementar funcionalidades adicionales pedidas por el cliente ni constituye una garantía de aptitud productiva.

Si el cliente ya opera con SQLite y hay datos reales e historial que preservar, la reserva inicial sería **2–4 semanas calendario**, sujeta a revisar volumen, calidad de datos, disponibilidad y entorno. Ambos rangos se recalcularán al planificar la migración con el modelo final y una prueba sobre PostgreSQL; no son plazos garantizados ni tareas de las issues actuales.

## Consecuencias

SQLite reduce requisitos de infraestructura para el MVP académico, pero fija un límite operativo claro: archivo local, un escritor y escala de un host. El código SQL Server heredado no pasa a ser la persistencia objetivo por coexistir en la solución; se retira o aísla según `B01`/`B02`. El motor elegido no sustituye autenticación, segregación lógica ni auditoría. La etapa comercial incorpora una migración ensayada a PostgreSQL, no un segundo proveedor mantenido durante el MVP. Antes de esa entrega se documentarán su arquitectura operativa, configuración, seguridad y recuperación, y se actualizarán guías y pruebas; cualquier cambio del destino acordado exige revisar la decisión.
