# Runbook de la demo local de turnos

## Propósito

Esta guía coordina la ejecución local aprobada en la issue #49. Los comandos concretos de arranque y seed deben completarse cuando existan los componentes; esta documentación no inventa scripts ausentes. El contrato HTTP canónico es [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml).

La rama de Persona 3 documenta en [`persona3-api-handoff.md`](persona3-api-handoff.md) qué partes de la API pueden probarse aisladamente y qué verificación depende todavía del modelo, la persistencia y el seed de Persona 2. No se debe ejecutar este runbook como si esa integración ya existiera.

## Prerrequisitos

- Backend, frontend y n8n disponibles localmente según sus README.
- Persistencia local inicializada con el seed de demo.
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

```bash
cd backend/Dsw2025Tpi.Api
dotnet run
```

- **Perfil**: `dotnet run` usa el profile `http` de `Properties/launchSettings.json`, que fija `ASPNETCORE_ENVIRONMENT=Development`. `appsettings.Development.json` trae `LocalDemo:Enabled=true`; ese flag (no el nombre del entorno en sí) es lo que apaga el JWT heredado y el seeding de Identity/Customer y activa la demo. Fuera de Development, `appsettings.json` trae `LocalDemo:Enabled=false` y el backend heredado se comporta exactamente igual que antes de esta demo.
- **Puerto real**: el `launchSettings.json` heredado apunta a `5142`; el contrato (`docs/contracts/openapi.yaml`) y el resto de esta guía asumen `5000`. Forzalo con:
  ```bash
  ASPNETCORE_URLS=http://localhost:5000 dotnet run
  ```
- **Persistencia**: SQLite embebido, un único archivo `agroflow-demo.db` (+ `-wal`/`-shm` en modo WAL) creado junto al ejecutable, en `backend/Dsw2025Tpi.Api/`. No se versiona (ver `.gitignore`). Para reiniciar desde cero, cerrá la API y borrá esos tres archivos.
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

1. `dotnet run` como arriba (backend), luego frontend y n8n con sus URLs base.
2. Verificá `GET http://localhost:5000/health` → `200 {"status":"Healthy"}` antes de probar negocio.
3. Alta con `carrierPhone=+5493815550101`, `truckPlate=AF123BC`, `farmCode=FINCA-NORTE` → `201 ASIGNADO`.
4. Repetí con el segundo camión (`+5493815550102` / `AF456DE` / `FINCA-SUR`); como el cupo es 2, ambos turnos pueden caer en la misma ventana.

El seed debe ser repetible. Si el equipo necesita borrar la base local para volver a cero, debe confirmarlo expresamente (borrar `agroflow-demo.db*`) y no se asume como paso automático de esta guía.

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
