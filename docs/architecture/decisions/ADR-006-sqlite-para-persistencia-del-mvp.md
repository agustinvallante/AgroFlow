# ADR-006: SQLite como persistencia del MVP

- Estado: aceptada para SQLite; modo WAL condicionado a validación
- Fecha: 2026-09-29
- Alcance: almacenamiento y operación local del MVP académico

## Contexto

La API de la demo usa un archivo SQLite sólo cuando `LocalDemo:Enabled`; el código heredado usa SQL Server para su dominio anterior. El MVP necesita una base persistente, reproducible y restaurable, pero no exige despliegue distribuido. El equipo eligió **SQLite para el MVP**, aprovechando la experiencia y las migraciones de la demo sin aceptar por ello su perfil sin autenticación ni sus supuestos de un solo ingenio. Se consultaron las referencias oficiales de [WAL](https://sqlite.org/wal.html), [copias en línea](https://sqlite.org/backup.html), [cadenas de conexión de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) y [limitaciones del proveedor EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations).

## Decisión

1. El perfil MVP persistirá datos AgroFlow en SQLite, con esquema y migraciones versionados y datos de arranque repetibles. La configuración no dependerá de editar manualmente la base. El perfil `LocalDemo` existente sigue siendo una implementación parcial; `B02` debe preparar el perfil MVP con identidad y segregación de ingenios.
2. Se evaluará **WAL** para lecturas del dashboard mientras se crean o transicionan turnos. Se habilitará sólo si las pruebas del ambiente objetivo muestran un beneficio y los procedimientos de arranque, cierre, checkpoint y respaldo quedan documentados. El diario tradicional de SQLite es la alternativa válida si WAL no es necesario o no supera esas pruebas. Esta ADR no afirma que WAL ya esté habilitado.
3. El ambiente académico del MVP operará con un único host de base de datos y archivo en almacenamiento local. No se compartirá el archivo SQLite por un sistema de archivos de red entre hosts: [WAL requiere memoria compartida para sus lectores](https://sqlite.org/wal.html). SQLite permite lectores concurrentes con una escritura, pero mantiene **un solo escritor a la vez**. Una futura topología de múltiples hosts o mayor presión de escritura obliga a reevaluar el motor; SQL Server o PostgreSQL serían alternativas, sujetas a otra decisión y plan de migración.
4. Las operaciones críticas de cupo y transición deben conservar transacciones, restricciones e índices, control de carreras y política de idempotencia aprobada en `OD-006`. WAL no resuelve esos requisitos. Configurar tiempos de espera de bloqueo y conexiones conforme al [proveedor .NET](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings); no combinar `Cache=Shared` con WAL sin una justificación y pruebas, porque la documentación del proveedor desaconseja esa combinación.
5. El respaldo de una base en uso debe usar la [Online Backup API](https://sqlite.org/backup.html) o `VACUUM INTO`, no una copia ingenua del archivo principal mientras puede existir un diario WAL activo. El procedimiento debe incluir restauración ensayada, protección de datos sensibles y limpieza de archivos temporales. La puerta `G11` sigue pendiente hasta ejecutar y registrar el ejercicio.

## Validación antes de aceptar el perfil MVP

- Probar migraciones y seed desde un clon/base limpia, repetición sin duplicados, reinicio con datos conservados y aislamiento de al menos dos ingenios (`G01`, `G02`).
- Probar competencia por último cupo, múltiples lectores durante escritura, cancelación y reintentos, tanto con el modo de diario elegido como con el tiempo de espera configurado; ninguna carrera puede sobreasignar o crear duplicados (`G04`, `G05`).
- Comparar WAL con el diario tradicional bajo la carga local esperada; documentar si se habilita, el checkpoint, los archivos auxiliares y el efecto real observado. No activarlo sólo por recomendación genérica.
- Revisar las [limitaciones EF Core SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations) que afectan migraciones y traducción de tipos, especialmente fechas con zona, decimales y operaciones de esquema. Conservar pruebas de consultas/filtros sobre el proveedor real.
- Ejecutar respaldo y restauración mientras la aplicación haya procesado turnos, y demostrar que el estado persistido coincide después de reiniciar (`G11`, `G12`).

## Si más adelante se cambia a SQL Server Express

El cambio de modo de diario SQLite (tradicional ↔ WAL) no es una migración de motor ni de modelo. En cambio, pasar de SQLite a SQL Server Express **no consiste en sustituir la cadena de conexión**. EF Core requiere [migraciones separadas por proveedor](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers); no se aplican las migraciones SQLite actuales directamente sobre SQL Server.

El camino de migración será: (1) elegir la versión y el ambiente de SQL Server; (2) configurar el proveedor y generar un conjunto propio de migraciones AgroFlow; (3) adaptar y probar el SQL y manejo de errores específicos de SQLite; (4) ejecutar pruebas de índices, restricciones, consultas, fechas, capacidad y concurrencia sobre SQL Server; (5) si ya existen datos que conservar, exportarlos, transformarlos e importarlos con verificación de recuentos e identificadores; y (6) ensayar corte, respaldo y reversión antes de cambiar el ambiente activo. El contrato HTTP y las reglas del dominio no deberían cambiar por el motor.

En el corte de esta ADR, `AgroFlowDbContext` y sus migraciones son SQLite. El SQL Server presente en la solución sirve al dominio e-commerce heredado, no ofrece un esquema AgroFlow listo para usar. Hay detalles concretos que revisar al migrar: `datetime(StartAt)` en una migración, la captura de `SqliteException` para conflictos y las consultas de fecha adaptadas a limitaciones del proveedor. Para mantener acotado ese trabajo, se aísla el código dependiente del proveedor y se prueban las reglas contra la base elegida, **sin mantener dos implementaciones completas durante el MVP**. La dificultad será moderada mientras los datos sean ficticios y mayor si deben conservarse datos reales.

## Consecuencias

SQLite reduce requisitos de infraestructura para el MVP académico, pero fija un límite operativo claro: archivo local, un escritor y escala de un host. El código SQL Server heredado no pasa a ser la persistencia objetivo por coexistir en la solución; se retira o aísla según `B01`/`B02`. El motor elegido no sustituye autenticación, segregación lógica ni auditoría. Cualquier expansión a varios hosts, un servicio central o una carga de escritura incompatible con SQLite requiere una ADR nueva, migración ensayada y actualización de guías/pruebas.
