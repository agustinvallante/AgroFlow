# Diseño: fundación del frontend

## Resumen

La fundación adopta Next.js con TypeScript y App Router como arquitectura objetivo, organizada por capacidad. TanStack Query administrará estado remoto y Zustand se limitará al estado efímero de interfaz. La integración comenzará desde OpenAPI y mantendrá al backend como autoridad operativa.

Este diseño describe una implementación futura. Es un cambio exclusivamente documental de OpenSpec (`skip_specs: true`): no crea código, contratos HTTP, capacidades funcionales ni una delta normativa. Los requisitos existentes permanecen en sus especificaciones por capacidad de negocio; cualquier cambio de comportamiento futuro deberá modificar allí su única definición normativa.

## Fuentes y restricciones

- OpenSpec conserva la autoridad sobre comportamiento mediante las capacidades de negocio vigentes; este cambio no agrega una especificación tecnológica paralela.
- OpenAPI conserva la autoridad sobre la forma HTTP.
- El backend decide identidad efectiva, permisos, ingenio, reglas y estado persistido.
- El prototipo bajo `docs/contexto/` es una referencia visual e histórica; no se importa.
- Las decisiones abiertas no se completan con supuestos de frontend.

## Decisiones de diseño

### Framework y render

Next.js y App Router serán la base. Los Server Components compondrán por defecto; los Client Components aparecerán en el límite mínimo que necesite interacción o APIs del navegador. Los Route Handlers no implementarán reglas de negocio. El modelo BFF está aceptado, pero su implementación y operación concreta siguen pendientes.

### Organización

El código específico se agrupará por capacidad bajo `features/`. `app/` compondrá rutas y `shared/` concentrará únicamente infraestructura y primitivas verdaderamente transversales. Esta estructura alinea cambios de UI con capacidades OpenSpec sin duplicar el dominio del backend.

### Datos y estado

TanStack Query administrará consultas, mutaciones e invalidaciones de respuestas remotas. Zustand administrará solo estado efímero de interfaz y no conservará tokens, permisos, ingenio efectivo o entidades operativas.

Una mutación permanecerá pendiente hasta recibir confirmación del backend. No se usarán datos locales o simulados para presentar una operación como persistida.

### Contrato HTTP

La integración seguirá OpenAPI primero: comportamiento aprobado, contrato publicado, tipos derivados, adaptador probado y recién luego conexión de la vista. OpenAPI ya publica rutas de datos maestros (`drivers`, `trucks`, `farms` y `driver-truck-associations`) y recibos de mutación, pero todavía no tiene rutas de autenticación/sesión/contexto ni de listado/detalle de turnos. Este cambio no define nombres, métodos, DTO ni errores para esos flujos ausentes.

### Sesión y autorización

OpenSpec exige JWT para usuarios internos. El modelo aceptado es un identificador opaco en cookie `HttpOnly` y una sesión del lado servidor en el BFF de Next.js. No hay implementación: TTL, CSRF, dominio y atributos de cookie, almacenamiento, renovación y despliegue siguen pendientes.

El backend siempre autorizará. La UI podrá adaptar la experiencia una vez aprobada OD-007, sin transformarse en barrera de seguridad. El ingenio efectivo provendrá de la identidad confirmada por el backend.

### Errores y refresco

La única semántica HTTP vigente reconocida por este diseño es `401 Unauthorized` para un token expirado en un recurso protegido, según la especificación de acceso y autorización. No se generaliza ese código a otros casos. Toda otra correspondencia entre estados HTTP, categorías de experiencia y recuperación esperará al contrato OpenAPI aprobado. Sin fijar esas semánticas pendientes, el frontend no transformará una operación rechazada o fallida en un éxito local ni expondrá información sensible.

El dashboard podrá usar polling porque la especificación vigente no exige WebSockets; OD-012 deberá fijar intervalo, indicadores y fecha operativa.

### Diseño visual, configuración y observabilidad

Se propone construir tokens semánticos y primitivas accesibles antes de componentes de negocio. La selección de librería y estilos continúa pendiente.

Solo configuración explícitamente pública podrá llegar al bundle del navegador. La observabilidad excluirá secretos y datos sensibles; proveedor, retención y objetivos productivos dependen de OD-011.

## Alternativas principales

| Tema | Alternativa no seleccionada o pendiente | Motivo |
|---|---|---|
| Base | Importar React/Vite del prototipo | Arrastra mocks, estructura y decisiones no normativas. |
| Organización | Carpetas globales por tipo técnico | Dispersa el cambio de una capacidad y su trazabilidad. |
| Estado remoto | Store global manual | Duplica caché y crea una fuente de verdad local. |
| Sesión | Token en navegador o sesión administrada directamente por el backend | No fueron seleccionados; el modelo BFF con identificador opaco está aceptado, aunque sus detalles operativos siguen pendientes. |
| Actualización | WebSockets obligatorios | No son requisito del MVP y agregan operación prematura. |
| Sistema visual | Librería completa inmediata | Fija una dependencia antes de acordar necesidades y accesibilidad. |

## Riesgos y mitigaciones

| Riesgo | Mitigación documental |
|---|---|
| Confundir el modelo BFF aceptado con una implementación disponible | ADR-FE-006 y la matriz distinguen la decisión aceptada de sus dependencias operativas y de la ausencia de código. |
| Duplicar reglas en la UI | Límites explícitos y referencia a ADR-003 y plataforma/segregación. |
| Integrar contra endpoints imaginados | ADR-FE-008 exige contrato OpenAPI previo. |
| Mezclar mocks con datos reales | Requisito observable de identificación y separación de simulaciones. |
| Filtrar datos entre ingenios o solicitudes SSR | Contexto confirmado por backend y stores por alcance, nunca singleton global de datos operativos. |
| Sobrediseñar antes del primer flujo | Organización mínima por capacidad y extracción compartida basada en uso real. |

## Migración futura

1. aprobar las decisiones bloqueantes de la primera capacidad;
2. publicar su delta OpenSpec y contrato OpenAPI;
3. crear la base Next.js sin importar el prototipo;
4. generar tipos y construir un adaptador probado;
5. implementar una porción vertical con estados de carga, vacío y error;
6. reemplazar el mock de esa porción sin mezclarlo con producción;
7. verificar permisos, rechazo, confirmación e aislamiento con backend real.

## Estrategia de pruebas futura

- unitarias para mapeos y estado de UI;
- componentes para interacción, accesibilidad y estados visibles;
- contrato para adaptadores OpenAPI;
- integración por capacidad;
- E2E con backend real para recorridos aprobados y aislamiento por ingenio.

Las herramientas y comandos no se fijan en este cambio documental.

## Trazabilidad

Este documento no tiene una delta spec propia: las referencias OpenSpec siguientes conservan la propiedad normativa del comportamiento relacionado.

- [Acceso y autorización](../../specs/acceso-y-autorizacion/spec.md)
- [Plataforma y segregación](../../specs/plataforma-y-segregacion/spec.md)
- [Arquitectura canónica](../../../docs/architecture/frontend/frontend-architecture.md)
- [Matriz de decisiones](../../../docs/architecture/frontend/frontend-decisions.md)
- [ADR-003: backend como fuente de verdad](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md)
- [Decisiones abiertas](../../../docs/planning/open-decisions.md)
