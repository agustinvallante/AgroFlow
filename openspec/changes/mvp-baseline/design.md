# Diseño: baseline funcional del MVP

## Resumen

El baseline adopta decisiones simples, deterministas y demostrables para el MVP académico. El backend resuelve toda regla con estado persistido; frontend y n8n reúnen entradas, conservan metadatos de reintento y presentan el resultado confirmado. Las deltas se distribuyen entre capacidades de negocio existentes para evitar una especificación tecnológica paralela.

Este documento describe el diseño esperado para la implementación posterior. Los nombres definitivos de endpoints, DTOs, headers y códigos HTTP pertenecerán a `docs/contracts/openapi.yaml` y no se fijan en esta propuesta.

## Fuentes y restricciones

- [`docs/planning/mvp-defaults.md`](../../../docs/planning/mvp-defaults.md) contiene los defaults aprobados por el usuario para esta propuesta.
- [`docs/planning/open-decisions.md`](../../../docs/planning/open-decisions.md) permanece pendiente hasta revisión y archivo.
- [ADR-002](../../../docs/architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md) mantiene a OpenSpec como autoridad de comportamiento.
- [ADR-003](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md) mantiene al backend como autoridad operativa.
- El contrato OpenAPI, el código y la automatización quedan fuera de este cambio.

## Decisiones de diseño

### Prioridad y asignación

Las solicitudes elegibles se comparan mediante una clave lexicográfica estable: instante de corte más antiguo, flota propia antes que terceros cuando el corte coincide al minuto, instante de recepción más antiguo y, finalmente, identificador estable. No se persiste un puntaje editable. La misma comparación se usa para asignación inicial y reprogramación.

La preferencia de ventana es opcional. El backend intenta la preferida si es compatible y tiene capacidad; de lo contrario usa la primera ventana compatible posterior. Sin preferencia, selecciona la primera compatible. La respuesta contractual futura deberá distinguir preferencia, asignación y motivo de fallback sin exponer una fórmula que el consumidor deba recalcular.

### Calendario y capacidad

El calendario se carga explícitamente por fecha e ingenio. Cada ventana dura 30 minutos, usa el intervalo `[inicio, fin)`, no cruza la fecha operativa y tiene una capacidad entera positiva. Cada turno activo consume una unidad. La fecha operativa se interpreta en `America/Argentina/Tucuman` para el baseline.

Asignar, verificar exclusión de turno activo, consumir capacidad y registrar el resultado forma una única transacción. Restricciones persistentes protegen las invariantes y un token de versión permite detectar escrituras sobre estado obsoleto.

### Datos maestros

La asociación transportista-camión es una entidad explícita muchos a muchos con vigencia. Las altas y ediciones aceptan únicamente los campos funcionales aprobados; inhabilitación y reactivación reemplazan al borrado físico cuando existe historia.

La implementación deberá normalizar patente y teléfono antes de aplicar unicidad. El ingenio efectivo y los permisos se derivan de la identidad autenticada, nunca de un valor libre enviado por el consumidor. No se incorporan coordenadas, polígonos ni infraestructura geoespacial.

### Roles y autorización

Los roles internos son operador, supervisor, gerente y administrador. El backend declara y verifica permisos en cada caso de uso. La sesión podrá comunicar permisos efectivos para adaptar la interfaz, pero ocultar controles no autoriza ni protege operaciones.

- operador: consulta operativa, ciclo de vida e interrupciones;
- supervisor: lo anterior, más datos maestros y auditoría;
- gerente: consulta operativa y auditoría, sin mutaciones funcionales;
- administrador: administración de acceso y todas las operaciones internas del MVP.

Cada identidad interna tiene un único ingenio efectivo por sesión en el MVP.

### Idempotencia y concurrencia

Cada intención reintentable recibe una clave idempotente estable generada por el consumidor y una correlación. El backend persiste alcance, actor, operación, hash de entrada y resultado. La misma clave con la misma entrada devuelve el resultado original aun después de un reinicio; la misma clave con otra entrada se rechaza.

La correlación no sustituye a la idempotencia. Las mutaciones usan transacciones, restricciones de unicidad y versiones optimistas. Ante una carrera, solo un resultado se confirma; los demás reciben un conflicto recuperable y el estado vigente, sin escrituras parciales.

### Integración y notificaciones

El adaptador de canal valida la autenticidad del evento de entrada. n8n usa una credencial de servicio separada, secreta y limitada a operaciones del canal; no comparte secretos con el navegador. El identificador estable del mensaje, la correlación y la clave idempotente se conservan durante reintentos.

Las notificaciones salen de un registro durable tipo outbox creado junto con la operación que las origina. El backend decide destinatario, plantilla/datos y clave de deduplicación. La entrega atraviesa `PENDIENTE`, `ENVIADA`, `ENTREGADA` cuando exista confirmación o `FALLIDA`; se permiten tres intentos con espera incremental y luego reintento manual autorizado. Una falla de entrega no revierte una mutación operativa confirmada.

El proveedor concreto puede ser un adaptador reproducible de pruebas. Su SDK, firma y transporte exactos se documentarán fuera de las specs normativas.

### Auditoría

Las mutaciones críticas generan en la misma unidad transaccional un evento append-only con ingenio, actor o servicio, instante UTC, acción, entidad, resultado, motivo cuando corresponda, correlación y referencias mínimas del cambio. No se almacenan secretos, credenciales ni cuerpos completos. La consulta es de solo lectura y respeta rol e ingenio.

### Monitoreo

El backend calcula en una misma lectura consistente los conteos de la fecha operativa: total, `ASIGNADO`, `EN_CAMINO`, `EN_ESPERA`, en proceso (`INGRESADO + EN_DESCARGA`), `FINALIZADO` y `CANCELADO`, junto con interrupción activa, zona e instante de cálculo.

La interfaz puede consultar cada 30 segundos mientras esté visible, actualizar manualmente y volver a consultar al recuperar foco. No deriva conteos desde una lista parcial y no requiere WebSockets.

### Perfil académico de operación y recuperación

La persistencia canónica prevista es SQL Server. El ambiente de demostración usa datos ficticios, configuración por ambiente sin secretos versionados, migraciones repetibles, health checks y logs estructurados con correlación. Debe existir un procedimiento probado de backup/restore. SQLite, si se usa en pruebas aisladas, no reemplaza silenciosamente la persistencia de aceptación.

SQL Server, los mecanismos de backup y las bibliotecas son decisiones técnicas del diseño; la delta normativa exige únicamente durabilidad, reinicio y recuperación reproducible. El 99,9 % queda como objetivo futuro, no como criterio demostrado del MVP.

## Alternativas descartadas

| Tema | Alternativa | Motivo |
|---|---|---|
| Prioridad | Puntaje ponderado configurable | Agrega pesos no validados y dificulta explicar desempates. |
| Asociación | Un camión con un único transportista | La evidencia no justifica exclusividad y pierde reemplazos históricos. |
| Ventana | Reserva obligatoria elegida por el transportista | Duplica autoridad y no resuelve capacidad concurrente. |
| Calendario | Recurrencia y capacidad por tonelaje | Excede el mínimo demostrable. |
| Idempotencia | Caché en memoria o clave generada después del ingreso | No sobrevive reinicios ni evita reintentos duplicados. |
| Autorización | Controles solo en frontend | Es eludible y contradice la autoridad del backend. |
| Mensajería | Estado autoritativo en n8n | Diverge del turno persistido. |
| Monitoreo | WebSockets obligatorios | Añade operación sin ser necesario para el MVP. |
| Datos maestros | Borrado físico o geodatos | Rompe historia y amplía el alcance sin necesidad. |

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Duplicar requisitos entre capacidades | Cada delta define solo el comportamiento de su dominio y remite garantías transversales a plataforma. |
| Sobreasignar por concurrencia | Reserva transaccional, restricciones persistentes y versión optimista. |
| Reintentos con cargas distintas | Hash de entrada asociado a la clave idempotente y rechazo explícito. |
| Filtrar datos entre ingenios | Ingenio derivado de identidad, autorización backend y consultas aisladas. |
| Exponer secretos o datos personales en auditoría/logs | Lista mínima de campos y exclusión expresa de credenciales y cuerpos completos. |
| Confundir notificación fallida con operación fallida | Estado de entrega separado; no se revierte una mutación confirmada. |
| Convertir el perfil académico en afirmación productiva | SLO y alta disponibilidad quedan expresamente fuera de alcance. |

## Secuencia de implementación posterior

1. revisar y aprobar esta propuesta;
2. archivar las deltas en las especificaciones vigentes y actualizar el registro de decisiones;
3. publicar el contrato OpenAPI de la primera porción vertical;
4. implementar plataforma, identidad, segregación, idempotencia y auditoría;
5. implementar datos maestros y capacidad;
6. implementar solicitud, ciclo de vida e interrupciones;
7. integrar n8n, outbox y adaptador reproducible;
8. conectar vistas y monitoreo sin duplicar reglas;
9. ejecutar aceptación, reinicio y recuperación sobre datos controlados.

## Estrategia de pruebas

- pruebas de dominio para prioridad, ventanas, campos mínimos, permisos y transiciones;
- pruebas de persistencia para unicidad, asociación, idempotencia durable, versiones, auditoría y outbox;
- pruebas concurrentes para capacidad, turno activo y mutaciones obsoletas;
- pruebas de autorización positivas y negativas por rol e ingenio;
- pruebas de integración del canal para autenticidad, scopes, replay, deduplicación y fallas de entrega;
- pruebas de contrato para entradas, respuestas, conflictos y errores estables;
- pruebas de monitoreo con dataset conocido, fecha local e interrupción;
- prueba documentada de instalación limpia, reinicio, backup y restore;
- recorridos E2E del MVP repetidos sin edición manual de la base.
