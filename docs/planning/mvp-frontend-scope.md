# Alcance real del frontend del MVP

> **Estado: planificación propuesta.** Este documento deriva el alcance de las especificaciones y decisiones vigentes; no crea comportamiento normativo, no aprueba los defaults de [`mvp-defaults.md`](mvp-defaults.md) y no afirma que el frontend esté implementado.

## Conclusión ejecutiva

El frontend del MVP será una aplicación web interna para operar y observar AgroFlow. Los **transportistas no usan esta aplicación**: solicitan, consultan e informan `EN_CAMINO` por **WhatsApp mediante n8n**, según [`integracion-whatsapp-n8n`](../../openspec/specs/integracion-whatsapp-n8n/spec.md). El backend conserva identidad efectiva, autorización, aislamiento por ingenio, reglas y estado persistido, como establece [ADR-003](../architecture/decisions/ADR-003-backend-fuente-de-verdad.md).

**El frontend todavía no tiene una aplicación ejecutable:** `frontend/` contiene solo su README. El contrato OpenAPI ya publica rutas de datos maestros para `drivers`, `trucks`, `farms` y `driver-truck-associations`, incluidas respuestas de recibo de mutación. No publica todavía autenticación/sesión/contexto ni listado o detalle de turnos, así que esa porción vertical no puede conectarse a contratos. El estado de OpenAPI es parcial, no vacío.

La aplicación de migraciones SQL Server permanece diferida por decisión explícita del usuario. El trabajo de interfaz no debe llenar las brechas de backend o contrato con mocks presentados como datos reales; no se usarán mocks para completar flujos operativos.

## Fuentes y límites de autoridad

Este alcance se deriva de:

- comportamiento normativo en [`openspec/specs/`](../../openspec/specs/);
- casos de uso en el [catálogo del MVP](../product/use-case-catalog.md);
- umbral y guion en [`aceptacion-del-mvp`](../../openspec/specs/aceptacion-del-mvp/spec.md) y la [guía de aceptación](../product/mvp-acceptance.md);
- arquitectura objetivo en [`frontend-architecture.md`](../architecture/frontend/frontend-architecture.md);
- secuencia de entrega en la [hoja de ruta](implementation-roadmap.md);
- decisiones pendientes en [`open-decisions.md`](open-decisions.md).

La forma HTTP solo puede provenir de OpenAPI aprobado. Las rutas de UI que se proponen abajo organizan la experiencia web; **no implican endpoints**, DTO ni permisos todavía inexistentes.

## Usuarios, roles y límite de tenant

### Actores del frontend

| Actor | Uso previsto de la web | Estado de definición |
|---|---|---|
| Operador | Operación diaria de turnos, interrupciones y dashboard | Rol previsto; permisos exactos pendientes de OD-007 |
| Supervisor | Supervisión operativa y, si se aprueba, gestión de datos maestros/auditoría | Rol previsto; permisos exactos pendientes de OD-007 |
| Gerente | Consulta de operación e indicadores | Rol previsto; permisos exactos pendientes de OD-007 |
| Administrador | Accesos y gestión autorizada del ambiente | Rol previsto; permisos exactos pendientes de OD-007 |
| Transportista | **No usa el frontend web del MVP**; opera por WhatsApp/n8n | Definido por las specs del canal |

OD-007 fue aprobado por el usuario como parte de la línea base `mvp-baseline`, que continúa abierta y no es normativa hasta su archivo. Hasta que se archive, la matriz aprobada no debe presentarse como comportamiento normativo. Ocultar un botón nunca reemplaza la autorización backend; ese límite está aceptado en [ADR-FE-007](../architecture/decisions/ADR-FE-007-roles-autorizacion.md).

### Aislamiento por ingenio

El ingenio efectivo se obtiene de la identidad confirmada por el backend. No existe selector libre de tenant, parámetro de URL confiable ni estado Zustand que pueda cambiarlo. Cada consulta y mutación vuelve a validar pertenencia en el backend según [`plataforma-y-segregacion`](../../openspec/specs/plataforma-y-segregacion/spec.md).

La sesión web seguirá el modelo aceptado en [ADR-FE-006](../architecture/decisions/ADR-FE-006-auth-session.md): identificador opaco en cookie `HttpOnly` y sesión del lado servidor en el BFF de Next.js. Su ciclo de vida, contrato, almacenamiento, CSRF, despliegue y fallas siguen pendientes; el BFF media sesión, no autoriza ni implementa reglas de negocio.

## Rutas y recorridos del MVP

Los nombres son una propuesta de navegación; podrán ajustarse sin cambiar la capacidad propietaria. Cada ruta funcional permanece bloqueada hasta tener comportamiento aprobado, backend real y OpenAPI publicado.

| Ruta web propuesta | Recorrido | Capacidad propietaria | Usuario principal |
|---|---|---|---|
| `/login` | Iniciar sesión interna y resolver contexto | [`acceso-y-autorizacion`](../../openspec/specs/acceso-y-autorizacion/spec.md) | Todos los roles internos |
| `/` | Redirigir al inicio permitido por contexto/rol | Acceso + OD-007 | Todos los roles internos |
| `/turnos` | Listar turnos del día, ordenar por horario y filtrar por fecha, estado y patente | [`turnos-consulta`](../../openspec/specs/turnos-consulta/spec.md) | Roles internos autorizados |
| `/turnos/[turnoId]` | Ver detalle y estado vigente; ejecutar acciones permitidas | [`turnos-consulta`](../../openspec/specs/turnos-consulta/spec.md), [`turnos-ciclo-de-vida`](../../openspec/specs/turnos-ciclo-de-vida/spec.md) | Operador/supervisor/administrador según OD-007 |
| `/operacion` | Ver indicadores diarios e interrupción activa | [`monitoreo-operativo`](../../openspec/specs/monitoreo-operativo/spec.md) | Operador, supervisor, gerente y administrador según OD-007 |
| `/interrupciones` | Consultar, registrar o actualizar la interrupción del ingenio | [`interrupciones-operativas`](../../openspec/specs/interrupciones-operativas/spec.md) | Roles autorizados según OD-007 |
| `/datos-maestros/transportistas` | Listar y gestionar transportistas | [`datos-maestros`](../../openspec/specs/datos-maestros/spec.md) | Roles de gestión según OD-007/OD-013 |
| `/datos-maestros/camiones` | Listar y gestionar camiones | [`datos-maestros`](../../openspec/specs/datos-maestros/spec.md) | Roles de gestión según OD-007/OD-013 |
| `/datos-maestros/fincas` | Listar y gestionar fincas | [`datos-maestros`](../../openspec/specs/datos-maestros/spec.md) | Roles de gestión según OD-007/OD-013 |
| `/datos-maestros/asociaciones` | Gestionar autorizaciones transportista-camión | [`datos-maestros`](../../openspec/specs/datos-maestros/spec.md), OD-002 | Roles de gestión según OD-007/OD-013 |
| `/auditoria` | Consultar historial de mutaciones críticas | [`aceptacion-del-mvp`](../../openspec/specs/aceptacion-del-mvp/spec.md), OD-009 | Roles autorizados según OD-007 |

### Recorridos funcionales

#### 1. Autenticación y contexto

1. La persona presenta credenciales en `/login`.
2. El BFF solicita autenticación al backend mediante el contrato aprobado.
3. El navegador recibe solo el identificador opaco de sesión.
4. La aplicación obtiene del backend usuario, rol, permisos efectivos e ingenio.
5. Una sesión inválida o expirada nunca deja visible estado operativo como si siguiera vigente.

La preparación documental ya existe en [`frontend-auth-readonly-slice`](../../openspec/changes/frontend-auth-readonly-slice/proposal.md), pero no habilita código ni contrato.

#### 2. Lista y detalle de turnos

1. `/turnos` solicita por defecto la fecha operativa actual y presenta filas ordenadas por horario.
2. La persona filtra por fecha, estado o patente.
3. Selecciona un turno y abre `/turnos/[turnoId]`.
4. El detalle vuelve a consultar el estado vigente y muestra contexto, ventana, estado, camión, transportista, finca y trazabilidad que el contrato apruebe.
5. Lista y detalle son inmutables: consultar no modifica el turno.

#### 3. Operación del turno

Desde el detalle, un actor autorizado puede solicitar la siguiente transición permitida o cancelar desde un estado cancelable. La UI muestra estado pendiente hasta recibir confirmación, luego reemplaza su vista con la respuesta persistida. No adelanta transiciones ni libera capacidad localmente. El transportista informa `EN_CAMINO` por WhatsApp/n8n; las restantes transiciones del MVP pertenecen al operador conforme a [`turnos-ciclo-de-vida`](../../openspec/specs/turnos-ciclo-de-vida/spec.md).

#### 4. Dashboard operativo

`/operacion` presenta únicamente indicadores calculados por el backend para el ingenio y fecha operativa actuales, el instante de actualización y la interrupción activa. Podrá usar polling una vez aprobado OD-012; no calcula indicadores desde la lista local y no requiere WebSockets, según [ADR-FE-010](../architecture/decisions/ADR-FE-010-dashboard-refresh.md).

#### 5. Interrupción

Un actor autorizado consulta la interrupción actual, registra o actualiza una y recibe el resultado procesado por backend: turnos `ASIGNADO` reprogramados, turnos `EN_CAMINO`/`EN_ESPERA` identificados para notificación y estados internos no modificados. El frontend no selecciona destinatarios ni recalcula ventanas.

#### 6. Datos maestros

Las pantallas administran solo las operaciones y campos que apruebe OD-013. Inhabilitar conserva historial; una entidad inactiva no puede participar de nuevos turnos. Las asociaciones transportista-camión dependen de OD-002. No se deduce un CRUD del prototipo.

#### 7. Integración visible, no operada por la web

La demo debe mostrar que solicitud, consulta y aviso `EN_CAMINO` ingresan por WhatsApp/n8n, llegan a la API real y luego aparecen con el mismo estado persistido en lista, detalle y dashboard. La web puede mostrar estado de notificación cuando exista contrato, pero no reemplaza ni simula el canal.

## Datos requeridos por el frontend

Los campos exactos deben congelarse en OpenAPI. Este inventario expresa necesidades de producto, no DTO propuestos.

| Área | Datos necesarios |
|---|---|
| Sesión/contexto | identidad visible mínima, rol, permisos efectivos, ingenio efectivo, vigencia de sesión |
| Lista de turnos | identificador, ventana, fecha operativa, estado, patente, transportista/finca resumidos y campos de orden/filtro aprobados |
| Detalle de turno | estado vigente, ventana, prioridad explicada, referencias de camión/transportista/finca, historial operativo permitido, versión/concurrencia si el contrato la requiere |
| Acciones de turno | acciones permitidas informadas por contrato, clave idempotente, correlación y resultado persistido |
| Dashboard | fecha operativa, zona horaria, instante de cálculo, indicadores backend e interrupción activa |
| Interrupciones | período, estado, motivo/descripción aprobados, turnos afectados, reprogramaciones y destinatarios determinados por backend |
| Datos maestros | campos aprobados de transportistas, camiones, fincas y asociaciones, actividad, versión e historial visible |
| Auditoría | actor representable, instante, acción, entidad, resultado, motivo/correlación permitidos y paginación |
| Notificaciones | destinatario enmascarado cuando corresponda, tipo, estado de entrega, intentos e instante; nunca secretos del proveedor |

Los datos remotos viven en TanStack Query; Zustand queda limitado a estado efímero de UI, según la [arquitectura canónica](../architecture/frontend/frontend-architecture.md). JWT, sesión, permisos, ingenio, turnos e indicadores no se almacenan como autoridad en el navegador.

## Qué puede mostrar el frontend

Cuando exista soporte real, la web puede mostrar:

- identidad y contexto de ingenio confirmados por backend;
- navegación y acciones adaptadas a permisos efectivos, sin sustituir autorización;
- lista, filtros y detalle del estado vigente de turnos;
- carga, vacío, error, conflicto y sesión expirada de forma accesible;
- acciones operativas solo mientras el backend las admita;
- indicadores e interrupción calculados por backend;
- datos maestros e historial dentro del alcance aprobado;
- resultado y correlación permitida de una operación;
- frescura de datos y estados de entrega de notificaciones cuando estén contratados.

Una respuesta vieja puede quedar identificada como desactualizada, pero nunca imponerse sobre una respuesta backend más reciente.

## Qué no puede ser funcional todavía

Mientras no existan la aplicación frontend ejecutable y contratos OpenAPI aprobados para cada capacidad, no pueden considerarse funcionales:

- login y sesión BFF conectados;
- contexto real de usuario, rol e ingenio;
- listado, filtros o detalle de turnos;
- transiciones, cancelación o liberación de capacidad;
- dashboard e interrupciones;
- gestión de datos maestros y asociaciones conectada a la UI, aunque sus contratos de API ya están disponibles;
- auditoría o estados de notificación;
- tipos generados, adaptadores, pruebas de contrato o E2E reales.

Un layout, tabla o formulario estático puede servir para trabajo visual aislado, pero debe rotularse como prototipo. **No se usarán mocks, fixtures, arreglos locales, respuestas interceptadas ni estado Zustand para completar o presentar flujos como estado operacional.** Esta regla deriva de [`aceptacion-del-mvp`](../../openspec/specs/aceptacion-del-mvp/spec.md): los recorridos obligatorios atraviesan el punto de entrada, API real, backend y persistencia.

## No objetivos del frontend MVP

- portal web o aplicación móvil para transportistas;
- reemplazar WhatsApp/n8n con formularios internos;
- geolocalización en tiempo real, mapas, rutas o PostGIS;
- estimación de posición, reloj simulado presentado como dato real o seguimiento GPS;
- WebSockets obligatorios;
- planificación predictiva, analítica avanzada o acreditación del 30 % de reducción;
- editor de reglas de prioridad, capacidad o transiciones;
- selector libre de ingenio o acceso multiingenio no aprobado;
- autorización, indicadores o reglas de negocio calculados en frontend/BFF;
- importación del código, contratos, datos o comportamiento del prototipo;
- funcionamiento offline, aplicación nativa, ERP o microservicios;
- panel de configuración de n8n, proveedor de WhatsApp o secretos.

El prototipo en [`docs/contexto/AgroFlow-Dashboard/README.md`](../contexto/AgroFlow-Dashboard/README.md) es **referencia visual únicamente**. No es una base de código, fuente normativa ni evidencia de funcionalidad.

## Primera porción vertical real recomendada

La primera porción debe ser **autenticación/contexto + lista de turnos + detalle de turno**:

1. autenticación interna y sesión BFF con identificador opaco;
2. usuario, rol, permisos e ingenio efectivos confirmados por backend;
3. listado del ingenio para la fecha operativa actual, con filtros de fecha, estado y patente;
4. detalle vigente, aislado e inmutable;
5. estados de carga, vacío, error, expiración y acceso cruzado rechazado.

Es la menor porción que prueba de extremo a extremo los límites más importantes: contrato, sesión, backend autoritativo, aislamiento por tenant y datos persistidos visibles. Además, no depende todavía de cerrar asignación, capacidad o mutaciones del ciclo. Su preparación actual está descrita en [`openspec/changes/frontend-auth-readonly-slice/design.md`](../../openspec/changes/frontend-auth-readonly-slice/design.md), cuya lista de congelamiento debe completarse antes de implementar.

Esta recomendación no convierte la porción de lectura en el MVP completo: las demás capacidades siguen siendo obligatorias para la aceptación final.

## Secuencia por fases: del contrato backend a Next.js

### Fase 0 — Aprobar decisiones y comportamiento

- revisar [`mvp-defaults.md`](mvp-defaults.md) sin tratarlo como normativa;
- cerrar las OD que bloquean cada porción;
- incorporar comportamiento aprobado en las capacidades OpenSpec existentes;
- no crear una especificación paralela “del frontend”.

**Puerta:** decisión aprobada y escenarios verificables por capacidad.

### Fase 1 — Reemplazar la base backend heredada

- establecer identidad, roles, ingenio y segregación AgroFlow;
- implementar persistencia, errores, auditoría y datos reproducibles;
- implementar primero las consultas necesarias para la porción vertical;
- probar aislamiento antes de exponer consumidores.

**Puerta:** backend AgroFlow probado; productos, clientes y órdenes heredados no conducen el comportamiento del MVP.

### Fase 2 — Publicar OpenAPI real

- documentar seguridad, operaciones, parámetros, esquemas y errores;
- validar que contrato y specs expresen el mismo comportamiento;
- incluir casos negativos, concurrencia e idempotencia cuando correspondan.

**Estado actual:** OpenAPI ya contiene rutas de datos maestros y recibos de mutación. La porción de autenticación/contexto y turnos requiere rutas aprobadas y verificadas; esas rutas todavía no existen.

### Fase 3 — Crear la fundación ejecutable Next.js

- Next.js + TypeScript + App Router;
- organización feature-first;
- TanStack Query para remoto y Zustand solo para UI;
- runtime servidor y límites BFF conforme a ADR-FE-006;
- cliente/tipos derivados de OpenAPI de forma reproducible;
- primitivas accesibles y estados de carga/vacío/error.

**Puerta:** la base compila y sus adaptadores prueban el contrato sin importar el prototipo.

### Fase 4 — Entregar la primera porción vertical

- implementar autenticación/contexto;
- conectar `/turnos` y `/turnos/[turnoId]`;
- verificar sesión, expiración, permisos e aislamiento entre dos ingenios;
- demostrar datos persistidos reales y consultas inmutables.

**Puerta:** autenticación + lista + detalle funcionan contra backend real y OpenAPI aprobado.

### Fase 5 — Agregar operación y dashboard

- transiciones y cancelación desde detalle;
- dashboard e interrupción activa;
- polling aprobado y conflictos recuperables;
- no aplicar estado optimista a mutaciones críticas.

**Puerta:** el estado confirmado se refleja igual en detalle, lista y dashboard.

### Fase 6 — Agregar datos maestros e interrupciones

- gestión acotada por OD-013;
- asociaciones según OD-002;
- interrupciones y visualización de efectos/notificaciones;
- auditoría solo lectura.

**Puerta:** datos válidos pueden prepararse de forma reproducible y las mutaciones conservan historia.

### Fase 7 — Integrar WhatsApp/n8n y aceptar el MVP

- conectar proveedor de pruebas/adaptador, n8n y API real;
- mostrar en la web el mismo turno creado/consultado por transportista;
- completar recorrido, cancelación, interrupción y notificación;
- ejecutar dos veces el guion de aceptación sin editar la base manualmente.

**Puerta:** todas las capacidades obligatorias y evidencias de [`aceptacion-del-mvp`](../../openspec/specs/aceptacion-del-mvp/spec.md) cumplen.

## Evidencia de aceptación del frontend

| Evidencia | Resultado observable |
|---|---|
| Build y pruebas | Next.js compila; unitarias, componentes, contrato, integración y E2E definidos para las capacidades implementadas pasan. |
| Contrato | Tipos/adaptadores derivan del OpenAPI aprobado y una divergencia se detecta automáticamente. |
| Sesión | JWT no aparece en JavaScript ni almacenamiento del navegador; expiración, cierre y falla se manejan según contrato. |
| Tenant | Usuario del ingenio A no lista ni abre por identificador un turno del ingenio B; el backend rechaza sin filtración. |
| Consulta | Lista por defecto, filtros y detalle reflejan datos persistidos y no generan escrituras. |
| Mutaciones | La UI no muestra éxito antes de confirmación; conflicto/reintento no duplica ni sobrescribe estado vigente. |
| Dashboard | Indicadores e interrupción coinciden exactamente con el dataset conocido y la respuesta API. |
| Canal | Un turno solicitado/consultado por WhatsApp/n8n aparece con el mismo estado en la web; `EN_CAMINO` se refleja tras persistencia. |
| Accesibilidad | Navegación por teclado, foco, semántica, contraste y estados de carga/vacío/error están verificados. |
| Repetibilidad | Datos demo se preparan/restauran por procedimiento; el guion completo funciona dos veces y sobrevive reinicio. |
| Ausencia de simulación | Ningún recorrido obligatorio depende de mocks ni presenta datos ficticios como estado operativo. |

La evidencia final se registra contra una versión exacta y sigue las puertas de [`docs/product/mvp-acceptance.md`](../product/mvp-acceptance.md). Una captura visual aislada no prueba funcionalidad.
