# Diseño: preparación de la primera porción autenticada de solo lectura

## Resumen

La primera porción vertical futura conectará CU-001 (autenticación interna), contexto actual de usuario e ingenio, CU-004 (listado filtrable de turnos) y CU-005 (detalle de turno). La sesión web seguirá el modelo aceptado en [ADR-FE-006](../../../docs/architecture/decisions/ADR-FE-006-auth-session.md): identificador opaco en cookie `HttpOnly` y sesión del lado servidor en el BFF de Next.js.

Este diseño prepara decisiones y criterios de entrada. No implementa ni afirma la existencia de endpoints, DTO, cookies, almacenamiento, pantallas, código de backend o infraestructura. Tampoco agrega deltas normativas: cuando el contrato esté listo, el comportamiento nuevo o aclarado deberá incorporarse a `acceso-y-autorizacion` y `turnos-consulta`.

## Autoridad y límites

- [`acceso-y-autorizacion`](../../specs/acceso-y-autorizacion/spec.md) es la capacidad propietaria de la autenticación y deberá recibir las futuras deltas normativas necesarias para sesión y contexto autenticado.
- [`turnos-consulta`](../../specs/turnos-consulta/spec.md) es la capacidad propietaria del listado, los filtros y el detalle.
- [`plataforma-y-segregacion`](../../specs/plataforma-y-segregacion/spec.md) conserva sus garantías vigentes de aislamiento, fuente de verdad y errores sin información sensible.
- OpenAPI definirá la forma HTTP solo después de aprobar las deltas de negocio y completar el congelamiento contractual. Ya existen rutas de datos maestros y recibos de mutación en OpenAPI, pero son independientes: esta porción de autenticación/sesión/contexto y consultas de turnos todavía no tiene rutas aprobadas.
- El backend seguirá resolviendo identidad efectiva, roles, pertenencia y acceso al ingenio, autorización, reglas de turnos y estado operativo.
- El BFF mediará exclusivamente la sesión web; no decidirá ni duplicará ninguna de esas reglas.

## Flujo conceptual de la porción

1. La persona presenta sus credenciales mediante la experiencia web.
2. El BFF solicita al backend la autenticación según el contrato que se apruebe.
3. Una autenticación aceptada permitirá que el BFF mantenga o referencie la autenticación del backend del lado servidor y entregue al navegador únicamente un identificador opaco en una cookie `HttpOnly`, bajo la política todavía pendiente.
4. La aplicación obtiene del backend la identidad actual y el contexto efectivo de ingenio; no los deriva de parámetros libres ni de estado local.
5. La aplicación solicita el listado autorizado del ingenio y permite expresar los filtros vigentes de fecha, estado y patente.
6. La aplicación solicita el detalle de un turno seleccionado y presenta el estado vigente devuelto por el backend.
7. Listado y detalle permanecen inmutables: ninguna consulta cambia el turno.

El flujo no determina nombres, métodos, cantidad de operaciones, DTO, códigos, mecanismo de renovación ni separación interna de componentes. Esas formas pertenecen al congelamiento contractual.

## Decisiones de diseño ya tomadas

### Límite de sesión

El modelo aceptado es una cookie `HttpOnly` que contiene solo un identificador opaco y una sesión mantenida del lado servidor por el BFF de Next.js. El BFF almacenará o referenciará allí el material de autenticación emitido por el backend; el JavaScript cliente no leerá ni almacenará el JWT. Esto requiere runtime servidor y mantiene separadas la sesión humana y las credenciales de integraciones entre servicios.

El modelo está decidido, pero su implementación sigue pendiente. La elección no define TTL, custodia concreta del JWT, renovación, revocación, cierre, CSRF, atributos y dominios de cookie, almacenamiento servidor o proveedor, múltiples instancias, despliegue ni comportamiento ante fallas.

### Contexto efectivo

El backend será la fuente del usuario autenticado, su rol vigente y su ingenio efectivo. El frontend podrá usar esa respuesta para presentar contexto y adaptar navegación, pero no para sustituir la autorización de cada consulta.

### Consultas de turnos

El listado y el detalle respetarán los requisitos vigentes de `turnos-consulta`: aislamiento por ingenio, datos actuales del backend e inmutabilidad. El listado conservará sus filtros normativos y su comportamiento por defecto, una vez que se congelen las semánticas temporales y la forma contractual.

### Integración contract-first

No se implementará un adaptador ni una vista conectada para esta porción hasta que las deltas normativas y sus rutas OpenAPI sean aprobadas. Los contratos de datos maestros existentes no sustituyen estas rutas. Los tipos futuros deberán derivar del contrato; no se usarán mocks o tipos locales para completar campos ausentes como si fueran definitivos.

## Decisiones pendientes

| Tema | Decisión necesaria | Por qué bloquea implementación |
|---|---|---|
| Ciclo de sesión | TTL, relación con la expiración de credenciales, renovación, revocación y cierre. | Define continuidad, recuperación y finalización del acceso. |
| Custodia del JWT | Material del backend que el BFF conserva o referencia del lado servidor y su ciclo de vida. | Mantiene la credencial fuera del navegador y delimita su protección. |
| Cookie y CSRF | Atributos, dominios, ambientes y defensa CSRF aplicable. | Define el límite de seguridad entre navegador y BFF. |
| Almacenamiento servidor | Protección, limpieza y operación del estado de sesión, sin elegir todavía un proveedor. | Determina riesgos, disponibilidad y recuperación. |
| Múltiples instancias | Estado compartido, consistencia y afinidad si correspondiera. | Evita sesiones dependientes accidentalmente de una instancia. |
| Despliegue | Runtime, dominios, TLS, conectividad BFF-backend y comportamiento durante despliegues o reinicios. | El modelo no funciona como exportación exclusivamente estática. |
| Fallas de sesión | Comportamiento ante indisponibilidad o pérdida del almacenamiento, del BFF o del backend. | Evita estados ambiguos, accesos incorrectos y falsa confirmación de sesión. |
| Contexto | Forma y vigencia de identidad, rol e ingenio efectivo. | Impide inventar datos o confiar en selección local. |
| Roles | Matriz de roles y permisos. | La UI no puede fijar visibilidad funcional ni la API completar autorización sin esa decisión. |
| Fechas | Fecha operativa, zona horaria, serialización y límites. | El listado por defecto y el filtro por fecha deben ser inequívocos. |
| Consultas | Forma de lista, filtros y detalle, incluidos campos y límites. | Frontend y backend necesitan un contrato único y verificable. |
| Errores | Categorías, forma, correspondencias y recuperación permitida. | Evita inferencias incompatibles y filtración de información. |

## Lista de congelamiento contractual

Todos los puntos deben estar resueltos y revisados antes de modificar OpenAPI o iniciar implementación.

### Sesión

- [x] Fijar el modelo: el navegador almacena únicamente un identificador opaco en una cookie `HttpOnly` y el BFF mantiene o referencia la autenticación del backend del lado servidor.
- [ ] Definir la forma contractual de autenticación, consulta de sesión y cierre sin asumir nombres de endpoints.
- [ ] Definir el TTL de la sesión web y su relación con la expiración de las credenciales del backend.
- [ ] Definir la custodia del JWT dentro del límite servidor del BFF, incluida su protección y eliminación, sin exponerlo al navegador.
- [ ] Definir la renovación de la sesión y de las credenciales del backend sin asumir operaciones ni respuestas.
- [ ] Definir la revocación y su propagación a las sesiones activas.
- [ ] Definir el cierre de sesión y la invalidación del identificador opaco y del estado servidor asociado.
- [ ] Definir el comportamiento ante credenciales o sesiones inválidas, vencidas o ausentes sin inventar códigos de error.
- [ ] Confirmar la aplicación obligatoria de `HttpOnly` y definir los demás atributos de cookie, incluidos `Secure`, `SameSite`, alcance de ruta, dominio o subdominio y diferencias entre ambientes.
- [ ] Definir la estrategia CSRF coherente con la política de cookie y los orígenes permitidos.
- [ ] Definir cómo se protege, elimina y recupera el almacenamiento de sesión del lado servidor, sin seleccionar todavía un proveedor.
- [ ] Definir el comportamiento de sesión con múltiples instancias, incluido el estado compartido o la afinidad si correspondieran.
- [ ] Definir el comportamiento durante despliegues, reinicios y cambios de versión.
- [ ] Confirmar runtime servidor, terminación TLS y conectividad entre navegador, BFF y backend.
- [ ] Definir el comportamiento seguro ante indisponibilidad o pérdida del almacenamiento de sesión, del BFF o de la comunicación con el backend.

### Usuario y contexto actual

- [ ] Definir la representación contractual mínima de la identidad autenticada, sin inventar campos en este diseño.
- [ ] Definir cómo el backend comunica el ingenio efectivo y la vigencia del contexto.
- [ ] Definir qué ocurre cuando el usuario deja de estar activo o cambia su contexto durante una sesión.
- [ ] Confirmar que el cliente no pueda elegir ni reemplazar el ingenio efectivo mediante un parámetro libre.

### Aislamiento por ingenio

- [ ] Confirmar validación backend de pertenencia en contexto, listado y detalle.
- [ ] Definir respuestas que no revelen la existencia de turnos de otro ingenio.
- [ ] Definir claves y descarte de caché para impedir reutilización de datos entre contextos o sesiones.
- [ ] Incluir escenarios negativos de acceso cruzado tanto por listado como por identificador de detalle.

### Roles y autorización

- [ ] Resolver y aprobar la matriz de roles y permisos sin completarla desde el frontend.
- [ ] Definir qué datos de autorización necesita la experiencia web y cuál es su fuente.
- [ ] Separar adaptación visual de autorización efectiva en cada operación del backend.
- [ ] Definir comportamiento ante cambios de rol o pérdida de permiso durante una sesión.

### Fechas y zona horaria

- [ ] Definir fecha operativa y zona horaria autoritativas para el listado por defecto.
- [ ] Definir formato y semántica contractual de fechas y horarios.
- [ ] Definir límites inclusivos o exclusivos y comportamiento en cambios de día.
- [ ] Alinear filtro por fecha y orden por horario con esas decisiones.

### Consultas de lista y detalle

- [ ] Definir la forma contractual del listado sin asumir ruta, método ni DTO.
- [ ] Definir representación y validación de los filtros vigentes de fecha, estado y patente.
- [ ] Confirmar el comportamiento por defecto y el orden vigentes con la semántica temporal aprobada.
- [ ] Definir límites de volumen y cualquier mecanismo de recorrido solo si el contrato lo requiere.
- [ ] Definir la identidad contractual usada para solicitar un detalle sin fijarla en este documento.
- [ ] Definir los campos de lista y detalle desde necesidades aprobadas, no desde el prototipo.
- [ ] Confirmar que lista y detalle devuelvan estado vigente y no produzcan escrituras.

### Errores

- [ ] Definir un esquema estable y no sensible para los errores de autenticación, sesión, contexto y consultas.
- [ ] Aprobar las correspondencias entre categorías de resultado, transporte y experiencia; este diseño no las fija.
- [ ] Distinguir recuperación de sesión, falla del almacenamiento servidor, indisponibilidad del BFF o backend, falta de permiso, recurso no visible, entrada inválida y otras fallas técnicas sin filtrar existencia o datos.
- [ ] Definir correlación y observabilidad permitidas sin registrar credenciales, cookies ni datos sensibles.

### Evidencia de pruebas

- [ ] Acordar pruebas de contrato para todas las operaciones aprobadas y sus errores.
- [ ] Acordar evidencia de autenticación aceptada y rechazada sin enumeración de usuarios.
- [ ] Acordar evidencia del ciclo completo de sesión, incluidos TTL, expiración, renovación, revocación y cierre según el contrato final.
- [ ] Acordar pruebas de custodia servidor del JWT, CSRF, atributos y dominios de cookie y ausencia de exposición del JWT al cliente.
- [ ] Acordar pruebas de aislamiento por ingenio para contexto, listado y detalle, incluidos intentos cruzados.
- [ ] Acordar pruebas de filtros, fecha operativa, zona horaria, orden y estados vacío y de error.
- [ ] Acordar evidencia de que listado y detalle no modifican datos y reflejan el estado vigente.
- [ ] Acordar pruebas del almacenamiento servidor, el comportamiento multi-instancia, despliegues, reinicios e indisponibilidad de dependencias.
- [ ] Acordar pruebas frontend de carga, vacío, error, sesión vencida y accesibilidad contra el contrato aprobado.

## Puerta de entrada a implementación

La implementación podrá planificarse únicamente cuando:

1. la lista de congelamiento esté completa;
2. las deltas normativas aprobadas estén en `acceso-y-autorizacion` y `turnos-consulta`;
3. las decisiones transversales aplicables estén resueltas;
4. OpenAPI publique la forma acordada en un cambio posterior;
5. exista un plan de evidencia para backend, BFF, frontend, seguridad y aislamiento.

Hasta entonces, este cambio documenta intención y dependencias. No habilita código ni permite completar contratos con supuestos.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Confundir modelo aceptado con implementación | Los documentos distinguen el modelo aprobado de sus detalles operativos, contrato y código todavía pendientes. |
| Crear una capacidad normativa del frontend | Las futuras deltas se asignan a `acceso-y-autorizacion` y `turnos-consulta`. |
| Inventar formas HTTP desde la UI | La lista exige congelamiento y cambio OpenAPI posterior. |
| Filtrar datos entre ingenios | Contexto confirmado por backend, autorización por operación y pruebas negativas obligatorias. |
| Duplicar permisos, acceso al ingenio o reglas de turnos en el BFF | Backend como autoridad y BFF limitado a mediación de sesión. |
| Implementar fechas ambiguas | Fecha operativa y zona horaria deben aprobarse antes del contrato. |
| Diseñar sesión no operable en producción | Almacenamiento, múltiples instancias y despliegue forman parte de la puerta de entrada. |

## Trazabilidad

| Caso de uso | Capacidad propietaria | Alcance preparado |
|---|---|---|
| CU-001 | `acceso-y-autorizacion` | Autenticación interna y futura sesión web mediada por el BFF. |
| CU-004 | `turnos-consulta` | Listado aislado por ingenio con filtros vigentes. |
| CU-005 | `turnos-consulta` | Detalle vigente, aislado e inmutable de un turno. |

Esta trazabilidad no crea formas HTTP ni transfiere autoridad de negocio al BFF.

- [Propuesta de este cambio](proposal.md)
- [ADR-FE-006: estrategia de sesión web](../../../docs/architecture/decisions/ADR-FE-006-auth-session.md)
- [Arquitectura canónica del frontend](../../../docs/architecture/frontend/frontend-architecture.md)
- [Matriz de decisiones del frontend](../../../docs/architecture/frontend/frontend-decisions.md)
- [Catálogo de casos de uso](../../../docs/product/use-case-catalog.md)
- [Decisiones abiertas](../../../docs/planning/open-decisions.md)
