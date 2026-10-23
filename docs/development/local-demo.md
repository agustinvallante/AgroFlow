# Ejecutar la demo local desde el monorepo

La API y el frontend integrado se ejecutan en el host; n8n, Evolution API, PostgreSQL y Redis se ejecutan en Docker. Esta guía empaqueta la demo existente: no habilita un despliegue productivo ni nuevas rutas de registro de transportistas. El backend sigue siendo la fuente operativa de verdad.

## Antes de iniciar: conservar la SQLite existente

Si esta instalación ya tenía datos antes del renombre de la API, **no seguir el camino rápido como si fuera una instalación nueva**. El [runbook canónico: actualización desde la demo anterior sin perder turnos](local-demo-runbook.md#actualización-desde-la-demo-anterior-sin-perder-turnos) es la autoridad para detener las conexiones, respaldar el archivo SQLite junto con sus auxiliares y verificar la continuidad de los turnos.

Antes de iniciar `backend/AgroFlow.Api`, completar ese respaldo y configurar en la terminal de la API `ConnectionStrings__AgroFlowDb` con la **ruta absoluta del archivo existente** y `Mode=ReadWrite`. La selección explícita evita crear una base vacía si la ruta es incorrecta; debe reaplicarse al abrir otra terminal. Git no mueve los datos ignorados. **No borrar ni mover SQLite, ni iniciar una base nueva para resolver la transición.** Los comandos siguientes no sustituyen ese procedimiento.

## Camino rápido

Requisitos: **.NET 8 SDK**, **Node.js 22** (base de CI), Docker con Compose y, para WhatsApp, acceso a OpenAI y dos números de prueba. Todos los comandos parten de la raíz de `AgroFlow/`, salvo los `cd` indicados.

1. Preparar `infra/.env` y la configuración local del frontend según la sección siguiente. Si ya existe una instalación, conservar sus valores y volúmenes antes de iniciar nada.
2. Abrir una terminal para la **API**:

   ```bash
   cd backend/AgroFlow.Api
   dotnet run --launch-profile http --urls http://localhost:5000
   ```

   Comprobar `http://localhost:5000/health`. En Linux, leer primero la salvedad de red más abajo.
3. Abrir otra terminal para el **frontend integrado**, no el repositorio Dashboard separado:

   ```bash
   cd frontend
   npm ci
   npm run dev -- --host 127.0.0.1 --port 5173 --strictPort
   ```

   Abrir `http://localhost:5173`. La cola usa la API si `VITE_DATA_SOURCE=http`; las demás vistas conservan mocks, según el [README del frontend](../../frontend/README.md).
4. En una tercera terminal, desde la raíz, iniciar la **infraestructura Docker**:

   ```bash
   docker compose --env-file infra/.env -f infra/compose.yaml config --quiet
   docker compose --env-file infra/.env -f infra/compose.yaml up -d
   docker compose --env-file infra/.env -f infra/compose.yaml ps
   ```

   Esperar los cuatro servicios saludables. Abrir n8n en `http://localhost:5678` y Evolution Manager en `http://localhost:8080/manager`; luego importar y vincular como se indica abajo.

## Configuración local sin perder datos

La muestra canónica es [`infra/env.example`](../../infra/env.example); el archivo secreto de ejecución es `infra/.env`, ignorado por Git. No copiar archivos de secretos al repositorio ni pegar sus valores en issues, chats o capturas.

**Sólo para una instalación nueva**, crear los archivos si no existen. Estos comandos no sobrescriben archivos previos y restringen permisos en macOS/Linux:

```bash
(umask 077; test -e infra/.env || cp infra/env.example infra/.env)
(umask 077; test -e frontend/.env || cp frontend/.env.example frontend/.env)
```

En Windows, copiar las muestras sólo si falta el destino y restringir sus permisos mediante ACL. Editar localmente `infra/.env` y reemplazar los tres placeholders de secretos. Para generar valores nuevos, usar `openssl rand -hex 32` para `N8N_ENCRYPTION_KEY` y `EVOLUTION_API_KEY`, y `openssl rand -hex 24` para `EVOLUTION_DB_PASSWORD`. No usar esos valores nuevos sobre volúmenes existentes.

En `frontend/.env`, configurar:

```dotenv
VITE_DATA_SOURCE=http
VITE_API_URL=http://localhost:5000
```

Vite reenvía `/api` y `/health` a esa URL; reiniciarlo tras cambiar el archivo. `AGROFLOW_API_URL` en `infra/.env` apunta a `http://host.docker.internal:5000`. OpenAI se configura como credencial en n8n, no como texto en el workflow.

### Instalación existente y cambio de carpeta

El proyecto Compose se llama `agroflow-demo`. Los contenedores, la red `agroflow_demo_backend` y los volúmenes tienen nombres explícitos, independientes de la carpeta:

- `agroflow_demo_n8n_data`
- `agroflow_demo_evolution_instances`
- `agroflow_demo_evolution_postgres_data`
- `agroflow_demo_evolution_redis_data`

En el mismo Docker Engine/contexto, ejecutar desde el monorepo reutiliza esos datos. Conservar el mismo archivo de entorno local mediante un traslado privado, manteniendo sus permisos; **conservar especialmente `N8N_ENCRYPTION_KEY`** para descifrar las credenciales existentes, además de la clave de Evolution y la contraseña de PostgreSQL. No copiar los valores a documentación. No ejecutar simultáneamente las dos carpetas: nombran los mismos recursos. Cambiar de máquina o contexto Docker no transporta los volúmenes automáticamente.

### Puertos y acceso desde Linux

El puerto API predeterminado es **5000**. Si macOS lo ocupa, coordinar la alternativa **5001** en los tres lugares y reiniciar los consumidores:

| Lugar | Alternativa |
|---|---|
| Comando API | `dotnet run --launch-profile http --urls http://localhost:5001` |
| `frontend/.env` | `VITE_API_URL=http://localhost:5001` |
| `infra/.env` | `AGROFLOW_API_URL=http://host.docker.internal:5001` |

En Linux, Compose mapea `host.docker.internal` al gateway del host; ese gateway no alcanza un listener sólo loopback. Para esa sesión, la API debe escuchar en una dirección alcanzable, por ejemplo `--urls http://0.0.0.0:5000` (o `5001`). Esto puede exponer la API a la LAN: usar una red confiable y firewall que limite el acceso al puente Docker; la demo no tiene autenticación productiva. No abrir ni reenviar puertos en el router.

n8n y Evolution publican exclusivamente en `127.0.0.1`; Vite también se inicia en loopback. PostgreSQL y Redis no publican puertos al host. Evolution mantiene una conexión saliente a WhatsApp; n8n requiere salida a OpenAI. No exponer Evolution ni sus credenciales a Internet.

## Importar el workflow y vincular credenciales

1. En n8n, crear la cuenta local si la instancia es nueva. Importar [`chatbot/workflows/Chatbot.json`](../../chatbot/workflows/Chatbot.json).
2. Verificar soporte para **AI Agent 3.1**, **OpenAI Chat Model 1.3** y **gpt-5.4-mini**, incluido el acceso al modelo en la cuenta OpenAI. No restaurar el agente antiguo 2.1. Las imágenes fijadas están en la muestra de entorno.
3. Crear o reutilizar una credencial OpenAI llamada `OpenAI account` y seleccionarla en `OpenAI Chat Model`.
4. Crear o reutilizar una credencial Header Auth llamada `Header Auth account`: Name `apikey`, Value igual a `EVOLUTION_API_KEY` del entorno local. Seleccionarla **sólo** en `Send WhatsApp reply`.
5. Crear una credencial Header Auth distinta llamada `AgroFlow Service Key account`: Name `X-AgroFlow-Service-Key`, Value igual a `ChannelIntegration__ServiceKey` de la API. Seleccionarla en los cinco nodos del canal: `Register Inbound Message`, `Register Reply Intent`, `Update Send Status`, `Report Agent Failure` y `Report Reply Failure`. Sin ella, la API responde `401` y el workflow contesta `503` al webhook sin invocar al agente.
6. Guardar y **publicar/activar** el workflow. El export está inactivo y las referencias por nombre no sustituyen la vinculación de credenciales de cada instancia.

La memoria, los prompts y las herramientas de turnos se conservan. Las herramientas y los nodos del canal leen `$env.AGROFLOW_API_URL`; la respuesta lee `$env.EVOLUTION_INTERNAL_URL` y `$env.EVOLUTION_INSTANCE`. Compose habilita el acceso a `$env` para esta demo con un autor confiable: no es una configuración multiusuario productiva.

### Historial de ejecuciones de n8n

Compose fija que n8n **no guarde** los datos de ejecuciones exitosas, manuales ni el progreso (`EXECUTIONS_DATA_SAVE_ON_SUCCESS=none`, entre otras); las fallidas se conservan para diagnóstico y se depuran a las 24 h, con un máximo de 50. El workflow exportado repite esa política en sus `settings` y el validador de CI la exige. El historial autoritativo de conversaciones vive en AgroFlow API. Para depurar una ejecución exitosa, habilitar temporalmente el guardado desde la configuración del workflow en n8n y volver a deshabilitarlo; no versionar ese cambio. Las ejecuciones previas a este ajuste no se borran solas: eliminarlas desde **Executions** si contienen datos de personas.

### Webhook Evolution → n8n sin autenticación

El webhook de n8n no verifica que el evento provenga de Evolution: quien alcance la URL podría inyectar un mensaje, que la API registraría y al que el bot respondería por WhatsApp. Mitigaciones aplicadas en LocalDemo:

- n8n publica sólo en `127.0.0.1` y Evolution lo llama por la red interna de Docker (`http://n8n:5678/...`); no abrir ni reenviar ese puerto.
- La ruta del webhook es un identificador aleatorio; no compartirla.
- El workflow descarta eventos de otra instancia que `EVOLUTION_INSTANCE`, mensajes propios, grupos, difusiones y estados, y direcciones que no sean `<dígitos>@s.whatsapp.net` o `<dígitos>@lid`.
- AgroFlow API exige la clave de servicio: el webhook no la recibe ni la expone.

Pendiente de verificar con Evolution real: si la versión fijada admite encabezados propios en el webhook, agregar autenticación por encabezado al nodo `Webhook` (credencial aparte). No se activó porque, sin esa verificación, rechazaría todos los eventos.

Recorrido del workflow (detalle en el [README del chatbot](../../chatbot/README.md)): valida el evento → registra el mensaje en la API → sólo entonces responde `200` al webhook → si es repetido se detiene → si el remitente es un transportista registrado invoca al agente, si no usa una plantilla fija → registra la intención de respuesta → envía exactamente ese texto por Evolution → informa `accepted`, `failed` o `unknown`. Si la API no persiste el mensaje, responde `503` y no hay agente ni respuesta.

El editor puede advertir que no resuelve `$env` en una **vista previa**; eso no prueba una falla de ejecución. Revisar el resultado real en Executions. Un error de ejecución de acceso al entorno o de conexión sí requiere revisar la configuración; no reemplazar expresiones por secretos o URLs hardcodeadas.

## Vincular WhatsApp

Usar un **número prescindible** para el bot: Evolution/Baileys es un cliente no oficial y existe riesgo de bloqueo de la cuenta. El segundo número envía mensajes al bot; escribir desde el propio bot se filtra por `fromMe`. El filtro también descarta grupos y mensajes no conversacionales de texto.

1. Entrar a Evolution Manager con la clave local, crear o reutilizar la instancia indicada por `EVOLUTION_INSTANCE` (por defecto `agroflow-demo`), canal Baileys.
2. Mostrar el QR y escanearlo desde **WhatsApp → Dispositivos vinculados → Vincular dispositivo** en el teléfono del bot. Confirmar estado `open`.
3. Configurar el webhook de esa instancia:

   ```text
   URL: http://n8n:5678/webhook/d01e7ba4-e412-47e5-a841-dafc71257beb
   enabled: true
   byEvents: false
   base64: false
   evento: MESSAGES_UPSERT
   ```

La URL usa el servicio `n8n` dentro de Docker, no `localhost`. `/webhook/` requiere el workflow publicado/activo. `/webhook-test/` sólo sirve durante una escucha de prueba en el editor; no dejar esa ruta en Evolution como configuración normal. Cada instancia mantiene su propia sesión; no vincular el mismo bot desde varias computadoras simultáneamente.

## Qué se verificó y qué falta

Antes de este cambio sólo se había confirmado en vivo una respuesta al **saludo** con Agent 3.1 / GPT-5.4-mini. La persistencia de conversaciones se verificó **sin Docker**: el workflow exportado se ejecutó con un simulador y fixtures sintéticos de Evolution contra la API real y SQLite temporal (`node chatbot/tests/acceptance.mjs`), con Evolution, WhatsApp y el agente simulados. **No se verificó en vivo** el workflow importado en n8n, Evolution, WhatsApp ni OpenAI con este cambio, ni el E2E de turnos con las tres herramientas.

El seed contiene únicamente transportistas ficticios. Un número de prueba consentido se registra desde la vista **Transportistas** del frontend (o `POST /api/v1/drivers`) en formato E.164; un remitente no identificado recibe la plantilla fija sin invocar al agente. **Un transportista registrado así es reconocido por el canal, pero no puede solicitar turnos**: un turno exige una asociación activa con un camión, y la gestión de asociaciones sigue pendiente (OD-002, OD-013) sin ruta publicada. La vista Transportistas lo indica como "sin camión asociado". Para el recorrido de turnos usar los transportistas del seed (`+5493815550101` con `AF123BC`, `+5493815550102` con `AF456DE`), cuyo WhatsApp puede editarse sin perder la asociación. **No editar teléfonos del código ni borrar SQLite para registrar usuarios o reiniciar ensayos.** No confirmar un turno si la API lo rechaza. La [guía de demostración](local-demo-runbook.md#demostración-conversación-persistida-en-el-dashboard) muestra el recorrido completo.

Referencias autoritativas, sin duplicar DTOs ni reglas:

- [Contrato HTTP canónico](../contracts/openapi.yaml) y [perfil local](../contracts/README.md).
- [Especificación WhatsApp/n8n](../../openspec/specs/integracion-whatsapp-n8n/spec.md).
- [Alcance de la demo](../planning/local-demo-scope.md) y [decisiones abiertas](../planning/open-decisions.md).
- [Integración del frontend](../../frontend/docs/LOCAL_DEMO_INTEGRATION.md).

## Diagnóstico y parada segura

| Síntoma | Verificación |
|---|---|
| No hay respuesta | Workflow publicado, evento y URL de Evolution; buscar una ejecución en n8n. |
| El filtro detiene el mensaje | Remitente distinto del bot, mensaje de texto, no grupo. |
| Herramienta sin conexión | API iniciada, puertos coordinados y listener alcanzable desde Docker en Linux. |
| `REFERENCE_NOT_FOUND` | Identidad o referencia fuera del seed; no implica un problema de credenciales. |
| Webhook responde `503 not_persisted` | API detenida, credencial `AgroFlow Service Key account` ausente o distinta, o instancia distinta de `LocalDemo:WhatsAppInstance`. |
| Webhook responde `200 ignored` | Mensaje propio, de grupo, sin texto o de una instancia distinta de `EVOLUTION_INSTANCE`. |
| Dashboard muestra error de clave | Falta `AGROFLOW_DASHBOARD_KEY` en el entorno de Vite o no coincide con `Dashboard__ApiKey`. |
| `NO_CAPACITY` | Rechazo operativo de la API; no borrar la base ni prometer asignación. |
| Respuesta con `401` | Credencial Header Auth vinculada y coherente con la clave local de Evolution. |

Detener API y Vite con `Ctrl+C`. Desde la raíz, detener Docker sin borrar datos:

```bash
docker compose --env-file infra/.env -f infra/compose.yaml stop
```

Si se necesita retirar contenedores y red, `docker compose --env-file infra/.env -f infra/compose.yaml down` conserva los volúmenes nombrados. No agregar opciones de borrado de volúmenes. Esta guía no importa nginx, perfiles VPS ni procedimientos de despliegue productivo.
