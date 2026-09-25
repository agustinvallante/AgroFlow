# Decisiones de la fundación del frontend

Esta matriz permite distinguir rápidamente qué puede guiar la implementación, qué es una recomendación y qué continúa bloqueado. Los ADR detallan contexto, alternativas y consecuencias; esta página no los reemplaza.

## Estados usados

- **Confirmada:** decisión aprobada para la arquitectura objetivo, aunque todavía no haya código.
- **Confirmada con dependencia:** el modelo o principio está aprobado, pero su implementación depende de decisiones operativas o contratos pendientes.
- **Propuesta:** recomendación no aprobada; no debe implementarse como si fuera definitiva.
- **Bloqueante pendiente:** falta una decisión necesaria para implementar o verificar un flujo.

## Matriz

| ADR | Tema | Estado | Decisión o límite | Dependencias pendientes |
|---|---|---|---|---|
| [ADR-FE-001](../decisions/ADR-FE-001-nextjs.md) | Framework | Confirmada | Next.js con TypeScript como base objetivo. | Versiones y despliegue se fijarán al implementar. |
| [ADR-FE-002](../decisions/ADR-FE-002-app-router.md) | Enrutamiento y render | Confirmada | App Router; Server Components por defecto y Client Components en límites interactivos. | Contrato de sesión y hosting con runtime servidor. |
| [ADR-FE-003](../decisions/ADR-FE-003-feature-first.md) | Organización | Confirmada | Agrupar por capacidad y reservar `shared` para infraestructura transversal. | Primeras capacidades y convenciones de importación al iniciar código. |
| [ADR-FE-004](../decisions/ADR-FE-004-tanstack-query.md) | Estado remoto | Confirmada | TanStack Query para consultas, mutaciones e invalidación. | Contratos HTTP e intervalos específicos. |
| [ADR-FE-005](../decisions/ADR-FE-005-zustand-ui-state.md) | Estado local | Confirmada | Zustand solo para estado efímero de UI. | Persistencia local excepcional deberá justificarse aparte. |
| [ADR-FE-006](../decisions/ADR-FE-006-auth-session.md) | Sesión web | **Confirmada con dependencia** | Modelo aceptado: el navegador conserva solo un identificador opaco en cookie `HttpOnly`; el BFF almacena o referencia la autenticación del backend del lado servidor. No existe implementación todavía. | Forma de endpoints y DTO, TTL, custodia del JWT, renovación, revocación, cierre, CSRF, atributos y dominios de cookie, almacenamiento del lado servidor, múltiples instancias, despliegue y comportamiento ante fallas. |
| [ADR-FE-007](../decisions/ADR-FE-007-roles-autorizacion.md) | Roles | Confirmada con dependencia | El backend autoriza; la UI solo adapta la experiencia. | OD-007: matriz de roles y permisos. |
| [ADR-FE-008](../decisions/ADR-FE-008-openapi-contract-first.md) | Contrato | Confirmada | Integración contract-first y tipos derivados de OpenAPI. | Rutas y esquemas; herramienta y política de generación. |
| [ADR-FE-009](../decisions/ADR-FE-009-error-handling.md) | Errores | Confirmada con dependencia | Errores tipados y accionables, sin asumir éxito ni exponer datos sensibles. | Esquema de error en OpenAPI y política de correlación. |
| [ADR-FE-010](../decisions/ADR-FE-010-dashboard-refresh.md) | Refresco | Confirmada con dependencia | Polling permitido para el MVP; WebSockets no son obligatorios. | OD-012: intervalo, indicadores y fecha operativa. |
| [ADR-FE-011](../decisions/ADR-FE-011-testing.md) | Pruebas | Confirmada | Capas de pruebas unitarias, de componente, contrato, integración y E2E. | Herramientas y comandos al crear la base ejecutable. |
| [ADR-FE-012](../decisions/ADR-FE-012-design-system.md) | Sistema visual | **Propuesta** | Tokens semánticos y primitivas accesibles antes de componentes de negocio. | Librería, estrategia de estilos, marca y catálogo visual. |

## Decisiones bloqueantes transversales

| Dependencia | Efecto sobre el frontend |
|---|---|
| OD-007, matriz de roles y permisos | Aprobada por el usuario como parte de la línea base abierta `mvp-baseline`; su aplicación normativa permanece pendiente hasta archivar ese cambio. No altera que el backend siempre autoriza. |
| OD-011, operación y calidad de servicio | Bloquea proveedor, retención y objetivos de observabilidad productiva. |
| OD-012, indicadores y refresco | Bloquea fórmulas, fecha operativa e intervalo concreto del dashboard. |
| Contratos OpenAPI parciales | Existen rutas de datos maestros (`drivers`, `trucks`, `farms` y `driver-truck-associations`) y respuestas de recibo de mutación. Faltan rutas de autenticación/sesión/contexto y de listado/detalle de turnos, que siguen bloqueando esos adaptadores y pruebas contractuales. |
| Contrato y operación de sesión web | El modelo de identificador opaco en cookie `HttpOnly` y sesión del lado servidor en el BFF está aceptado. La integración sigue bloqueada hasta congelar endpoints, TTL, custodia del JWT, renovación, revocación, cierre, CSRF, cookies/dominios, almacenamiento del lado servidor, múltiples instancias, despliegue y comportamiento ante fallas. El BFF solo media sesión; el backend conserva roles, acceso al ingenio, autorización y reglas de turnos. |
| Decisiones de negocio en [`open-decisions.md`](../../planning/open-decisions.md) | Bloquean únicamente las capacidades indicadas en ese registro y no deben completarse desde la UI. |

## Reglas para cambiar una decisión

1. Actualizar o reemplazar el ADR correspondiente con estado y fundamento explícitos.
2. Resolver la entrada relacionada en `docs/planning/open-decisions.md` cuando exista.
3. Actualizar OpenSpec si cambia comportamiento observable.
4. Actualizar OpenAPI si cambia la interacción HTTP.
5. Recién entonces adaptar implementación y pruebas.

La arquitectura completa está en [frontend-architecture.md](frontend-architecture.md). La preparación documental de la primera porción autenticada de consulta se revisa en [`openspec/changes/frontend-auth-readonly-slice/`](../../../openspec/changes/frontend-auth-readonly-slice/). `frontend/` todavía contiene solo su README: no hay aplicación ejecutable. OD-001..OD-013 fueron aprobados por el usuario en `mvp-baseline`, que continúa abierto y no normativo hasta su archivo.
