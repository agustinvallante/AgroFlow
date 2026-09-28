# Integración local mínima con AgroFlow API

## Objetivo

Conectar el dashboard existente a la demo local de turnos sin reescribir la aplicación. El recorrido del lunes debe permitir crear, listar, consultar y actualizar turnos persistidos por AgroFlow API.

El contrato HTTP canónico vive en `docs/contracts/openapi.yaml` del repositorio `agustinvallante/AgroFlow`. Este documento sólo explica cómo lo consume el dashboard; no redefine reglas, DTOs ni estados válidos.

## Alcance

### Incluido

- conservar React, TypeScript, Vite y la arquitectura actual;
- implementar `HttpTurnoRepository` detrás del puerto `TurnoRepository` existente;
- seleccionar fuente `mock` o `http` desde la raíz de composición;
- crear un turno manual con teléfono, patente, código de finca, momento de corte y carga estimada;
- listar turnos y aplicar los filtros soportados por la API;
- consultar el detalle vigente;
- solicitar la siguiente transición o la cancelación;
- refrescar la cola mediante polling cada 3–5 segundos;
- mostrar estados de carga, error y conflicto sin predecir el resultado localmente.

### Fuera de alcance

- login, registro, roles y sesión;
- migración a Next.js;
- CRUD de transportistas, camiones o fincas;
- interrupciones, reprogramaciones y notificaciones;
- mapa, geolocalización y posiciones simuladas;
- reportes, configuración y conversaciones conectadas a la API;
- despliegue y configuración productiva.

Las vistas fuera de alcance pueden conservar mocks deterministas durante la demo. No deben presentarse como información operativa real.

## Configuración

Crear un archivo local `.env` a partir de la convención de Vite:

```env
VITE_DATA_SOURCE=http
VITE_API_URL=http://localhost:5000
```

Valores admitidos:

- `VITE_DATA_SOURCE=mock`: respaldo local con los repositorios existentes;
- `VITE_DATA_SOURCE=http`: usa AgroFlow API.

El navegador consume `http://localhost:5000`. `host.docker.internal` es exclusivo del workflow n8n que corre dentro de Docker.

## Diseño de integración

```text
presentation
    ↓
application/usecases
    ↓
domain/repositories/TurnoRepository
    ↓
infrastructure/http/HttpTurnoRepository
    ↓
AgroFlow API
```

Archivos previstos:

```text
src/infrastructure/http/
├── ApiClient.ts
├── dto/AppointmentDto.ts
├── mappers/appointmentMapper.ts
└── repositories/HttpTurnoRepository.ts
```

`src/composition/container.ts` es el único lugar que selecciona la implementación:

```ts
const turnoRepository = import.meta.env.VITE_DATA_SOURCE === "http"
  ? new HttpTurnoRepository(import.meta.env.VITE_API_URL)
  : new MockTurnoRepository();
```

## Operaciones consumidas

| Acción del dashboard | Operación canónica |
|---|---|
| Crear turno manual | `POST /api/v1/appointments` |
| Cargar cola y filtros | `GET /api/v1/appointments` |
| Abrir detalle | `GET /api/v1/appointments/{id}` |
| Avanzar o cancelar | `POST /api/v1/appointments/{id}/transitions` |

El alta usa identificadores naturales presentes en el seed:

- `carrierPhone`;
- `truckPlate`;
- `farmCode`;
- `cutAt`;
- `estimatedLoadTons`.

El frontend no transforma esos datos en UUID ni consulta catálogos auxiliares para la demo.

## Mapeo de estados

El adaptador HTTP traduce entre los estados canónicos y el modelo visual actual. Las vistas no deben conocer valores de transporte.

| API | Dashboard actual |
|---|---|
| `ASIGNADO` | `pendiente` |
| `EN_CAMINO` | `viaje` |
| `EN_ESPERA` | `cancha` |
| `INGRESADO` | `cancha` |
| `EN_DESCARGA` | `descargando` |
| `FINALIZADO` | `completado` |
| `CANCELADO` | `cancelado` |

Si distinguir `EN_ESPERA` de `INGRESADO` requiere cambiar la entidad de presentación, debe hacerse dentro del slice de integración y no mediante reglas duplicadas en la vista.

## Reglas de implementación

- La prioridad y la ventana se muestran tal como las devuelve la API.
- El dashboard nunca calcula capacidad ni decide la siguiente transición.
- Una respuesta `409` conserva el estado visible y dispara una recarga.
- La cancelación usa la operación de transiciones con `newStatus: CANCELADO`.
- El botón **Marcar** no debe llamar a cancelar.
- `AvanzarEstadoTurno` debe quedar conectado a la acción visible correspondiente.
- El polling consulta; nunca produce transiciones automáticas.

## Tareas del responsable de dashboard

1. Crear cliente, DTO, mapper y `HttpTurnoRepository`.
2. Incorporar `VITE_DATA_SOURCE` y `VITE_API_URL` en la composición.
3. Conectar listado, filtros, detalle, alta, avance y cancelación.
4. Corregir el botón **Marcar** y exponer la acción de avance.
5. Agregar polling de la cola cada 3–5 segundos.
6. Representar `400`, `404`, `409` y `500` con mensajes claros.
7. Mantener mocks como respaldo, sin mezclar sus datos con la fuente HTTP.
8. Verificar el recorrido contra la API real local.

## Criterios de aceptación

- [ ] `npm run build` y `npm run lint` pasan.
- [ ] `VITE_DATA_SOURCE=mock` conserva el respaldo existente.
- [ ] `VITE_DATA_SOURCE=http` muestra turnos persistidos por la API.
- [ ] Un turno creado desde el dashboard aparece en la cola.
- [ ] Un turno creado por n8n aparece sin recargar manualmente la página.
- [ ] El detalle refleja el estado vigente.
- [ ] Las transiciones y cancelación sólo se consideran exitosas después de la respuesta de la API.
- [ ] La UI no calcula prioridad, ventana, capacidad ni transiciones válidas.
- [ ] Las pantallas fuera de alcance están identificadas como mock o demo.
