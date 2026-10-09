# Runbook de la demo local de turnos

## Propósito

Esta guía coordina la ejecución local aprobada en la issue #49. El backend migra y siembra SQLite automáticamente al arrancar en el perfil `LocalDemo`; no hay un paso manual de seed. El contrato HTTP canónico es [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml).

[`persona3-api-handoff.md`](persona3-api-handoff.md) registra el estado anterior de la rama aislada de Persona 3; para esta integración, seguí los pasos ejecutables de esta guía.

## Prerrequisitos

- SDK de .NET 8 instalado para iniciar el backend. Frontend y n8n sólo son necesarios para el recorrido integrado.
- Permiso de escritura en el directorio local donde se creará la base SQLite.
- Puertos libres: `5000` para API, `5173` para frontend y `5678` para n8n.
- Fecha local cubierta por las ventanas generadas por el seed.

No uses secretos ni datos personales reales. La demo no requiere login, registro ni credenciales.

## URLs

| Componente | URL desde el host |
|---|---|
| Frontend | `http://localhost:5173` |
| API | `http://localhost:5000` |
| Salud | `http://localhost:5000/health` |
| n8n | `http://localhost:5678` |

### n8n en Docker

Dentro de un contenedor, `localhost` apunta al propio contenedor. Si la API corre en el host, configurá la URL base del workflow como:

```text
http://host.docker.internal:5000
```

Docker Desktop suele resolver ese nombre. En Linux puede ser necesario agregar el equivalente a `host.docker.internal:host-gateway` en la configuración del contenedor. Si API y n8n comparten una red de Docker Compose, usá el nombre del servicio de la API en lugar de una dirección fija. Esta configuración no modifica las rutas de OpenAPI.

El navegador continúa usando `http://localhost:5000`; no debe recibir `host.docker.internal` como URL pública.

## Preparación

### Backend: arranque de la demo local (perfil LocalDemo)

Desde un clon limpio, sin instalar SQL Server/LocalDB ni configurar JWT:

Si este clon ya se usó para la demo antes del renombre, **no lo trates como un clon limpio**: seguí primero la [transición de una base existente](#actualización-desde-la-demo-anterior-sin-perder-turnos).

```bash
cd backend/AgroFlow.Api
dotnet run --launch-profile http --urls http://localhost:5000
```

- **Perfil y puerto**: `--launch-profile http` usa `Properties/launchSettings.json`, que fija `ASPNETCORE_ENVIRONMENT=Development`; `appsettings.Development.json` trae `LocalDemo:Enabled=true`. `--urls` sobrescribe el puerto predeterminado `5142` y deja la API en `5000`. El flag `LocalDemo:Enabled` activa los endpoints de turnos sin identidad; cuando es `false`, esas rutas y `/health` de la demo no se publican y SQLite no se inicializa. Identity/JWT es sólo andamiaje externo a la demo, pendiente de B03; `/healthcheck` sigue disponible.
- **Persistencia**: SQLite embebido, un archivo `agroflow-demo.db` con los auxiliares del modo de diario utilizado. Las rutas relativas de `ConnectionStrings:AgroFlowDb` se resuelven contra la raíz de contenido de la API (`backend/AgroFlow.Api/` en este arranque), no contra el directorio de la terminal. Por ejemplo, `Data Source=custom.db` apunta a `backend/AgroFlow.Api/custom.db` y `Data Source=data/custom.db` apunta a `backend/AgroFlow.Api/data/custom.db`; esas rutas relativas siguen siendo válidas cuando no hay datos históricos coincidentes. No se versionan los archivos. Para evitar una base alternativa silenciosa, el arranque comprueba el nombre histórico predeterminado y el counterpart exacto de la ruta relativa configurada contra la raíz histórica de la API. Normaliza componentes `.` y `..` antes de comprobar; aun si `..` deja esa raíz, se comprueba esa ruta resultante exacta. Para cada candidato sólo comprueba el principal y `-wal`, `-shm`, `-journal`; no explora otros nombres. Si alguno existe, exige selección explícita y aborta sin crear la base destino. Las rutas absolutas explícitas y `:memory:` no pasan por esta detección. Este cambio no modifica el modo de diario existente ni declara validado WAL para el MVP.
- **Migración y seed automáticos**: al arrancar, la API aplica las migraciones pendientes de `AgroFlowDbContext` y corre el seed antes de empezar a escuchar. Si migrar o sembrar falla, el proceso **no arranca** (fail-fast): no hay riesgo de que `/health` responda `200` sin haber inicializado. El seed es idempotente: correrlo de nuevo (reiniciar la API) no duplica datos maestros, asociaciones ni ventanas, y nunca toca turnos ya creados.
- **Fixtures fijos del seed** (siempre los mismos, para que la demo sea reproducible):

  | Dato | Valor |
  |---|---|
  | Ingenio | zona `America/Argentina/Tucuman` |
  | Transportista 1 | `+5493815550101` → camión `AF123BC` |
  | Transportista 2 | `+5493815550102` → camión `AF456DE` |
  | Fincas | `FINCA-NORTE`, `FINCA-SUR` |
  | Ventanas | 30 minutos, cupo **2** camiones, de 08:00 a 18:00 hora local, para hoy y mañana |

### Recorrido reproducible

1. Ejecutá el comando de backend anterior, luego iniciá frontend y n8n con sus URLs base.
2. Verificá `GET http://localhost:5000/health` → `200 {"status":"Healthy"}` antes de probar negocio.
3. Alta con `carrierPhone=+5493815550101`, `truckPlate=AF123BC`, `farmCode=FINCA-NORTE` → `201 ASIGNADO`.
4. Repetí con el segundo camión (`+5493815550102` / `AF456DE` / `FINCA-SUR`); como el cupo es 2, ambos turnos pueden caer en la misma ventana.

El seed debe ser repetible. Reiniciar la API no borra datos. Un reinicio del escenario desde cero requiere consentimiento explícito, respaldo previo y selección de los archivos exactos; no se ejecuta como parte de esta guía ni de la actualización.

### Actualización desde la demo anterior sin perder turnos

Git renombra los archivos versionados, **no** la SQLite ignorada. Su ubicación histórica era `backend/Dsw2025Tpi.Api/agroflow-demo.db`; el nombre anterior aparece aquí sólo para localizar datos previos, nunca como proyecto activo. No borres esa carpeta ni inicies una base nueva para resolver un error de arranque.

1. Detené la API anterior, cualquier otra instancia, herramienta SQLite, frontend con operaciones pendientes y workflow n8n que pueda escribir. Cerrá todas las conexiones antes del respaldo y no vuelvas a abrirlas mientras lo generás. Registrá los UUID y estados de los turnos conocidos y el historial de `__EFMigrationsHistory`. Si hay archivos en ambas carpetas, o la protección detecta un nombre personalizado/anidado, no los mezcles ni sobrescribas: identificá cuál contiene los datos a conservar.
2. Respaldá el conjunto en una carpeta nueva fuera del repositorio. Con todos los procesos detenidos, copiá el archivo principal y los auxiliares existentes (`-wal`, `-shm`, `-journal`) juntos. **No borres un WAL residual**: puede contener transacciones confirmadas que aún no llegaron al archivo principal. Para obtener una copia independiente de un solo archivo, usá una herramienta que implemente la [SQLite Backup API](https://www.sqlite.org/backup.html); no copies solamente el `.db` si existen auxiliares. Verificá la copia sin modificar el original. Este procedimiento de actualización no acredita por sí solo la puerta de recuperación `G11` del MVP.
3. Reutilizá la ubicación elegida con una cadena absoluta en `ConnectionStrings__AgroFlowDb` y `Mode=ReadWrite`: si el archivo no existe por una ruta incorrecta, el arranque falla en lugar de crear otro. Para una base histórica con nombre personalizado o anidada, elegí su ruta absoluta exacta; el nombre del archivo no tiene que ser `agroflow-demo.db`. La detección no elige, copia ni mueve ninguna base. No hace falta mover la base ni sus auxiliares. El ejemplo siguiente se ejecuta desde la raíz del repositorio, **después de detener los procesos**:

   ```powershell
   $ErrorActionPreference = 'Stop'
   $previousDb = (Resolve-Path -LiteralPath 'backend/Dsw2025Tpi.Api/agroflow-demo.db').Path
   $backupRoot = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'AgroFlow-backups'
   New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
   $backupDirectory = Join-Path $backupRoot ([Guid]::NewGuid().ToString('N'))
   New-Item -ItemType Directory -Path $backupDirectory | Out-Null
   foreach ($suffix in @('', '-wal', '-shm', '-journal')) {
       $sourceFile = $previousDb + $suffix
       if (Test-Path -LiteralPath $sourceFile) {
           Copy-Item -LiteralPath $sourceFile -Destination $backupDirectory -ErrorAction Stop
       }
   }
   $env:ConnectionStrings__AgroFlowDb = "Data Source=$previousDb;Mode=ReadWrite;Default Timeout=5"
   dotnet run --project backend/AgroFlow.Api/AgroFlow.Api.csproj --launch-profile http --urls http://localhost:5000
   ```

   En Linux/macOS, configurá la misma variable con la ruta absoluta válida en ese host. Guardá la configuración fuera de Git y reaplicala al abrir otra terminal; sin ella, la protección de arranque vuelve a exigir selección explícita. Si trasladás después la base a una ubicación estable de AgroFlow, hacelo con una copia consistente verificada y conservando original/respaldo; no reemplaces una base ya existente.
4. Comprobá `/health`, consultá los UUID registrados mediante `/api/v1/appointments/{id}` y verificá sus estados, maestros y cupos. Las migraciones pendientes se aplican al archivo seleccionado conservando su historial; el seed no duplica registros ni reemplaza turnos. Reiniciá y repetí la comprobación. Revisá la existencia de bases en ambas carpetas antes de limpiar nada.
5. Si falla la comprobación, detené el backend y conservá todas las copias para diagnosticar. No borres ni sobreescribas la base previa como reparación. Una reversión utiliza el conjunto respaldado completo en una ubicación separada, con procesos detenidos y configuración absoluta; no mezcla el `.db` de una versión con auxiliares de otra.

La prueba `LocalDemoDatabaseTests.Existing_demo_survives_api_rename_migration_seed_and_two_host_restarts` reproduce una base en la ubicación histórica con un turno `EN_CAMINO`, aplica la última migración y arranca dos veces la API real mediante la selección absoluta. Verifica detalle/listado, IDs de maestros y ventanas, cupo ocupado y continuidad del historial, con diario DELETE y WAL. Las pruebas de protección comprueban el nombre predeterminado, nombres personalizados y anidados (incluida la normalización de `..`), cada auxiliar SQLite, archivos con otros nombres que no son candidatos, ausencia de creación de destino, y rutas absolutas/memoria.

## Smoke test del contrato

Usá la UI, el chatbot o un cliente HTTP que respete OpenAPI.

1. Crear un turno con `carrierPhone`, `truckPlate` y `farmCode` del seed, `cutAt` válido y `estimatedLoadTons` positivo; comprobar `201`, estado `ASIGNADO` y ventana asignada. La solicitud no debe contener UUIDs.
2. Listar sin filtros; comprobar la fecha local actual y el orden ascendente por inicio de ventana.
3. Filtrar por `date`, `status=ASIGNADO`, `truckPlate` y `phone` (con el `+` codificado como `%2B`); comprobar que los filtros se combinan con AND, que sin `date` se aplican sobre la fecha local actual del ingenio, que sin coincidencias se recibe `200` con `[]` y que `phone` permite la consulta de CU-003 desde el número del remitente.
4. Consultar el UUID del turno; comprobar que la lectura no cambia sus datos.
5. Intentar otro turno activo para el mismo camión; comprobar `409` y ausencia de consumo adicional.
6. Intentar un alta con un `carrierPhone`, `truckPlate` o `farmCode` ajeno al seed; comprobar `404` sin consumo de capacidad.
7. Consultar un UUID de turno inexistente; comprobar `404` con la forma de error canónica.

Para probar n8n, el workflow debe tomar `carrierPhone` del teléfono del mensaje entrante y enviar los identificadores naturales anteriores directamente a la API. No debe consultar endpoints de datos maestros ni almacenar UUIDs fijos.

## Recorrido principal

Sobre el primer turno, solicitar una transición por vez y volver a consultar después de cada respuesta:

```text
ASIGNADO
  -> EN_CAMINO
  -> EN_ESPERA
  -> INGRESADO
  -> EN_DESCARGA
  -> FINALIZADO
```

Comprobar:

- cada respuesta refleja un estado ya persistido;
- un salto o retroceso responde `409`;
- `FINALIZADO` rechaza transiciones posteriores;
- frontend y chatbot muestran la respuesta de la API, no una predicción local.

## Recorrido de cancelación

1. Crear un segundo turno con otro camión del seed.
2. Cancelarlo desde `ASIGNADO`, `EN_CAMINO` o `EN_ESPERA` mediante la misma operación de transición y `newStatus: CANCELADO`.
3. Consultarlo y comprobar que permanece como `CANCELADO`.
4. Crear un turno que pueda usar el cupo liberado.
5. Comprobar que una cancelación desde `INGRESADO`, `EN_DESCARGA`, `FINALIZADO` o `CANCELADO` responde `409`.

## Evidencia y cierre

Registrá para cada ejecución:

- fecha y zona horaria;
- revisión o rama probada;
- resultado de `/health`;
- UUID de los dos turnos ficticios;
- resultado de alta, filtros, detalle, secuencia, conflicto y cancelación;
- consumidor usado: frontend, n8n o cliente HTTP;
- defectos observados.

Ejecutá ambos recorridos dos veces desde datos controlados. La demo está lista sólo si también pasan las validaciones OpenSpec, OpenAPI y Markdown disponibles y si la evidencia declara que login/registro, despliegue, CRUD de datos maestros, interrupciones, mapa y reportes están fuera de alcance.
