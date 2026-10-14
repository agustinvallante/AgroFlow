# Continuidad de datos al renombrar la API

## Problema detectado en la review de #77

La cadena relativa `Data Source=agroflow-demo.db` dependía del directorio de ejecución. Git no mueve un archivo ignorado al renombrar una carpeta: arrancar desde el proyecto AgroFlow podía crear otra base y ocultar los turnos anteriores sin borrar el original. Conservar los IDs de migración no resuelve la selección del archivo.

## Decisión de implementación

- Resolver rutas relativas contra `ContentRootPath` de la API y conservar el resto de opciones SQLite.
- Antes de migrar/sembrar, detectar el nombre histórico predeterminado y el counterpart exacto de cualquier `Data Source` relativo configurado, con normalización de `.` y `..` contra la raíz histórica de la API. Se comprueba únicamente cada candidato exacto y sus sufijos `-wal`, `-shm` y `-journal`, sin escanear la carpeta. Si `..` normaliza el candidato fuera de esa raíz, se comprueba igualmente esa ruta normalizada exacta para no eludir la política de selección explícita. Si aparece cualquiera de esos archivos, abortar e indicar el runbook.
- Reutilizar una base previa mediante `ConnectionStrings__AgroFlowDb` con ruta absoluta y `Mode=ReadWrite`. Este modo falla si el archivo elegido no existe, evitando una base vacía por un error de ruta.
- No hacer una migración automática de archivos: no conocemos qué procesos los mantienen abiertos ni cuál de dos bases contiene los datos correctos. La transición documentada detiene conexiones, respalda el conjunto SQLite, selecciona el archivo y verifica UUIDs/estados después de reiniciar.
- Conservar los nombres de migración aplicados y el modelo. El nombre del proyecto y los namespaces activos son AgroFlow; el nombre anterior sólo identifica la ubicación histórica en la protección, documentación y fixture de actualización.

No cambia el contrato HTTP, la política de turnos, el modo de diario ni la decisión de motor. Una ruta relativa fresca como `data/custom.db` sigue resolviéndose bajo la raíz de contenido AgroFlow; su counterpart histórico sólo bloquea el inicio si existe exactamente allí. Una ruta absoluta explícita y `:memory:` quedan fuera de esa detección. Esta corrección no implementa la recuperación productiva ni acredita `G11`.

## Verificación

`LocalDemoDatabaseTests` comprueba resolución independiente del directorio de la terminal, archivos históricos del nombre predeterminado y del candidato relativo (incluidos nombres personalizados, rutas anidadas normalizadas y auxiliares), archivos de otros nombres no relacionados, configuración ausente y selección ReadWrite inexistente. La prueba de actualización crea una SQLite previa al último ajuste de capacidad en su ubicación histórica, conserva un turno `EN_CAMINO`, aplica la migración pendiente y consulta detalle/listado desde dos arranques de la API real. Se verifica en DELETE y WAL sin modificar bases de los integrantes.

## Integración de las PRs

Integrar #77 primero (renombre y continuidad SQLite), después #76 (replanificación y ADRs). La rama documental se reconciliará sobre la rama de #77 para revisar el resultado integrado sin conservar comandos activos del proyecto anterior. README, onboarding y guías compartidas deben preservar tanto los cambios de B01 como las decisiones y dependencias del MVP.
