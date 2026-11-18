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
  | Transportista 1 (`demo-uno`) | `+5493815550101` → camión `AF123BC` |
  | Transportista 2 (`demo-dos`) | `+5493815550102` → camión `AF456DE` |
  | Transportista 3 (`demo-tres`) | `+5493815550121` → camión `AH301AB` |
  | Transportista 4 (`demo-cuatro`) | `+5493815550122` → camión `AH302AB` |
  | Transportista 5 (`demo-cinco`) | `+5493815550123` → camión `AH303AB` |
  | Fincas | `FINCA-NORTE`, `FINCA-SUR` |
  | Ventanas | 30 minutos, cupo **2** camiones, de 08:00 a 18:00 hora local, para hoy y mañana |

  Las cinco asociaciones están activas: los cinco transportistas pueden solicitar turnos. Una base existente con dos transportistas incorpora las tres posiciones nuevas al arrancar, sin perder datos.

- **Precarga privada del equipo**: si existe `config/local-demo/transportistas.private.json` (ignorado por Git), al arrancar asigna a cada posición el nombre y el WhatsApp autorizado de un integrante. Conserva el DNI, el camión y la asociación de cada posición. Ver la [guía de la precarga](../../config/local-demo/README.md).

### Canal de WhatsApp, conversaciones y transportistas

Estas rutas existen sólo en el perfil LocalDemo y **nunca son anónimas** ([contrato](../contracts/openapi.yaml), etiquetas `Canal`, `Conversaciones` y `Transportistas`):

| Consumidor | Encabezado | Configuración |
|---|---|---|
| n8n (canal) | `X-AgroFlow-Service-Key` | `ChannelIntegration__ServiceKey` |
| Dashboard (conversaciones y transportistas) | `X-AgroFlow-Dashboard-Key` | `Dashboard__ApiKey` |

- Cada clave debe tener al menos 32 bytes y ser distinta de la otra. Se cargan desde variables de entorno o `dotnet user-secrets`, nunca desde `appsettings*.json` versionados ni desde el JSON del workflow. Generación sugerida: `openssl rand -hex 32`.
- Sin clave configurada la API arranca igual (la demo de turnos no cambia) y registra un aviso: las rutas de esa clave responden `401` hasta configurarla. Una clave presente pero corta, o igual a la otra, aborta el arranque.
- La clave del dashboard es una medida de demo en loopback, no una identidad de usuario; B03 la reemplaza por JWT con ingenio.
- El seed habilita la instancia de Evolution `LocalDemo:WhatsAppInstance` (`agroflow-demo` en `appsettings.Development.json`, igual que `EVOLUTION_INSTANCE`) para el ingenio sembrado. Si usás otro nombre, configurá `LocalDemo__WhatsAppInstance` con el mismo valor que `EVOLUTION_INSTANCE`; un evento de una instancia no habilitada responde `403`.
- El seed identifica sus filas por una clave estable (`SeedRecords`): editar por la API el WhatsApp, DNI o nombre de un transportista sembrado se conserva tras reiniciar, sin duplicados ni sobrescrituras.
- Las migraciones `AddMasterDataFoundationTables`, `AddWhatsAppConversations` y `AddSeedRecords` se aplican solas al arrancar, sobre la base existente y sin perder turnos. Si la base ya repite un DNI o un WhatsApp entre transportistas, la migración aborta con el check `resolve_duplicated_transportista_dni_or_whatsapp_before_migrating` y no modifica datos.
- Los transportistas se registran con el WhatsApp en formato E.164 (`+549…`) y el encabezado `Idempotency-Key`; ese número es el que reconoce al remitente del canal.

```powershell
$env:ChannelIntegration__ServiceKey = "<clave de n8n, 32 bytes o más>"
$env:Dashboard__ApiKey = "<clave del dashboard, distinta>"
dotnet run --launch-profile http --urls http://localhost:5000
```

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

   En Linux/macOS, configurá la misma variable con la ruta absoluta válida en ese host. Guardá la configuración fuera de Git y reaplicala al abrir otra terminal; sin ella, la protección de arranque vuelve a exigir selección explícita. Si trasladás después la base a una ubicación estable de ZafraFlow, hacelo con una copia consistente verificada y conservando original/respaldo; no reemplaces una base ya existente.
4. Comprobá `/health`, consultá los UUID registrados mediante `/api/v1/appointments/{id}` y verificá sus estados, maestros y cupos. Las migraciones pendientes se aplican al archivo seleccionado conservando su historial; el seed no duplica registros ni reemplaza turnos. Reiniciá y repetí la comprobación. Revisá la existencia de bases en ambas carpetas antes de limpiar nada.
5. Si falla la comprobación, detené el backend y conservá todas las copias para diagnosticar. No borres ni sobreescribas la base previa como reparación. Una reversión utiliza el conjunto respaldado completo en una ubicación separada, con procesos detenidos y configuración absoluta; no mezcla el `.db` de una versión con auxiliares de otra.

La prueba `LocalDemoDatabaseTests.Existing_demo_survives_api_rename_migration_seed_and_two_host_restarts` reproduce una base en la ubicación histórica con un turno `EN_CAMINO`, aplica la última migración y arranca dos veces la API real mediante la selección absoluta. Verifica detalle/listado, IDs de maestros y ventanas, cupo ocupado y continuidad del historial, con diario DELETE y WAL. Las pruebas de protección comprueban el nombre predeterminado, nombres personalizados y anidados (incluida la normalización de `..`), cada auxiliar SQLite, archivos con otros nombres que no son candidatos, ausencia de creación de destino, y rutas absolutas/memoria.

## Demostración: conversación persistida en el dashboard

Objetivo: registrar un transportista, simular un mensaje de WhatsApp y verlo almacenado y mostrado en el frontend. Sin Docker, el mensaje se simula ejecutando el workflow exportado contra la API real; Evolution, WhatsApp y el agente se reemplazan por dobles y **no se envía nada a WhatsApp**. Usar sólo números ficticios (rango `+549381555…`) o de prueba consentida.

1. **Claves** (una sola vez por terminal; no versionarlas). En PowerShell:

   ```powershell
   $env:ChannelIntegration__ServiceKey = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
   $env:Dashboard__ApiKey = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
   ```

2. **API** (en esa misma terminal, desde `backend/AgroFlow.Api`): `dotnet run --launch-profile http --urls http://localhost:5000`. Verificar `GET http://localhost:5000/health` → `200`.
3. **Frontend** (otra terminal, desde `frontend/`, con la clave del dashboard copiada sin prefijo `VITE_`):

   ```powershell
   $env:AGROFLOW_DASHBOARD_KEY = "<mismo valor que Dashboard__ApiKey>"
   npm run dev
   ```

4. **Registrar el transportista**: en `http://localhost:5173` → **Transportistas** → **+ Nuevo transportista**, con nombre, DNI y WhatsApp E.164, por ejemplo `+5493815550155`. Aparece en el listado leído de la API como "sin camión asociado": el canal lo reconoce, pero todavía no puede solicitar turnos, porque la gestión de asociaciones transportista–camión sigue pendiente (OD-002, OD-013). Para probar turnos por el chatbot, usar un transportista del seed.
5. **Simular el mensaje** (tercera terminal, desde la raíz del repositorio):

   ```powershell
   $env:AGROFLOW_SERVICE_KEY = "<mismo valor que ChannelIntegration__ServiceKey>"
   node chatbot/tests/simulate-message.mjs --from +5493815550155 --text "Hola, quiero consultar mi turno" --id DEMO-0001
   ```

   Debe informar `Ingesta: HTTP 201 process`, `Remitente reconocido: sí` y `Estado de envío registrado: ACCEPTED`.
6. **Ver la conversación**: **Chatbot WhatsApp** (o **Actualizar**) muestra al transportista por nombre, el mensaje entrante y la respuesta con "Aceptado por el proveedor".
7. **Duplicado**: repetir el paso 5 con el mismo `--id`. Debe informar `duplicate_stop`, sin nueva respuesta ni mensaje en el dashboard.
8. **No registrado**: `--from +5493815550199 --id DEMO-0002`. Se registra como "Remitente no registrado" con la plantilla fija, sin agente.
9. **Persistencia**: detener la API (`Ctrl+C`), volver a iniciarla y actualizar el dashboard: la conversación sigue ahí.

Prueba automatizada equivalente, con base y claves temporales (no toca la base de la demo): `node chatbot/tests/acceptance.mjs`, después de `dotnet build backend/AgroFlow.Api` y `npm ci` en `frontend/`.

Para usar WhatsApp real hace falta n8n y Evolution en Docker con las credenciales de la [guía de la demo](local-demo.md#importar-el-workflow-y-vincular-credenciales); ese recorrido no se verificó con este cambio.

### Chat largo: verificar paginación

Como parte del ensayo, abrir una conversación ficticia con más de 50 mensajes.
La segunda petición de historial debe reutilizar nextCursor y responder 200,
sin `Cursor inválido`. Seguir la [guía de paginación y regresión de #122](conversation-pagination.md).
La corrección conserva el historial existente y no requiere reinicializar la base.

## Franjas y cupos del panel

Con la misma clave del dashboard, **Panel general → Cola del día** muestra
`GET /api/v1/windows`: todas las franjas, su capacidad, ocupados y disponibles.
La fecha vacía delega hoy a la API y los horarios conservan el desplazamiento
del ingenio. Se relee cada cuatro segundos y con **Actualizar franjas**.
Seguir el [recorrido de alta, cancelación, fechas y recuperación](dashboard-windows-demo.md).
Es un corte parcial de #73; indicadores e interrupciones permanecen pendientes.

## Recuperación de las vistas después de una falla

Chatbot y Transportistas muestran **Reintentar** ante una falla inicial.
Chatbot conserva el refresco automático cada quince segundos y Transportistas
ofrece **Actualizar** después de cargar. Una falla posterior conserva los datos
válidos con un aviso, en lugar de mostrarlos como una respuesta vacía.
El [ensayo de recuperación de #113](dashboard-read-recovery.md) describe cómo
comprobarlo sin borrar la base de la demo.

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

## Verificar visibilidad de turnos en el dashboard

Recorrido para #108 y #109, con el mismo contrato LocalDemo y datos ficticios.

1. Abrí Cola de turnos y aplicá una búsqueda, estado o patente que no coincida
   con el camión que vas a crear. Creá un turno válido con referencias del seed.
2. Comprobá la confirmación con patente, fecha, rango horario y desplazamiento
   de la ventana devuelta por la API, el detalle abierto y la selección de su
   fecha en la cola. El aviso explica que se limpiaron los demás filtros.
   Cerrá el aviso sin perder el detalle. Repetí un alta y cambiá cualquiera de
   los filtros: la confirmación anterior debe retirarse. Verificá también un
   alta con la fecha ya seleccionada y los demás filtros vacíos; el turno nuevo
   debe aparecer de inmediato, sin esperar los cuatro segundos de polling.
3. Repetí con una ventana del día siguiente. Para reproducirlo sin tocar las
   reglas ni los datos operativos, usá una base ficticia de prueba después del
   cierre de ventanas de hoy, o una base de prueba sin cupo futuro hoy y con
   cupo mañana. La API siempre decide la ventana, no el formulario.
4. Verificá que fila y detalle muestran el día y hora del ingenio, también con
   el navegador configurado en otra zona. Si una ventana cruza medianoche, sus
   dos fechas deben ser distinguibles.
5. Avanzá el turno a `EN_CAMINO`, abrí Panel general y comprobá que aparece en
   Camiones en curso aunque su ventana sea de mañana. La consulta de red debe
   incluir `active=true` sin `date`. Un turno activo atrasado también se incluye.
   Los `ASIGNADO` todavía no aparecen en esta lista.
6. Completá el recorrido hasta `FINALIZADO` o cancelá desde un estado permitido.
   Debe desaparecer tras el refresco. Volvé a la cola y verificá que su fecha y
   filtros siguen funcionando como consultas diarias.
7. Detené temporalmente la API con datos ya cargados. La lista previa debe
   permanecer marcada como desactualizada. Una carga inicial fallida informa
   el error y ofrece reintento, sin afirmar que no existen camiones.

Pruebas de regresión automatizadas, incluidas ventanas de hoy/mañana, ventana
vencida, cruce de medianoche, errores y respuestas fuera de orden:

```bash
npm --prefix frontend test
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

No se agregan endpoints. Las operaciones, el filtro `active=true` y las ventanas
RFC 3339 ya están publicados en [OpenAPI](../contracts/openapi.yaml). Los cambios
se especifican en
[visibilidad-turnos-presentacion](../../openspec/changes/visibilidad-turnos-presentacion/proposal.md).

### Evidencia de la corrección conjunta #108 / #109

Verificación del 2026-11-14 sobre la rama
`fix/demo-turnos-visibilidad-108-109`, creada desde main `8873a26`.

- Frontend: 104 pruebas aprobadas, typecheck y build correctos. Lint termina sin
  errores, con cinco advertencias preexistentes.
- Backend sin cambios: build y 352 pruebas aprobados.
- Integraciones y workflow: 106 pruebas aprobadas. Aceptación local del chatbot
  contra API/SQLite y proxy reales aprobada, con WhatsApp, Evolution y agente
  simulados.
- OpenSpec estricto, OpenAPI y enlaces Markdown locales válidos. OpenAPI conserva
  la advertencia preexistente sobre la ausencia de respuesta 4xx en `/health`.
- Chrome con zona `Asia/Tokyo`, API en LocalDemo y SQLite efímera: alta de hoy con
  filtros previos, confirmación y detalle automáticos, ciclo hasta `FINALIZADO`;
  alta de mañana, paso a `EN_CAMINO` y aparición en panel; ventana vencida todavía
  activa visible tras polling; cancelación que la retira del panel y consulta
  diaria de la fecha anterior que sigue mostrando el turno cancelado.

Las ventanas especiales de hoy y del día anterior se prepararon únicamente en
esa base temporal para reproducir los escenarios fuera del horario del seed.
La verificación no cambia el seed ni el calendario del producto. Estos resultados
acreditan visibilidad e integración local, no una prueba del canal WhatsApp real.

### Verificación de las correcciones de review — 2026-11-15

- Frontend: 113 pruebas aprobadas, tipos y build correctos; lint sin errores y
  con las cinco advertencias preexistentes.
- Regresiones nuevas: cerrar confirmación sin perder detalle, retirar el aviso
  al cambiar cualquiera de los cinco filtros, recargar con fecha ya coincidente,
  separar la selección del detalle de la recarga de lista y reutilizar detalles
  cacheados durante el polling.
- OpenSpec estricto y enlaces Markdown válidos.
- Chrome en `Asia/Tokyo`, LocalDemo y SQLite temporal: se repitieron altas de hoy
  y mañana, ciclo completo, ventana vencida, cancelación y cola diaria. Se
  verificó además el cierre del aviso, su retirada al cambiar búsqueda y que un
  ASIGNADO no aparezca en el panel hasta pasar a EN_CAMINO. Sin errores JavaScript.

La optimización del primer listado con caché vacía queda registrada como
[pendiente del MVP](../frontend/appointment-query-performance.md), con
coordinación F06/F07 y #106.

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

## Guion de presentación reproducible: dos actores, persistencia y respaldo (#111)

Guion para que cualquier integrante repita la presentación con dos transportistas ficticios sobre una base de ensayo identificada. Entrega parcial de #111 (`Refs #111`): **no acredita** WhatsApp real (#110), el mapa de #105 ni el ensayo final de otra persona; ver [Pendiente](#pendiente-de-111).

### Qué es real y qué es simulado

| Componente | Guion manual (frontend + API) | Respaldo `demo-rehearsal.mjs` |
|---|---|---|
| ZafraFlow API (LocalDemo) y SQLite | Real | Real, base temporal |
| Frontend y proxy de Vite | Real | Real (se omite con `--sin-frontend`) |
| Workflow `Chatbot.json` | n8n real sólo con Docker | Ejecutado por el simulador `n8n-simulator.mjs` |
| Evolution / WhatsApp | Real sólo en el ensayo de #110 | **Simulado** |
| Agente de IA (OpenAI) | Real sólo con n8n | **Doble** que elige las herramientas; el modelo no se simula |

Mientras el respaldo se muestre, decirlo en voz alta: *"el canal y el agente están simulados; la API, la base y el dashboard son reales"*. La simulación no prueba que WhatsApp funcione.

### Prerrequisitos y preparación

- .NET 8 SDK (verificado con el SDK 9.0.203, que compila el destino `net8.0`) y Node.js 22 (base de CI; el respaldo usa `node:sqlite`, verificado con v22.20.0).
- PowerShell para los comandos del guion manual. Docker, OpenAI y WhatsApp **no** hacen falta para el guion manual ni para el respaldo.
- Puertos libres: `5000` y `5173` (guion manual), `5098` y `5178` (respaldo).
- Desde la raíz del repositorio, una sola vez: `npm ci --prefix frontend` y `dotnet build backend/AgroFlow.Api` (Debug).

| Variable | Dónde | Valor |
|---|---|---|
| `ConnectionStrings__AgroFlowDb` | API | ruta absoluta de la base de ensayo (ver abajo) |
| `LocalDemo__TeamFile` | API | ruta que **no existe**: evita cargar `transportistas.private.json` con números reales |
| `Dashboard__ApiKey` | API | generada localmente, 32 bytes o más |
| `ChannelIntegration__ServiceKey` | API | generada localmente, distinta de la anterior |
| `AGROFLOW_DASHBOARD_KEY` | Vite | mismo valor que `Dashboard__ApiKey`, sin prefijo `VITE_` |
| `VITE_API_URL` | Vite | `http://localhost:5000` (opcional; es el valor por defecto) |

Ningún valor se versiona ni se pega en issues o capturas.

### Base temporal de ensayo

En la terminal de la API, desde la raíz del repositorio. Crea una carpeta nueva: no toca ni reinicializa ninguna base existente del equipo.

```powershell
$ensayo = Join-Path ([IO.Path]::GetTempPath()) "agroflow-ensayo-$(Get-Date -Format yyyyMMdd-HHmmss)"
New-Item -ItemType Directory $ensayo | Out-Null
$env:ConnectionStrings__AgroFlowDb = "Data Source=$ensayo\agroflow-ensayo.db;Default Timeout=5"
$env:LocalDemo__TeamFile = "$ensayo\sin-precarga.json"
$env:Dashboard__ApiKey = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
$env:ChannelIntegration__ServiceKey = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
Write-Host "Base de ensayo: $ensayo"
cd backend/AgroFlow.Api
dotnet run --launch-profile http --urls http://localhost:5000
```

Anotá la ruta impresa: **reiniciar la API en la misma terminal reutiliza el mismo archivo**; una ruta distinta crearía otra base vacía. En otra terminal: `$env:AGROFLOW_DASHBOARD_KEY = "<mismo valor que Dashboard__ApiKey>"` y `npm run dev` desde `frontend/`. Comprobar `GET http://localhost:5000/health` → `{"status":"Healthy"}`.

### Actores ficticios

Posiciones del seed (ver la [tabla de fixtures](#backend-arranque-de-la-demo-local-perfil-localdemo)); al usar un `LocalDemo__TeamFile` inexistente conservan estos valores:

| Actor | Transportista | WhatsApp | Camión | Finca |
|---|---|---|---|---|
| A | `demo-uno` | `+5493815550101` | `AF123BC` | `FINCA-NORTE` |
| B | `demo-dos` | `+5493815550102` | `AF456DE` | `FINCA-SUR` |

Las ventanas son las del seed: 20 por día (08:00–18:00, 30 minutos), cupo 2, para hoy y mañana.

### Guion manual

En una tercera terminal PowerShell, con la misma `Dashboard__ApiKey` definida, cargar los ayudantes:

```powershell
$api = "http://localhost:5000"
function Turno($tel, $patente, $finca) {
  Invoke-RestMethod -Method Post -Uri "$api/api/v1/appointments" -ContentType 'application/json' -Body (@{
    carrierPhone = $tel; truckPlate = $patente; farmCode = $finca
    cutAt = (Get-Date).AddHours(-1).ToUniversalTime().ToString("o"); estimatedLoadTons = 25 } | ConvertTo-Json)
}
function Pasar($id, $estado) {
  Invoke-RestMethod -Method Post -Uri "$api/api/v1/appointments/$id/transitions" -ContentType 'application/json' -Body (@{ newStatus = $estado } | ConvertTo-Json)
}
function Ventanas($fecha) {
  $q = if ($fecha) { "?date=$fecha" } else { "" }
  Invoke-RestMethod -Uri "$api/api/v1/windows$q" -Headers @{ 'X-AgroFlow-Dashboard-Key' = $env:Dashboard__ApiKey }
}
```

En Windows PowerShell 5.1, guardar el resultado antes de filtrar (encadenar `Ventanas | Where-Object` trata el arreglo como un solo objeto).

**Fecha**: `Ventanas` sin argumento consulta hoy, pero la API asigna la primera ventana futura con cupo y, pasado el cierre diario, puede ser mañana. Por eso hay que consultar siempre con la fecha local del `startAt` que devolvió el alta, y seleccionar esa misma fecha en **Panel general** y en **Cola de turnos** (el alta por API no cambia la fecha de la Cola).

**Cupos**: los números de abajo (1/1, 2/2, 1/2) son los de la **primera pasada en base nueva**. En cualquier otra pasada, medir la variación sobre la ocupación previa `N` de la ventana que asigne la API: alta +1, cancelación −1 y finalización sin cambio (no libera el cupo). La capacidad es 2: nunca debe haber más de 2 ocupados.

**Escenario A: turno hasta FINALIZADO**

1. Abrir **Panel general → Cola del día**: en una base nueva, 20 franjas con 0 ocupados y 2 disponibles. Al repetir el guion, esos valores no se cumplen: la ocupación previa `N` de la ventana asignada se obtiene en el paso 3 como su `occupied` menos 1.
2. `$a = Turno '+5493815550101' 'AF123BC' 'FINCA-NORTE'` → `status` `ASIGNADO`. Guardar la fecha asignada: `$fechaA = ([DateTimeOffset]$a.window.startAt).ToString('yyyy-MM-dd')`.
3. `$todas = Ventanas $fechaA; $todas | Where-Object { $_.startAt -eq $a.window.startAt }` → en la primera pasada, `occupied` 1 y `available` 1 (en otras pasadas, `N`+1). En **Panel general** y **Cola de turnos** seleccionar `$fechaA`; el panel lo refleja en hasta 4 segundos y la Cola muestra `AF123BC`.
4. `Pasar $a.id 'INGRESADO'` → `409`: no se saltan estados (PowerShell lo muestra como error).
5. `foreach ($e in 'EN_CAMINO','EN_ESPERA','INGRESADO','EN_DESCARGA','FINALIZADO') { Pasar $a.id $e }` → cada respuesta devuelve el estado pedido. Con `EN_CAMINO` el camión aparece en **Camiones en curso**; con `FINALIZADO` desaparece.
6. `Pasar $a.id 'CANCELADO'` → `409`: un turno finalizado no se cancela.
7. La ventana mantiene su `occupied` (1 en la primera pasada): finalizar conserva el cupo usado; sólo cancelar lo libera.

**Escenario B: cancelación y reutilización**

1. `$b = Turno '+5493815550102' 'AF456DE' 'FINCA-SUR'` → `ASIGNADO`. Guardar `$fechaB` igual que `$fechaA`, con `$b.window.startAt`, y consultar con `Ventanas $fechaB` (filtrando por `$b.window.startAt`). B comparte la ventana de A si esa franja sigue siendo futura y conserva cupo, es decir, si A no tomó el último. En la primera pasada en base nueva es así: queda en 2/2 y el panel la marca **Sin cupos**. Si la franja de A se llenó con A, la API asigna la próxima franja futura con lugar, y allí la ocupación sube +1 respecto de lo que tenía. Comparar siempre con la ocupación previa de la franja que efectivamente recibió B.
2. Repetir el comando anterior → `409`: el camión ya tiene un turno activo.
3. `Pasar $b.id 'CANCELADO'` → `CANCELADO`; la ventana de B baja −1 (primera pasada: 1/2 con 1 disponible).
4. `$b2 = Turno '+5493815550102' 'AF456DE' 'FINCA-SUR'` → `ASIGNADO`; `$b2.window.startAt -eq $b.window.startAt` da `True`: reutilizó el cupo liberado. La ventana de B vuelve a subir +1 (primera pasada: 2/2).
5. Para repetir el guion: `Pasar $b2.id 'CANCELADO'`.

**Persistencia tras reiniciar**

1. Detener la API con `Ctrl+C` y volver a iniciarla **en la misma terminal** (mismas variables, mismo archivo).
2. `Invoke-RestMethod "$api/api/v1/appointments/$($a.id)"` → `FINALIZADO`; `$b2` sigue `ASIGNADO` (o `CANCELADO` si ejecutaste el paso 5); `Ventanas $fechaA` y `Ventanas $fechaB` conservan las mismas ocupaciones que antes de reiniciar. Panel y Cola de turnos muestran lo mismo tras actualizar.

**Repetición**: volver a ejecutar A y B sobre la misma base es válido. Cada pasada consume un cupo (el turno finalizado de A), así que las ventanas se llenan en orden y la API asigna la próxima franja futura con lugar. Según la ocupación de partida, A y B pueden compartir franja (por ejemplo, 08:00 en la primera pasada y 08:30 en la tercera) o quedar en franjas consecutivas (A llena las 08:00 y B pasa a las 08:30 en la segunda); nunca debe haber una ventana con más ocupados que capacidad. Los valores esperados de cada pasada se calculan con la regla de cupos de arriba, no con los números de la primera.

### Respaldo si fallan WhatsApp, Evolution u OpenAI

```bash
node chatbot/tests/demo-rehearsal.mjs             # con proxy de Vite
node chatbot/tests/demo-rehearsal.mjs --sin-frontend
node chatbot/tests/demo-rehearsal.mjs --conservar # no borra la base e imprime su ruta
```

Levanta la API real con una SQLite temporal y claves aleatorias que no imprime, y ejecuta **dos veces** ambos escenarios. En cada pasada:

- **A**: el turno se solicita y se consulta por el canal **simulado** (`Chatbot.json` + agente doble, herramientas `Create Appointment` y `Lookup Appointments By Phone` contra la API real), se verifica en `GET /api/v1/windows` y en el proxy del dashboard, y avanza `EN_CAMINO → … → FINALIZADO` con `409` ante saltos y cambios posteriores.
- **B**: reserva, cancela, comprueba el cupo liberado y lo reutiliza con una nueva solicitud; luego cancela ese segundo turno para poder repetir.
- Verifica en SQLite que `Ventanas.Occupied` es igual a los turnos no cancelados de cada ventana y que ningún camión tiene dos turnos activos.
- **Reinicia la API con el mismo archivo** y exige que turnos, estados, ocupación y conversaciones sean idénticos.

Cada línea indica `[REAL]` o `[SIMULADO]`. El final imprime `Ensayo completo: … reinicio sin pérdida` y declara que el canal fue simulado; un fallo termina con código 1. No toca bases existentes ni `transportistas.private.json`. No corre en CI.

Para mostrar la conversación en el dashboard hay dos opciones: la [demostración de conversación persistida](#demostración-conversación-persistida-en-el-dashboard) (`simulate-message.mjs`, API en marcha) o `--conservar` y levantar la API sobre la ruta impresa con `ConnectionStrings__AgroFlowDb`.

### Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| `✗ El puerto de la API ya responde en http://127.0.0.1:5098/health` | Hay otro ensayo (u otra API) en ese puerto. Cerrarlo y reintentar. |
| `✗ La API terminó antes de responder (código …)` | Dos ensayos lanzados casi a la vez: el segundo no pudo escuchar. Esperar a que termine el primero. |
| `401 UNAUTHENTICATED` en las franjas o en Transportistas | Falta `AGROFLOW_DASHBOARD_KEY` en el entorno de Vite, o no coincide con `Dashboard__ApiKey`. Reiniciar Vite. |
| `409` al pedir un turno | El camión ya tiene un turno activo: finalizarlo o cancelarlo primero. |
| `ExperimentalWarning: SQLite is an experimental feature` | Aviso de Node al leer la base en el respaldo; no afecta el resultado. |

Según el contrato, la API asigna la primera ventana **futura** con cupo: si el turno cae en otra ventana o en mañana, elegir esa fecha en el panel.

### Evidencia — 2026-11-16

Sobre `main` `ea1153b` (incluye #119 y #120), Windows 11, .NET SDK 9.0.203, Node.js v22.20.0, API compilada en Debug.

- `node chatbot/tests/demo-rehearsal.mjs`: **22 comprobaciones, 2 ejecuciones, reinicio sin pérdida**, código de salida 0, en dos invocaciones independientes (32 s y 29 s, con base temporal nueva cada una). La segunda pasada asignó el turno de B a la ventana siguiente porque la de A ya estaba en 2/2.
- Guion manual con PowerShell (API temporal en otro puerto): A hasta `FINALIZADO`, `409` al saltar estados y al cancelar un finalizado, B con `409` por turno activo duplicado, cancelación, reutilización de la misma ventana (`True`) y ocupación 2/2. Tras detener y reiniciar la API con el mismo archivo: A `FINALIZADO`, B2 `ASIGNADO` y la misma ocupación. `GET /api/v1/windows` sin clave: `401`; Vite sin `AGROFLOW_DASHBOARD_KEY`: `401 UNAUTHENTICATED`.
- Puerto ocupado: un segundo ensayo concurrente termina con código 1 y el mensaje documentado, sin usar la API ajena.
- Línea base previa: `node chatbot/tests/acceptance.mjs` pasó sus 13 pasos. `node --test` de integraciones (73) y workflow (33), `validate-integrations.mjs`, OpenSpec estricto (26/26) y enlaces Markdown: correctos.
- **No verificado**: WhatsApp/Evolution/OpenAI reales, la ejecución por otro integrante, el mapa de #105 y la interfaz en un navegador (el panel y la cola se describen según la documentación de #108, #109 y #120, no se recorrieron en este ensayo).

### Pendiente de #111

- Ejecución por **otro integrante** siguiendo sólo esta guía.
- WhatsApp real con el SHA final (#110) y el mapa simulado de #105 con datos ficticios, coordinados con sus autores.
- Fijar el SHA de presentación y registrar el ensayo final:

| SHA | Fecha | Resultado | Responsables |
|---|---|---|---|
| _pendiente_ | _pendiente_ | _pendiente_ | _pendiente_ |

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
