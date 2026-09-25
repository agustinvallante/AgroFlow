# Arquitectura canónica del frontend

Este documento define la arquitectura objetivo de la aplicación web interna de AgroFlow. Es una base documental: no afirma que el frontend esté implementado ni habilita pantallas, endpoints o reglas de negocio.

## Lectura rápida

1. El frontend se implementará con Next.js, TypeScript y App Router.
2. La organización será por capacidad (`feature-first`) y mantendrá límites técnicos compartidos.
3. TanStack Query administrará estado remoto; Zustand, solo estado efímero de interfaz.
4. El backend seguirá siendo la fuente operativa de verdad y aplicará autorización y aislamiento por ingenio.
5. La integración HTTP comenzará únicamente cuando el contrato OpenAPI correspondiente esté aprobado.
6. La sesión web seguirá el modelo aceptado de identificador opaco en cookie `HttpOnly` y sesión del lado servidor en el BFF de Next.js; sus detalles operativos, contrato e implementación siguen pendientes.

## Autoridad y alcance

La jerarquía documental de [ADR-002](../decisions/ADR-002-jerarquia-documental-y-openspec.md) se mantiene:

- [`openspec/specs/`](../../../openspec/specs/) define el comportamiento vigente;
- [`docs/contracts/openapi.yaml`](../../contracts/openapi.yaml) define la forma HTTP aprobada;
- los ADR registran decisiones técnicas;
- este documento guía la implementación sin crear comportamiento funcional.

El backend conserva las reglas, la persistencia, la identidad efectiva, los permisos y el aislamiento por ingenio según [ADR-003](../decisions/ADR-003-backend-fuente-de-verdad.md). El frontend reúne entradas, presenta respuestas y mejora la experiencia, pero no calcula prioridad, capacidad, transiciones, permisos ni indicadores operativos.

## Estado de las decisiones

| Categoría | Significado | Ejemplos en esta base |
|---|---|---|
| Confirmada | Puede orientar la futura implementación. | Next.js, App Router, organización por capacidad, TanStack Query para estado remoto, Zustand para estado de UI, contrato OpenAPI primero. |
| Confirmada con dependencia | El modelo está aprobado, pero no puede implementarse hasta congelar contratos o decisiones operativas asociados. | Identificador opaco en cookie `HttpOnly` y sesión del lado servidor en el BFF de Next.js. |
| Propuesta | Recomendación que requiere aprobación antes de implementarse. | Sistema visual inicial. |
| Bloqueante pendiente | Impide completar un flujo funcional o fijar una configuración. | Detalles operativos y contrato de sesión, forma de errores, intervalo del dashboard (OD-012), decisiones de negocio todavía no aprobadas. OD-007 fue aprobado por el usuario como parte de la línea base abierta; no es normativo hasta que se archive `mvp-baseline`. |

La matriz completa está en [decisiones del frontend](frontend-decisions.md).

## Responsabilidades y límites

| Área | Responsabilidad del frontend | Límite obligatorio |
|---|---|---|
| Presentación | Navegación, formularios, tablas, feedback y accesibilidad. | No convierte una acción visible en una autorización. |
| Datos remotos | Consultar, cachear, invalidar y volver a obtener respuestas de la API. | No reemplaza el estado confirmado por datos locales como autoridad. |
| Validación | Detectar formatos inválidos para dar feedback temprano. | El backend vuelve a validar y decide toda regla de negocio. |
| Sesión | Representar el estado de acceso mediante un identificador opaco en cookie `HttpOnly`; el BFF mantendrá o referenciará del lado servidor la autenticación del backend cuando exista el contrato aprobado. | Mediar la sesión únicamente: no exponer credenciales ni decidir roles, acceso al ingenio, autorización o reglas de turnos. |
| Integración | Traducir el contrato OpenAPI a tipos y adaptadores de cliente. | No inventar rutas, DTO, errores ni equivalencias de estados. |
| Observabilidad | Propagar correlación permitida y registrar fallas técnicas sin datos sensibles. | No registrar tokens, credenciales ni datos personales innecesarios. |

## Estructura objetivo por capacidad

La organización será `feature-first`: el código específico se agrupará por capacidad, y solo los elementos realmente transversales vivirán en áreas compartidas.

```text
frontend/
└── src/
    ├── app/                    # rutas, layouts y límites de render de App Router
    ├── features/
    │   └── <capacidad>/
    │       ├── components/     # presentación propia de la capacidad
    │       ├── queries/        # consultas y mutaciones remotas
    │       ├── schemas/        # validación estructural en el límite
    │       ├── types/          # tipos de vista derivados del contrato
    │       └── tests/          # pruebas enfocadas de la capacidad
    └── shared/
        ├── api/                # cliente y tipos generados desde OpenAPI
        ├── components/         # primitivas visuales reutilizables
        ├── config/             # configuración pública validada
        ├── observability/      # telemetría técnica sin datos sensibles
        └── state/              # estado efímero transversal estrictamente necesario
```

Esta estructura es objetivo, no evidencia de directorios existentes. Una capacidad no debe duplicar reglas que ya pertenecen al backend. Consultar [ADR-FE-003](../decisions/ADR-FE-003-feature-first.md).

## App Router y límites de render

App Router organizará rutas y layouts. La regla por defecto será usar Server Components para composición y contenido que no requiera interactividad del navegador, y agregar Client Components en el límite mínimo que necesite eventos, APIs del navegador o hooks de cliente.

- `"use client"` no se propagará por comodidad a una ruta completa.
- Los datos sensibles no se serializarán hacia el navegador sin necesidad.
- Un componente del servidor no llamará a un Route Handler propio para alcanzar la API si puede usar directamente el adaptador permitido.
- El modelo de sesión BFF está aceptado con dependencias operativas, pero ningún Route Handler se considera implementado ni puede inventar contratos de sesión; su uso depende de las definiciones pendientes de [ADR-FE-006](../decisions/ADR-FE-006-auth-session.md).
- Ningún tipo de componente implementará reglas operativas del backend.

Ver [ADR-FE-001](../decisions/ADR-FE-001-nextjs.md) y [ADR-FE-002](../decisions/ADR-FE-002-app-router.md).

## Estado remoto y estado de interfaz

### TanStack Query

TanStack Query será el mecanismo de estado remoto en componentes cliente. Sus claves deberán incluir el contexto funcional que afecte la respuesta, sin usar un ingenio arbitrario como sustituto de la identidad autenticada.

- Una mutación solo se mostrará como confirmada después de una respuesta exitosa del backend.
- La invalidación o actualización de caché seguirá el contrato de la capacidad.
- No se aplicarán actualizaciones optimistas a estados operativos críticos salvo una decisión posterior con reconciliación explícita.
- El polling del dashboard será configurable cuando se resuelva OD-012; no se fija un intervalo en esta base.

Ver [ADR-FE-004](../decisions/ADR-FE-004-tanstack-query.md) y [ADR-FE-010](../decisions/ADR-FE-010-dashboard-refresh.md).

### Zustand

Zustand se reservará para estado efímero de interfaz que no provenga de la API, por ejemplo apertura de un panel o una preferencia visual transitoria.

No almacenará tokens, permisos, pertenencia al ingenio, turnos, indicadores ni otras entidades operativas. Las stores no serán singletons compartidos entre solicitudes del servidor; se crearán con el alcance necesario. Ver [ADR-FE-005](../decisions/ADR-FE-005-zustand-ui-state.md).

## Integración con la API

[`docs/contracts/openapi.yaml`](../../contracts/openapi.yaml) publica actualmente rutas de datos maestros para `drivers`, `trucks`, `farms` y `driver-truck-associations`, incluidas respuestas de recibo de mutación. Todavía no publica rutas de autenticación, sesión, contexto de usuario ni listado o detalle de turnos. Esta base no define endpoints para esas capacidades ni adapta el prototipo a rutas hipotéticas.

La integración seguirá este orden:

1. aprobar el comportamiento en OpenSpec y resolver decisiones bloqueantes;
2. publicar rutas, esquemas, seguridad y errores en OpenAPI;
3. generar tipos de cliente de forma reproducible;
4. implementar un adaptador por capacidad;
5. probar mapeos, errores y aislamiento contra el contrato;
6. conectar la interfaz sin completar datos faltantes con mocks.

Los tipos generados serán derivados del contrato, no una fuente normativa paralela. La herramienta y política de versionado de artefactos generados se elegirán al implementar el primer contrato. Ver [ADR-FE-008](../decisions/ADR-FE-008-openapi-contract-first.md).

## Sesión, roles y aislamiento por ingenio

OpenSpec exige JWT para usuarios internos, pero no define todavía cómo la capa web debe obtener, renovar, revocar ni traducir esa credencial a una sesión de navegador. El modelo de cookie con identificador opaco y sesión servidor en el BFF de Next.js está aceptado; su ciclo operativo y contrato siguen pendientes.

### Dirección de sesión aprobada

| Alternativa | Estado | Consideración principal |
|---|---|---|
| Identificador opaco en cookie `HttpOnly` + sesión del lado servidor en el BFF de Next.js | **Confirmada con dependencia** | El modelo está aceptado: el navegador conserva solo el identificador opaco y el BFF almacena o referencia la autenticación del backend del lado servidor; la operación concreta sigue pendiente. |
| Token administrado por el navegador | No seleccionada | Amplía la superficie de exposición y exige una política segura de almacenamiento en el cliente. |
| Sesión administrada directamente por el backend | No seleccionada | Requeriría que el backend adopte el contrato de sesión del navegador. |

La aceptación de este modelo no afirma que exista implementación ni elige un proveedor de almacenamiento. Antes de escribir código deben congelarse la forma de los endpoints y DTO, el TTL de la sesión, la custodia servidor del JWT, la renovación, la revocación, el cierre, los controles CSRF, los atributos y dominios de cookies, el almacenamiento del lado servidor, el comportamiento con múltiples instancias, el despliegue y el comportamiento ante fallas. Ver [ADR-FE-006](../decisions/ADR-FE-006-auth-session.md) y el [cambio preparatorio de autenticación y consultas](../../../openspec/changes/frontend-auth-readonly-slice/proposal.md).

El BFF se limita a mediar la sesión. La navegación puede ocultar o deshabilitar acciones para mejorar la experiencia, pero el backend siempre decide roles, acceso al ingenio, autorización y reglas de turnos. OD-007 está aprobado por el usuario en la línea base abierta, aunque todavía no es normativo hasta el archivo de `mvp-baseline`. El contexto de ingenio deriva de la identidad confirmada por el backend; no de un parámetro libre, una variable pública ni un filtro del navegador. Ver [ADR-FE-007](../decisions/ADR-FE-007-roles-autorizacion.md) y la especificación de [plataforma y segregación](../../../openspec/specs/plataforma-y-segregacion/spec.md).

## Manejo de errores

La única semántica HTTP vigente que esta base reconoce es la definida por la especificación de acceso y autorización: un token expirado en un recurso protegido recibe `401 Unauthorized`. Esta regla no amplía el significado de `401` a otros casos.

Los códigos, campos, textos y demás semánticas de error se definirán cuando OpenAPI publique el contrato correspondiente. Hasta entonces, el frontend no asociará estados HTTP con validación, permisos, conflictos, cierre de sesión u otras respuestas de producto. En todos los casos, una operación rechazada o fallida no se mostrará como exitosa. La interfaz tampoco expondrá trazas, secretos ni detalles internos. Ver [ADR-FE-009](../decisions/ADR-FE-009-error-handling.md) y la especificación de [acceso y autorización](../../../openspec/specs/acceso-y-autorizacion/spec.md).

## Diseño visual y accesibilidad

La base adoptará tokens semánticos y primitivas accesibles antes de crear componentes específicos de negocio. El prototipo bajo `docs/contexto/` es referencia visual no normativa: no se importa ni aporta reglas, datos o contratos.

La librería de componentes, la estrategia de estilos y el catálogo visual son propuestas pendientes. Cualquier elección deberá conservar navegación por teclado, foco visible, semántica, contraste y estados de carga, vacío y error. Ver [ADR-FE-012](../decisions/ADR-FE-012-design-system.md).

## Configuración y observabilidad

La configuración se separará por sensibilidad:

- solo valores explícitamente públicos podrán incluirse en el bundle del navegador;
- secretos y credenciales permanecerán en el servidor o en el backend;
- la configuración requerida se validará al iniciar o compilar según corresponda;
- no se agregan nombres de variables ni proveedores en esta base porque dependen del despliegue aprobado.

La observabilidad deberá registrar fallas técnicas, duración y correlación cuando el contrato lo permita. Se excluirán tokens, contraseñas, cookies, datos personales y cuerpos sensibles. La selección de proveedor, retención y objetivos operativos depende de OD-011.

## Estrategia de pruebas

La pirámide objetivo incluye:

1. pruebas unitarias para mapeos, validación estructural y estado de UI;
2. pruebas de componentes para carga, vacío, error, permisos visibles y accesibilidad;
3. pruebas de contrato para adaptadores generados o implementados desde OpenAPI;
4. pruebas integradas por capacidad con backend real o doble contractual controlado;
5. recorridos end-to-end para los flujos aprobados del MVP y el aislamiento entre ingenios.

Las herramientas concretas y los comandos se fijarán al crear el proyecto de pruebas. Esta decisión no afirma que hoy exista una suite. Ver [ADR-FE-011](../decisions/ADR-FE-011-testing.md).

## Reemplazo controlado de mocks

Los mocks solo podrán usarse en desarrollo aislado, pruebas o demostraciones claramente rotuladas. Nunca se presentarán como estado real ni se mezclarán silenciosamente con respuestas de la API.

Para reemplazar un mock:

1. identificar la capacidad y el escenario OpenSpec;
2. aprobar su contrato HTTP;
3. implementar y probar el adaptador;
4. conectar la consulta o mutación;
5. retirar el mock de la composición productiva;
6. verificar que errores y respuestas vacías no se completen con datos ficticios.

## Decisiones que bloquean implementación funcional

- La implementación normativa de OD-007 queda pendiente del archivo de `mvp-baseline`; la decisión ya fue aprobada por el usuario dentro de esa línea base abierta.
- OD-011: operación, calidad de servicio y observabilidad productiva.
- OD-012: indicadores, fecha operativa e intervalo de refresco.
- Los detalles operativos y el contrato del modelo de sesión aceptado en [ADR-FE-006](../decisions/ADR-FE-006-auth-session.md): endpoints, TTL, custodia del JWT, renovación, revocación, cierre, CSRF, cookies/dominios, almacenamiento del lado servidor, múltiples instancias, despliegue y comportamiento ante fallas.
- Los contratos de endpoints, DTO, seguridad y errores aún ausentes en OpenAPI.
- Las demás decisiones de negocio enumeradas en [`open-decisions.md`](../../planning/open-decisions.md) según la capacidad elegida.

## Ruta de revisión

1. Revisar la matriz de [decisiones del frontend](frontend-decisions.md).
2. Confirmar los límites normativos en el [cambio de fundación](../../../openspec/changes/frontend-foundation/proposal.md).
3. Revisar la preparación de la primera porción vertical en el [cambio de autenticación y consultas](../../../openspec/changes/frontend-auth-readonly-slice/proposal.md).
4. Aprobar las decisiones y congelar los contratos pendientes antes de crear código funcional.
5. Implementar la primera porción vertical solo después de publicar su contrato.
