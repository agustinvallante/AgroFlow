# Despliegue de la demo en el VPS compartido

Cómo se publica el stack completo de la demo (API, dashboard, n8n, Evolution, PostgreSQL y Redis) en el VPS de pruebas de Castel Tech, siguiendo el mismo patrón que pad-skyroute. Los datos del host (dirección, usuario, otros proyectos) viven en los registros privados de Castel Tech y **no se escriben en este repositorio público**.

La demo local ([local-demo.md](../development/local-demo.md)) sigue siendo el plan de respaldo: este despliegue no la modifica.

**Alcance:** es la demo del perfil `LocalDemo` publicada en un solo host para la presentación, con datos ficticios o números de prueba consentidos. No es un despliegue productivo ni resuelve lo pendiente de OD-008, OD-010 y OD-011 (autenticación entre servicios fuera de `LocalDemo`, observabilidad, copias de seguridad, disponibilidad); ver [decisiones abiertas](../planning/open-decisions.md).

## Decisión

```text
https://zafraflow.casteltech.ar/        -> agroflow_web:80   (bundle + nginx con clave del dashboard)
https://zafraflow.casteltech.ar/api/... -> agroflow_web -> agroflow_api:8080 (red interna)
n8n y Evolution Manager                  -> sólo 127.0.0.1 del VPS, por túnel SSH
```

- **Un solo host y mismo origen**: el bundle llama a `/api` en su propio origen, igual que con Vite. No hace falta CORS.
- **El nginx del contenedor web cumple el papel del proxy de Vite** (`frontend/server/dashboardProxy.ts`): agrega `X-AgroFlow-Dashboard-Key` del lado del servidor sólo en conversaciones, transportistas, asociaciones y en la lectura `GET` de ventanas, camiones y fincas. Responde `404` en las rutas de n8n (`conversation-events`, `conversation-reply-intents`), descarta cualquier clave enviada por el navegador y rechaza rutas con secuencias de escape.
- **Todo el sitio va detrás de autenticación básica** (`WEB_BASIC_AUTH`). Las rutas de turnos de `LocalDemo` son anónimas y la clave del dashboard no identifica a una persona: sin esta barrera, cualquiera con la URL podría operar la demo. Si falta la variable, el sitio responde `401` a todos (falla cerrado).
- **La API corre con `LocalDemo:Enabled=true` y `ASPNETCORE_ENVIRONMENT=Production`**: Swagger queda apagado y la precarga privada del equipo nunca se aplica.
- **La SQLite vive en el volumen `agroflow_demo_api_data`** (`/app/data/agroflow-demo.db`), con ruta absoluta y `Mode=ReadWrite`. Si el archivo no está, la API no arranca en lugar de crear una base vacía.
- **n8n se migra con su volumen** (workflow, credenciales y su clave de cifrado), así que en el VPS queda igual que en local. **WhatsApp se vuelve a vincular con QR**: la sesión de Evolution no se copia.

## Archivos

| Archivo | Función |
|---|---|
| `backend/Dockerfile` | Imagen de la API (.NET 8). |
| `frontend/Dockerfile` | Build de Vite + nginx. |
| `frontend/deploy/nginx/default.conf.template` | Reglas del proxy del dashboard (el entrypoint de nginx sustituye sólo las variables `AGROFLOW_*`). |
| `frontend/deploy/nginx/10-basic-auth.sh` | Escribe el archivo de autenticación básica desde `WEB_BASIC_AUTH`. |
| `infra/compose.vps.yaml` | Override sobre `infra/compose.yaml`: agrega `api` y `web` sin puertos públicos; `web` se une a `castel_network`. |
| `infra/nginx/zafraflow.casteltech.ar.conf` | Vhost del proxy compartido `castel_proxy`. |

## Variables nuevas en `infra/.env` del VPS

| Variable | Valor |
|---|---|
| `AGROFLOW_API_URL` | `http://agroflow_api:8080` (n8n llega a la API por la red interna). |
| `AGROFLOW_SERVICE_KEY` | **La misma** clave de canal que usa la credencial `AgroFlow Service Key account` de n8n (en local: `ChannelIntegration:ServiceKey` de `dotnet user-secrets`). Mínimo 32 bytes. |
| `AGROFLOW_DASHBOARD_KEY` | Clave del dashboard (en local: `Dashboard:ApiKey`). Mínimo 32 bytes. |
| `WEB_BASIC_AUTH` | `usuario:hash`, con el hash de `openssl passwd -apr1`. **Entre comillas simples**, porque el hash contiene `$`. |

El resto de `infra/.env` se copia de la instalación local: con el mismo `N8N_ENCRYPTION_KEY` las credenciales migradas de n8n siguen siendo legibles, y con el mismo `EVOLUTION_API_KEY` la credencial de Evolution de n8n sigue siendo válida.

## Reglas del host

1. Ningún contenedor publica un puerto sin el prefijo `127.0.0.1:`. Este stack no publica nada hacia afuera.
2. El registro DNS va **proxied** (nube naranja) en Cloudflare.
3. No se toca ningún contenedor, vhost ni compose de otro proyecto. Cada paso tiene su rollback.
4. Los comandos del VPS los ejecuta una persona, de a uno.
5. Para detener: `stop`. **Nunca `down -v`**: borra los volúmenes (base, n8n, sesión de WhatsApp).

## Paso a paso

Comandos locales desde la raíz del repositorio; comandos del VPS desde `~` salvo indicación.

### 1. DNS (panel de Cloudflare, zona `casteltech.ar`)

Agregar `A zafraflow <dirección del VPS>`, **Proxied**. Rollback: borrar el registro.

### 2. Preparar el paquete de migración (local)

```bash
mkdir -p ~/agroflow-migration
sqlite3 backend/AgroFlow.Api/agroflow-demo.db ".backup '$HOME/agroflow-migration/agroflow-demo.db'"
docker stop agroflow-n8n
docker run --rm -v agroflow_demo_n8n_data:/data -v ~/agroflow-migration:/backup alpine tar czf /backup/n8n_data.tgz -C /data .
docker start agroflow-n8n
cp infra/.env ~/agroflow-migration/env
dotnet user-secrets list --project backend/AgroFlow.Api
```

El último comando muestra las dos claves que van en `AGROFLOW_SERVICE_KEY` y `AGROFLOW_DASHBOARD_KEY`. A partir de este momento, lo que se cargue en local ya no llega al VPS.

Copiar la carpeta al VPS (con el acceso de los registros de Castel Tech):

```bash
scp -r ~/agroflow-migration <usuario>@<host>:~/
```

### 3. Código y configuración (VPS)

```bash
free -h
git clone https://github.com/agustinvallante/AgroFlow.git ~/projects/AgroFlow
cd ~/projects/AgroFlow && git checkout chore/vps-deploy && git log -1 --oneline
cp ~/agroflow-migration/env infra/.env && chmod 600 infra/.env
openssl passwd -apr1
nano infra/.env
```

En `infra/.env`: poner `AGROFLOW_API_URL=http://agroflow_api:8080` y agregar `AGROFLOW_SERVICE_KEY`, `AGROFLOW_DASHBOARD_KEY` y `WEB_BASIC_AUTH='usuario:<hash>'`.

Comprobar que los puertos de administración estén libres en el VPS (otro proyecto puede usar 5678 u 8080); si están ocupados, cambiar `N8N_BIND_PORT` o `EVOLUTION_BIND_PORT`:

```bash
ss -ltn | grep -E ':(5678|8080)\b' || echo puertos-libres
```

### 4. Restaurar n8n (VPS, antes del primer arranque)

```bash
docker volume create agroflow_demo_n8n_data
docker run --rm -v agroflow_demo_n8n_data:/data -v ~/agroflow-migration:/backup alpine tar xzf /backup/n8n_data.tgz -C /data
```

### 5. Construir, copiar la base y arrancar (VPS, en `~/projects/AgroFlow`)

La primera construcción descarga las imágenes de .NET y Node y tarda varios minutos.

```bash
docker network inspect castel_network >/dev/null && echo network-ok
docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml config --quiet && echo config-ok
docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml build
docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml create
docker cp ~/agroflow-migration/agroflow-demo.db agroflow_api:/app/data/agroflow-demo.db
docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml up -d
docker ps --filter name=agroflow --format '{{.Names}}\t{{.Status}}\t{{.Ports}}'
```

Esperado: seis contenedores; en Ports sólo `127.0.0.1:` o puertos internos, nunca `0.0.0.0:`.
Rollback: `docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml stop`.

### 6. Comprobaciones internas (VPS)

```bash
docker exec agroflow_web wget -qO- http://agroflow_api:8080/health
docker exec agroflow-n8n wget -qO- http://agroflow_api:8080/health
docker logs agroflow_api 2>&1 | grep -iE 'exception|fail' | tail -5
```

### 7. Vhost (VPS, archivo nuevo)

```bash
sudo cp ~/projects/AgroFlow/infra/nginx/zafraflow.casteltech.ar.conf /opt/castel-stack/nginx/conf.d/zafraflow.casteltech.ar.conf
docker exec castel_proxy nginx -t
docker exec castel_proxy nginx -s reload
```

Si `nginx -t` falla, no recargar: `sudo rm /opt/castel-stack/nginx/conf.d/zafraflow.casteltech.ar.conf` y repetir `nginx -t`. Si los otros vhosts usan `listen 443 ssl http2;` en lugar de `http2 on;`, copiar su estilo.

### 8. Comprobaciones públicas (cualquier máquina)

```bash
curl -sI https://zafraflow.casteltech.ar | head -1
curl -s -o /dev/null -w '%{http_code}\n' -u '<usuario>:<contraseña>' https://zafraflow.casteltech.ar/api/v1/drivers
```

Esperado: `401` sin credenciales y `200` con ellas. Abrir el sitio en el navegador de la PC y de la tablet.

### 9. Mover WhatsApp al VPS

Un número de bot vinculado en dos Evolution a la vez responde dos veces. Primero se desvincula el local:

1. En el teléfono del bot: **WhatsApp → Dispositivos vinculados**, cerrar la sesión de la instancia local. Luego, en local: `docker stop agroflow-evolution agroflow-n8n`.
2. Abrir un túnel desde la computadora local (usar puertos locales distintos a los de la demo local):

   ```bash
   ssh -N -L 18080:127.0.0.1:8080 -L 15678:127.0.0.1:5678 <usuario>@<host>
   ```

3. En `http://localhost:18080/manager`, con `EVOLUTION_API_KEY`, crear la instancia `agroflow-demo` (Baileys), escanear el QR desde el teléfono del bot y configurar el webhook exactamente como en [Vincular WhatsApp](../development/local-demo.md#vincular-whatsapp).
4. En `http://localhost:15678`, confirmar que el workflow esté publicado/activo.
5. Enviar un mensaje de prueba desde un número de transportista y verlo aparecer en **Conversaciones** del sitio.

## Volver a la demo local si algo falla

1. VPS: `docker stop agroflow-evolution` (y cerrar esa sesión en **Dispositivos vinculados**).
2. Local: `docker start agroflow-evolution agroflow-n8n`, volver a escanear el QR de la instancia local, levantar la API y el frontend según [local-demo.md](../development/local-demo.md).

Los datos cargados en el VPS no vuelven a local.

## Actualizar

```bash
cd ~/projects/AgroFlow && git pull --ff-only && docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml up -d --build
```

## Después de la demo

`docker compose --env-file infra/.env -f infra/compose.yaml -f infra/compose.vps.yaml stop`, desvincular WhatsApp y, al terminar el curso, quitar el vhost y el registro DNS. Borrar volúmenes es irreversible y requiere una decisión explícita.
