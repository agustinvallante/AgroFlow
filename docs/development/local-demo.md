# Ejecutar la demo local desde el monorepo

La API y el frontend integrado se ejecutan en el host; n8n, Evolution API, PostgreSQL y Redis se ejecutan en Docker. Esta guía empaqueta la demo existente: no habilita un despliegue productivo ni nuevas rutas de registro de transportistas. El backend sigue siendo la fuente operativa de verdad.

## Camino rápido

Requisitos: **.NET 8 SDK**, **Node.js 22** (base de CI), Docker con Compose y, para WhatsApp, acceso a OpenAI y dos números de prueba. Todos los comandos parten de la raíz de `AgroFlow/`, salvo los `cd` indicados.

1. Preparar `infra/.env` y la configuración local del frontend según la sección siguiente. Si ya existe una instalación, conservar sus valores y volúmenes antes de iniciar nada.
2. Abrir una terminal para la **API**:

   ```bash
   cd backend/Dsw2025Tpi.Api
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
4. Crear o reutilizar una credencial Header Auth llamada `Header Auth account`: Name `apikey`, Value igual a `EVOLUTION_API_KEY` del entorno local. Seleccionarla en `Send WhatsApp reply`.
5. Guardar y **publicar/activar** el workflow. El export está inactivo y las referencias por nombre no sustituyen la vinculación de credenciales de cada instancia.

La memoria, los prompts y las herramientas se conservan. Las herramientas leen `$env.AGROFLOW_API_URL`; la respuesta lee `$env.EVOLUTION_INTERNAL_URL` y `$env.EVOLUTION_INSTANCE`. Compose habilita el acceso a `$env` para esta demo con un autor confiable: no es una configuración multiusuario productiva.

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

Sólo se confirmó una respuesta al **saludo** con la actualización Agent 3.1 / GPT-5.4-mini. No se verificó el E2E de turnos ni las tres herramientas (alta, consulta y aviso `EN_CAMINO`). El remitente real probado no está registrado en la API.

El seed contiene únicamente transportistas ficticios. La demo no ofrece una ruta soportada para registrar un número real; un remitente no reconocido debe recibir el rechazo de la API. **No editar teléfonos del código ni borrar SQLite para registrar usuarios o reiniciar ensayos.** La preparación segura de identidades de prueba sigue pendiente de un mecanismo aprobado; no confirmar un turno si la API lo rechaza.

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
| `NO_CAPACITY` | Rechazo operativo de la API; no borrar la base ni prometer asignación. |
| Respuesta con `401` | Credencial Header Auth vinculada y coherente con la clave local de Evolution. |

Detener API y Vite con `Ctrl+C`. Desde la raíz, detener Docker sin borrar datos:

```bash
docker compose --env-file infra/.env -f infra/compose.yaml stop
```

Si se necesita retirar contenedores y red, `docker compose --env-file infra/.env -f infra/compose.yaml down` conserva los volúmenes nombrados. No agregar opciones de borrado de volúmenes. Esta guía no importa nginx, perfiles VPS ni procedimientos de despliegue productivo.
