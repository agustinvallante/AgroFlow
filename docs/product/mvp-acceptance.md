# Guía de aceptación del MVP

## Propósito

Esta guía convierte la pregunta «¿ya es suficiente para presentar como MVP?» en una decisión verificable. Complementa la especificación normativa de [aceptación del MVP](../../openspec/specs/aceptacion-del-mvp/spec.md) con las evidencias y el recorrido que debe usar el equipo.

Un MVP suficiente no es un porcentaje de pantallas ni una colección de endpoints: es una porción vertical coherente que funciona desde sus puntos de entrada hasta la persistencia y vuelve a mostrar el estado confirmado por la API.

## Regla de aceptación

El candidato se considera **MVP aceptado** únicamente cuando se cumplen simultáneamente estas condiciones:

1. todas las puertas obligatorias de esta guía están en **Cumple**;
2. todos los requisitos `SHALL` y `MUST` de las nueve capacidades incluidas en el perfil obligatorio, y de la propia especificación de aceptación, tienen evidencia;
3. no queda una [decisión abierta](../planning/open-decisions.md) que impida ejecutar o evaluar un recorrido obligatorio;
4. no existe un defecto crítico o alto conocido que impida alguno de esos recorridos;
5. el guion completo se ejecutó satisfactoriamente dos veces con datos controlados, sin corregir la base manualmente;
6. OpenSpec, OpenAPI, frontend, backend, persistencia y n8n describen y utilizan el mismo comportamiento.

No se usa puntaje ni aprobación parcial: una puerta obligatoria pendiente significa que el MVP aún no está aceptado. Si el equipo decide quitar una capacidad, primero debe aprobar el cambio en OpenSpec; no alcanza con declararla «opcional» en una tarea o durante la presentación.

El [catálogo de casos de uso](use-case-catalog.md) y la [matriz de reglas de negocio](business-rules.md) permiten auditar la cobertura de los 12 casos de uso, las 54 reglas numeradas y sus 7 adendas. Las decisiones pendientes se evalúan según su resolución aprobada, no mediante una interpretación improvisada del PDF.

## Puertas obligatorias

| ID | Puerta | Resultado mínimo observable | Evidencia mínima |
|---|---|---|---|
| MVP-G01 | Preparación reproducible | Desde un clon limpio se configuran dependencias, migraciones y datos demo sin editar registros manualmente. | Guía ejecutada por otra persona y registro de los comandos o pipeline exitoso. |
| MVP-G02 | Acceso y segregación | Un usuario activo ingresa con su rol; un token inválido o expirado y el acceso a otro ingenio son rechazados por la API. | Pruebas de autenticación, rol y aislamiento más una comprobación integrada desde la interfaz. |
| MVP-G03 | Datos maestros | Los transportistas, teléfonos, camiones, tipos de flota, fincas y asociaciones necesarios se preparan de forma reproducible y persisten. Se aplican las reglas aprobadas de identidad, actividad, autorización e historial y las entidades inactivas no habilitan turnos nuevos. | Pruebas de la capacidad `datos-maestros`, preparación repetible y una operación de gestión representativa según el alcance que resuelva `OD-013`. |
| MVP-G04 | Solicitud y asignación | Una solicitud válida obtiene una ventana persistida; prioridad, capacidad, asociación y turno activo único se aplican en el backend. Los reintentos no duplican la operación. | Pruebas de éxito, falta de capacidad, duplicado, concurrencia e idempotencia más un turno creado de extremo a extremo. |
| MVP-G05 | Consulta, ciclo y cancelación | El mismo turno aparece en listado, detalle y filtros; recorre `ASIGNADO → EN_CAMINO → EN_ESPERA → INGRESADO → EN_DESCARGA → FINALIZADO`. Otro turno puede cancelarse, libera capacidad y queda consultable como `CANCELADO`. | Pruebas de transiciones válidas e inválidas, terminalidad y liberación de capacidad, más ambos recorridos integrados. |
| MVP-G06 | Dashboard operativo | Los indicadores del día y la interrupción visible provienen de la API y coinciden exactamente con un conjunto de datos conocido del ingenio del usuario. | Comparación de resultados esperados contra API y pantalla; la actualización periódica puede usar polling. |
| MVP-G07 | Interrupciones | Una interrupción activa reprograma los turnos `ASIGNADO` afectados, identifica sin reprogramar automáticamente los `EN_CAMINO`/`EN_ESPERA` afectados y no altera los que ya ingresaron al proceso. | Pruebas por estado y recorrido integrado con dashboard y destinatarios determinados por el backend. |
| MVP-G08 | WhatsApp y n8n | Solicitud, consulta y aviso `EN_CAMINO` pasan por n8n hacia la API real; al menos una interrupción genera una notificación para un destinatario determinado por el backend. Las respuestas reflejan solo operaciones persistidas y los reintentos no duplican cambios. | Flujo exportable de n8n, contrato de integración y ejecución reproducible con proveedor de pruebas o adaptador que emule el límite de WhatsApp. |
| MVP-G09 | Contrato e integración | OpenAPI documenta los endpoints usados y frontend, API y n8n comparten modelos, estados y errores compatibles. | Validación del contrato, pruebas de contrato o integración y ausencia de respuestas simuladas en los recorridos obligatorios. |
| MVP-G10 | Calidad, seguridad y trazabilidad | Cada escenario que integra el perfil obligatorio tiene una prueba automatizada o un paso manual justificado; no hay secretos ni datos personales reales en el repositorio. | Matriz requisito–escenario–evidencia, verificaciones exitosas y revisión de configuración y repositorio. |
| MVP-G11 | Auditoría y recuperación | Las mutaciones críticas conservan actor, momento y resultado según la política aprobada; el respaldo y la restauración de los datos de aceptación se ejecutan sin exponer información sensible. | Pruebas de auditoría y registro de un ejercicio exitoso de respaldo/restauración conforme a `OD-009` y al subconjunto académico de `OD-011`. |
| MVP-G12 | Presentación repetible | El guion completo funciona dos veces en el ambiente de demostración y los datos aceptados siguen disponibles después de reiniciar la aplicación. | Registro fechado de ambas ejecuciones, responsables, versión evaluada y resultado. |

El estado de estas puertas debe mantenerse en el proyecto de trabajo o en el acta de entrega, con enlace a evidencia. Esta página define qué verificar; no debe usarse para afirmar resultados que todavía no se ejecutaron.

## Guion mínimo de demostración

Este guion hace visible el recorrido principal; no reemplaza las pruebas de variantes y errores exigidas por las especificaciones.

1. Preparar el ambiente desde un clon limpio, aplicar migraciones y cargar datos demo documentados.
2. Iniciar sesión como usuario interno del ingenio A.
3. Preparar los datos maestros mediante el mecanismo documentado, ejecutar una operación de gestión aprobada y comprobar que una entidad inactiva no puede usarse para un turno nuevo.
4. Solicitar un turno con datos válidos y mostrar la ventana, prioridad y estado persistidos.
5. Consultar ese turno en listado, detalle y un filtro relevante del frontend.
6. Consultarlo mediante el flujo controlado de WhatsApp/n8n y comprobar que devuelve el mismo estado.
7. Informar `EN_CAMINO` mediante ese canal; continuar como operador por `EN_ESPERA`, `INGRESADO`, `EN_DESCARGA` y `FINALIZADO`.
8. Crear un segundo turno, cancelarlo desde un estado permitido y demostrar que conserva su historial y libera capacidad.
9. Preparar turnos en estados diferentes, registrar una interrupción, mostrar el tratamiento que corresponde a cada uno y entregar por el canal controlado una notificación indicada por la API.
10. Abrir el dashboard y contrastar sus indicadores y alerta de interrupción con los datos conocidos.
11. Intentar acceder a un recurso del ingenio B con el usuario del ingenio A y mostrar el rechazo del backend.
12. Reiniciar la aplicación y comprobar que los estados ya confirmados continúan disponibles.

Para la segunda ejecución se restaura el conjunto de datos mediante el mecanismo documentado, no mediante cambios manuales improvisados.

## Simplificaciones aceptables para la entrega académica

- Usar un proveedor de pruebas o un adaptador reproducible que emule el contrato de entrada y salida de WhatsApp, manteniendo n8n, la API real y la idempotencia del flujo. Una llamada manual a un paso interno de n8n no reemplaza el canal.
- Actualizar el dashboard mediante polling en lugar de WebSockets.
- Adoptar políticas simples y deterministas de prioridad, capacidad y desempate, una vez aprobadas y documentadas.
- Mantener una sola topología y un solo ambiente de demostración documentados.
- Usar datos demo ficticios y controlados, cargados mediante seed o un procedimiento repetible.
- Mostrar una operación representativa de cada CRUD durante la exposición, siempre que las restantes variantes estén cubiertas por pruebas.

Estas simplificaciones reducen complejidad de infraestructura o presentación; no eliminan reglas funcionales vigentes.

El perfil actual no exige cancelar un turno desde WhatsApp: la cancelación debe demostrarse desde un punto de entrada autorizado y siempre confirmarse mediante la API. Si el equipo decide ofrecerla también por WhatsApp, deberá incorporarla explícitamente a la especificación del canal antes de convertirla en criterio obligatorio.

## Lo que no constituye un MVP funcional

- un dashboard visual alimentado por arreglos, fixtures o respuestas simuladas;
- endpoints demostrados solo desde Swagger sin el recorrido integrado que corresponda;
- un happy path que pierde sus datos al reiniciar;
- reglas de prioridad, capacidad, permisos o transiciones implementadas únicamente en el frontend o en n8n;
- capturas estáticas de una conversación de WhatsApp;
- correcciones directas en la base de datos para poder continuar la demo;
- omitir una capacidad vigente porque no alcanza el tiempo, sin aprobar antes un cambio OpenSpec.

## No bloquea este MVP

Mientras permanezca fuera del alcance vigente, la aceptación no exige:

- geolocalización en tiempo real, mapas, ruteo o PostGIS;
- integración con ERP;
- analítica predictiva;
- aplicación móvil nativa;
- WebSockets;
- optimización logística externa al proceso de turnos;
- alta disponibilidad, despliegue multirregión o evidencia de un 99,9 % real en producción;
- acreditar ya una reducción real del 30 % del tiempo de espera.

Los dos últimos objetivos requieren una línea base, instrumentación y tiempo de observación. Para el MVP alcanza con definir cómo se medirán y conservar los datos necesarios; no corresponde afirmar todavía que fueron alcanzados.

## Decisiones que deben cerrarse

Antes de aceptar el MVP debe existir una respuesta aprobada para toda decisión que afecte un recorrido obligatorio:

- `OD-001` a `OD-006`: prioridad, relación transportista-camión, ventana, capacidad, solicitud e idempotencia;
- `OD-007`: roles y permisos;
- `OD-008` y `OD-010`: seguridad y entrega en el canal n8n/WhatsApp, al menos para el ambiente elegido;
- `OD-009`: auditoría mínima y conservación del historial;
- `OD-012`: fórmulas y refresco del dashboard;
- `OD-011`: configuración segura, registro de errores, respaldo y recuperación mínimos para la entrega. La acreditación de un SLO productivo puede quedar para una etapa posterior.
- `OD-013`: operaciones y campos de gestión que completarán CU-009, CU-010 y CU-012 sin asumir un CRUD desde el prototipo.

La fuente para ver el estado y el procedimiento de cierre es el [registro de decisiones abiertas](../planning/open-decisions.md).

## Registro de evidencia

Para cada puerta se recomienda registrar, como mínimo:

| Campo | Contenido |
|---|---|
| Puerta | Identificador `MVP-Gxx`. |
| Versión | Commit o etiqueta exacta evaluada. |
| Ambiente | Configuración y versión de los componentes. |
| Evidencia | Enlace a prueba, pipeline, captura, video o acta reproducible. |
| Resultado | `Cumple`, `No cumple` o `Bloqueada`. |
| Responsable | Persona que ejecutó la verificación. |
| Fecha | Momento de la ejecución. |
| Observaciones | Defecto, decisión o excepción aprobada relacionada. |

La aprobación final debe quedar asociada a una versión exacta del repositorio. Una modificación posterior necesita volver a ejecutar las puertas afectadas.
