# Runbook de la demo local de turnos

## Propósito

Esta guía coordina la ejecución local aprobada en la issue #49. Los comandos concretos de arranque y seed deben completarse cuando existan los componentes; esta documentación no inventa scripts ausentes. El contrato HTTP canónico es [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml).

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

1. Iniciá la persistencia y la API siguiendo `backend/README.md`.
2. Ejecutá el mecanismo de seed provisto por backend.
3. Confirmá que creó un ingenio ficticio con zona `America/Argentina/Tucuman`, ventanas de 30 minutos y cupo 2 para la fecha local.
4. Anotá los teléfonos ficticios de los transportistas, las patentes de sus camiones asociados y los códigos de finca publicados por el seed; por ejemplo, `+5493815550101`, `AF123BC` y `FINCA-NORTE`.
5. Iniciá frontend y n8n con sus URLs base configuradas.
6. Verificá `GET http://localhost:5000/health` antes de probar negocio.

El seed debe ser repetible. Si el equipo necesita borrar un volumen local para volver a cero, debe confirmarlo expresamente y usar el procedimiento del componente; no se asume como paso automático de esta guía.

## Smoke test del contrato

Usá la UI, el chatbot o un cliente HTTP que respete OpenAPI.

1. Crear un turno con `carrierPhone`, `truckPlate` y `farmCode` del seed, `cutAt` válido y `estimatedLoadTons` positivo; comprobar `201`, estado `ASIGNADO` y ventana asignada. La solicitud no debe contener UUIDs.
2. Listar sin filtros; comprobar la fecha local actual y el orden ascendente por inicio de ventana.
3. Filtrar por `date`, `status=ASIGNADO`, `truckPlate` y `phone`; comprobar que los filtros se combinan y que `phone` permite la consulta de CU-003 desde el número del remitente.
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
