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
- solicitar el cambio al estado que elige el operador, o la cancelación;
- refrescar la cola mediante polling cada 4 segundos;
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

El navegador no llama directo a la API: pide `/api/...` y `/health` a su propio origen y el proxy de Vite (`vite.config.ts`) los reenvía a `VITE_API_URL`. Así el dashboard no depende de la política CORS del backend ni del puerto en que abra Vite. Si la API arrancó en otro puerto (por ejemplo `5142`, sin `--urls http://localhost:5000`), se corrige cambiando `VITE_API_URL` y reiniciando `npm run dev`. El proxy existe con `npm run dev` y `npm run preview`; servir `dist/` desde otro servidor requiere su propio proxy.

`host.docker.internal` es exclusivo del workflow n8n que corre dentro de Docker.

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

Archivos:

```text
src/infrastructure/http/
├── ApiClient.ts
├── dto/AppointmentDto.ts
├── mappers/appointmentMapper.ts
└── repositories/HttpTurnoRepository.ts
```

En presentación, `hooks/useColaTurnos.ts` concentra consulta, filtros, polling y estados de carga, y `components/common/CambioEstadoSelect.tsx` ofrece el cambio de estado en la cola, el detalle y el panel general.

`src/composition/container.ts` es el único lugar que selecciona la implementación:

```ts
const turnoRepository = fuenteDatos === "http"
  ? new HttpTurnoRepository("") // mismo origen; el proxy de Vite reenvía a VITE_API_URL
  : new MockTurnoRepository();
```

## Operaciones consumidas

| Acción del dashboard | Operación canónica |
|---|---|
| Crear turno manual | `POST /api/v1/appointments` |
| Cargar cola y filtros | `GET /api/v1/appointments` |
| Abrir detalle | `GET /api/v1/appointments/{id}` |
| Cambiar estado o cancelar | `POST /api/v1/appointments/{id}/transitions` |

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
| `INGRESADO` | `ingresado` |
| `EN_DESCARGA` | `descargando` |
| `FINALIZADO` | `completado` |
| `CANCELADO` | `cancelado` |

`EN_ESPERA` e `INGRESADO` se representan como estados distintos (`cancha` → "En espera", `ingresado` → "Ingresado"); la entidad `Turno` se amplió dentro de este slice. `demorado` sólo existe en los datos mock y no tiene equivalente en la API.

## Reglas de implementación

- La prioridad y la ventana se muestran tal como las devuelve la API. Flota, canal, espera y prioridad no están en el contrato: con `VITE_DATA_SOURCE=http` se muestran como "—" u ocultos, nunca calculados.
- El dashboard nunca calcula capacidad ni decide la siguiente transición.
- Una respuesta `409` conserva el estado visible y dispara una recarga.
- La cancelación usa la operación de transiciones con `newStatus: CANCELADO`.
- El polling consulta; nunca produce transiciones automáticas.

### Cambio de estado

El contrato exige indicar `newStatus`. El operador elige el destino en el selector **Cambiar estado…**, que ofrece los estados de `AppointmentStatus` salvo el actual, sin filtrarlos por secuencia, y confirma antes de enviar. El frontend envía exactamente ese valor; si la transición no es válida, la API responde `409 INVALID_TRANSITION` y se muestra el mensaje. Tampoco se deshabilitan acciones según el estado (por ejemplo, en `FINALIZADO`): sólo mientras hay una solicitud en curso.

Si en el futuro el contrato informa las transiciones permitidas (por ejemplo, un campo `allowedTransitions` en el detalle), el selector debe ofrecer esa lista. Esa decisión corresponde al contrato (Persona 1).

### Filtros

`status` y `date` se envían tal cual. `truckPlate` y `phone` se envían sólo completos, según el formato publicado en OpenAPI (patente de 6 a 10 caracteres; teléfono E.164). Mientras un filtro está incompleto no se consulta —tampoco en el polling— y la vista muestra **Filtro incompleto** con el motivo en lugar de una lista, para no presentar turnos que no coinciden. La búsqueda de texto libre filtra localmente lo ya recibido.

### Estados de carga

Cada resultado queda asociado a los filtros que lo produjeron; al cambiar un filtro no se muestran datos del filtro anterior. Del mismo modo, sólo se aplica la respuesta del último pedido de detalle del turno seleccionado: una respuesta o un error tardío de otro turno, de un polling anterior o posterior al cierre del detalle se descarta.

| Situación | Presentación |
|---|---|
| Consulta en curso sin respuesta previa | "Cargando turnos…" |
| Falla la primera consulta | Sólo el error y **Reintentar**; sin tabla ni estado vacío. |
| Respuesta `200 []` | "No hay turnos que coincidan con estos filtros." |
| Falla el polling con datos previos | La lista se conserva atenuada con **Datos desactualizados** y la hora de la última actualización exitosa; se reintenta automáticamente y el aviso desaparece al recuperarse. |

### Fechas y zona horaria

El contrato 0.3.0 emite cada `date-time` en RFC 3339 con el desplazamiento de la zona del ingenio y pide no asumir uno fijo. El dashboard muestra la hora **tal como viene escrita** (`2026-09-28T08:00:00-03:00` → `08:00`) y, en el detalle, agrega el desplazamiento recibido (`28/09/2026 05:30 (UTC-03:00)`); no convierte a la zona del navegador. Las utilidades están en `src/shared/utils/date.ts`.

Al crear un turno, el campo de corte (`datetime-local`) no tiene zona: se envía con el desplazamiento explícito de la zona del navegador donde el operador escribió la hora (por ejemplo `2026-09-28T05:30:00-03:00`). Para la demo, navegador e ingenio están en la misma zona.

## Pruebas

```bash
npm test          # Vitest + Testing Library (jsdom)
npm run typecheck
npm run lint
npm run build
```

El CI ejecuta los cuatro en `frontend/`. Los tests cubren:

- `HttpTurnoRepository`: envía el `newStatus` elegido en un solo `POST`, traduce cada estado y propaga el `409` sin decidir localmente.
- `appointmentMapper` y `shared/utils/date`: horas en la zona informada por la API, independientes de la zona del navegador, y sin inventar datos ausentes.
- `useColaTurnos`: filtros incompletos (no consulta ni muestra lista) y completos (consulta con el filtro); respuestas de detalle fuera de orden (A tras abrir B, error tardío, polling viejo del mismo turno y respuesta posterior al cierre).
- `ColaTurnosView`: error inicial frente a `200 []`, datos desactualizados y su recuperación, filtro incompleto y cambio de estado elegido por el operador.

## Tareas del responsable de dashboard

1. Crear cliente, DTO, mapper y `HttpTurnoRepository`.
2. Incorporar `VITE_DATA_SOURCE` y `VITE_API_URL` en la composición.
3. Conectar listado, filtros, detalle, alta, cambio de estado y cancelación.
4. Reemplazar el botón **Marcar** (llamaba a cancelar) por el cambio de estado elegido por el operador.
5. Agregar polling de la cola cada 4 segundos.
6. Representar `400`, `404`, `409` y `500` con mensajes claros.
7. Mantener mocks como respaldo, sin mezclar sus datos con la fuente HTTP.
8. Verificar el recorrido contra la API real local.

## Criterios de aceptación

- [x] `npm run build`, `npm run lint`, `npm run typecheck` y `npm test` pasan.
- [x] `VITE_DATA_SOURCE=mock` conserva el respaldo existente.
- [ ] `VITE_DATA_SOURCE=http` muestra turnos persistidos por la API.
- [ ] Un turno creado desde el dashboard aparece en la cola.
- [ ] Un turno creado por n8n aparece sin recargar manualmente la página.
- [ ] El detalle refleja el estado vigente.
- [ ] Las transiciones y cancelación sólo se consideran exitosas después de la respuesta de la API.
- [x] La UI no calcula prioridad, ventana, capacidad ni transiciones válidas.
- [x] Las pantallas fuera de alcance están identificadas como mock o demo.
- [x] Un filtro incompleto no muestra una lista sin filtrar.
- [x] Un fallo de carga se distingue de una respuesta vacía y los datos desactualizados se señalan.
- [x] Las horas se muestran con el desplazamiento informado por la API.

Los criterios sin marcar requieren el recorrido contra la API real local (QA).
