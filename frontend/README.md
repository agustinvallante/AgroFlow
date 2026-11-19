# Frontend de ZafraFlow

Aplicación web de ZafraFlow, incorporada desde el prototipo
[AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard),
basada en React, TypeScript y Vite. La [ADR-005](../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)
confirma la continuidad de esta base para el MVP. Las propuestas anteriores de
Next.js y sus librerías asociadas no son decisiones vigentes; véanse los
[documentos históricos de frontend](../docs/architecture/frontend/frontend-architecture.md).

## Estado y alcance actual

Las vistas operativas del dashboard muestran datos de la API de ZafraFlow.
El mapa usa las fincas y los turnos activos de la API para simular viajes. También permite cargar viajes de prueba en el navegador.
Su alcance y prueba reproducible están en la
[guía del mapa](../docs/development/mapa-simulado-presentacion.md).

| Sección | Fuente |
|---|---|
| Cola de turnos | `/api/v1/appointments` ([`openapi.yaml`](../docs/contracts/openapi.yaml)): alta, listado, filtros, detalle, transiciones y cancelación. La fila y el detalle muestran el problema en ruta abierto (`openIncident`: tipo, si puede seguir y demora estimada) sin cambiar el estado del turno. El alta ofrece transportistas habilitados y sus patentes (`/api/v1/drivers`) y fincas activas (`/api/v1/farms`). |
| Transportistas | `/api/v1/drivers`: lista, registra (WhatsApp E.164), edita activos e inhabilita. **Reactivar** habilita el registro existente en LocalDemo. |
| Chatbot WhatsApp | `/api/v1/conversations`: conversaciones y mensajes persistidos, con remitente y estado de envío informados por la API, recorridos por cursor hasta 1000 por lectura; una lectura incompleta se muestra con aviso. En vivo: la conversación abierta se lee cada 1 s y la lista cada 3 s; una lectura no empieza mientras la anterior del mismo tipo sigue pendiente y se pausan con la pestaña oculta (al volver, se lee de inmediato). Lo nuevo mantiene al final a quien está ahí; si el operador subió, un botón indica los mensajes nuevos. Los mensajes entrantes se animan (salvo con `prefers-reduced-motion`), "El bot está respondiendo" aparece si el último mensaje entrante sigue en `PROCESSING`, y la lista suma búsqueda, filtros y novedades sin leer contadas en el navegador. Métricas y plantillas no tienen endpoint: conservan su lugar, sin datos ni acciones. |
| Panel general | Camiones en curso mediante `/api/v1/appointments?active=true`, de cualquier fecha. Franjas y cupos mediante `/api/v1/windows` (LocalDemo), con fecha opcional y refresco cada 4 segundos. Camiones en espera: turnos activos en `EN_ESPERA`. Medidas temporales para la demo: los turnos del bot son los activos que este navegador no cargó con "Nuevo turno", y la molienda se simula en el navegador, sin aviso por WhatsApp. Espera promedio, feed del chatbot y aviso a la flota siguen sin datos ni acciones, porque la API no los publica. |
| Mapa | Fincas de `/api/v1/farms`, ubicadas de forma aproximada alrededor de un ingenio de referencia (la API no publica coordenadas). Un viaje por turno activo de `/api/v1/appointments`, leídos cada 3 s, que sigue los eventos de la API: Asignado espera en su finca como Programado; En camino sale a la hora `departedAt` (si es `null`, cuando el mapa lo ve en camino) y avanza descontando las paradas resueltas (`stops`); con un problema en ruta abierto (`openIncident`) figura con demora, detenido donde estaba, y al resolverse sigue desde ese punto y llega más tarde. Al cumplirse la llegada estimada espera **En la puerta** («Esperando ingreso») y figura Llegó recién cuando la API informa En espera, Ingresado o En descarga. Con x10, x60 o +1 h cada navegador convierte las horas de la API a su propio reloj, con un registro acotado de tramos, para que ningún camión retroceda. Viajes de prueba sólo para la demo, guardados en el navegador. Recorridos sobre una red vial simulada y detalle de cada camión, con el transportista del turno. |
| Reportes y configuración | Conservan su diseño, sin datos y con las acciones deshabilitadas. |

No hay requisito de conectar todas las pantallas ahora. El backend sigue siendo
autoridad sobre reglas y estado.

Antes de ampliar un flujo, consultar las
[especificaciones vigentes](../openspec/specs/), los
[contratos compartidos](../docs/contracts/README.md), las decisiones técnicas
aceptadas y las [decisiones abiertas](../docs/planning/open-decisions.md). La
integración de datos maestros, autenticación web y demás recorridos espera sus
contratos y dependencias aprobados; sus propuestas documentales no publican
rutas utilizables.

### Visibilidad de turnos en la presentación

El alta manual confirma el turno devuelto por la API, muestra la fecha y ventana
asignadas, abre su detalle y selecciona su fecha en la cola. Limpia estado,
patente, teléfono y búsqueda anteriores y lo informa en pantalla, para que un
turno de mañana sea accesible aunque antes se estuviera consultando hoy u otro
camión. El aviso puede cerrarse y desaparece al cambiar un filtro. Un error del
POST conserva el formulario y los filtros. Cada alta dispara una recarga
explícita del listado aunque la fecha ya estuviera seleccionada, sin depender
de abrir el detalle.

La cola conserva su consulta diaria y sus filtros. Camiones en curso usa una
consulta independiente de activos sin fecha, y muestra sólo los estados en
curso, sin incluir los asignados que todavía no iniciaron el viaje. Ambas vistas
conservan el refresco cada cuatro segundos y los avisos de error/desactualización.
Las ventanas muestran las fechas y el desplazamiento recibidos del ingenio,
incluyendo ambos días si cruzan la medianoche.

Especificación del cambio en
[`visibilidad-turnos-presentacion`](../openspec/changes/visibilidad-turnos-presentacion/proposal.md).
Ver el [recorrido de verificación](../docs/development/local-demo-runbook.md#verificar-visibilidad-de-turnos-en-el-dashboard).
El [pendiente de rendimiento del MVP](../docs/frontend/appointment-query-performance.md)
describe el coste de completar resúmenes con detalles, la caché actual y la
adaptación futura al listado con proyección completa.

### Recuperación ante fallas de API

Chatbot y Transportistas ofrecen **Reintentar** si falla la carga inicial.
Chatbot también se recupera automáticamente con su refresco de tres segundos.
Una falla posterior conserva los datos válidos y muestra el aviso de
desactualización; Transportistas permite repetir la lectura con **Actualizar**.
Las respuestas viejas se descartan para conservar únicamente los mensajes de
la conversación vigente.

Ver [comportamiento y ensayo de recuperación](../docs/development/dashboard-read-recovery.md)
y [OpenSpec](../openspec/changes/recuperar-cargas-dashboard/proposal.md).

## Cómo correrlo

```bash
npm ci
npm run dev       # entorno de desarrollo
npm run build     # build de producción (queda en dist/)
npm run preview   # sirve el build de producción localmente
npm test          # tests (Vitest + Testing Library)
npm run typecheck # verificación de tipos
npm run lint      # oxlint
```

Copiar `.env.example` como `.env` y configurar `VITE_API_URL`; reiniciar Vite
después de cambiarlo. El dashboard necesita la API en ejecución. Vite reenvía
`/api/...` a la URL configurada durante `dev` y `preview`; servir `dist/` desde
otro servidor requiere su propio proxy.

### Clave del dashboard (intermediario local)

Conversaciones, transportistas, asociaciones, ventanas y los catálogos de camiones y fincas usan `X-AgroFlow-Dashboard-Key` en las solicitudes del dashboard. El navegador
nunca la conoce: [`server/dashboardProxy.ts`](server/dashboardProxy.ts) la
agrega en el proxy de Vite, del lado de Node, leyendo `AGROFLOW_DASHBOARD_KEY`
(sin prefijo `VITE_`) del entorno o de `.env.local`. Además:

- sólo la agrega a las rutas de conversaciones, transportistas y asociaciones, y al GET de las colecciones `/api/v1/windows`, `/api/v1/trucks` y `/api/v1/farms`; descarta cualquier encabezado `X-AgroFlow-*` que envíe el navegador;
- exige cliente de loopback, `Host` local y mismo origen (contra DNS
  rebinding y pedidos de otros sitios); en caso contrario responde `404`;
- bloquea las rutas de n8n (`/api/v1/conversation-events`,
  `/api/v1/conversation-reply-intents`).

Limitaciones: cualquier proceso local que pueda hablar con el servidor de Vite
actúa como el dashboard; no hay usuarios ni roles. Es una medida de demo en
loopback que B03 reemplaza por JWT con ingenio. Servir `dist/` sin este proxy
deja esas vistas sin acceso (`401`), nunca sin autenticación.

Las [franjas del panel](../docs/development/dashboard-windows-demo.md) muestran
capacidad, ocupados y disponibles sin recalcularlos. Distinguen error inicial,
fecha vacía y última lectura desactualizada; el filtro no limita Camiones en curso.

## Organización actual

El código existente está organizado en `domain/`, `application/`,
`infrastructure/`, `composition/`, `presentation/` y `shared/`. En HTTP, el
`HttpTurnoRepository`, `HttpTransportistaRepository` y
`HttpConversacionRepository` y `HttpVentanaRepository` con sus lecturas/DTOs/mappers
consumen appointments, drivers, conversations y windows. Esta estructura
vigente se describe aquí como inventario, no como una regla para rehacerla ni
como autorización para sumar dependencias.

La frontera de dependencias actual es:

```text
presentation → application → domain ← infrastructure
```

`composition/container.ts` conecta las implementaciones concretas. Ninguna capa
frontend debe decidir prioridad, capacidad, autorizaciones o transiciones. Cuando
el contrato incorpore otra capacidad, se agrega su repositorio HTTP y se conecta
la vista que hoy se muestra vacía.

El registro de la integración de la demo está en
[`frontend/docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md); lo
que describe sobre el modo mock quedó sin efecto.
