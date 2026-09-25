# Defaults aceptados para el baseline del MVP

> **Estado: baseline aprobado por el usuario y aceptado como base de implementación, todavía no normativo.** Este documento no modifica por sí solo el comportamiento vigente de AgroFlow ni cierra ninguna decisión de [`open-decisions.md`](open-decisions.md). El baseline adquirirá carácter normativo únicamente cuando el cambio OpenSpec `mvp-baseline` sea revisado y archivado; no se debe implementar una capacidad dependiente antes de ese archivo.

## Propósito y criterios

Este baseline aceptado busca un MVP académico simple, determinista y demostrable. Se apoya en las especificaciones vigentes de [`openspec/specs/`](../../openspec/specs/), la [aceptación del MVP](../product/mvp-acceptance.md), la [hoja de ruta](implementation-roadmap.md) y la decisión de mantener al [backend como fuente operativa de verdad](../architecture/decisions/ADR-003-backend-fuente-de-verdad.md).

En cada decisión se separan:

- **Comportamiento de producto:** resultado observable que deberá quedar en OpenSpec si se aprueba.
- **Implementación técnica:** mecanismo recomendado para producir ese resultado; deberá quedar en ADR, OpenAPI o diseño técnico según corresponda.

El frontend y n8n pueden reunir entradas y presentar resultados, pero no calculan prioridad, capacidad, permisos, transiciones, destinatarios ni indicadores. El prototipo conservado en [`docs/contexto/AgroFlow-Dashboard/README.md`](../contexto/AgroFlow-Dashboard/README.md) es **solo referencia visual**: no define comportamiento, campos, contratos ni datos. Ningún mock del frontend puede presentarse como estado operativo.

## Resumen de aprobación

| ID | Default del baseline | Estado |
|---|---|---|
| OD-001 | Prioridad lexicográfica, explicable y estable | Aceptado; normatividad pendiente del archivo |
| OD-002 | Asociación explícita muchos a muchos | Aceptado; normatividad pendiente del archivo |
| OD-003 | Preferencia opcional y fallback automático | Aceptado; normatividad pendiente del archivo |
| OD-004 | Ventanas fijas de 30 minutos y capacidad entera configurable | Aceptado; normatividad pendiente del archivo |
| OD-005 | Solicitud mínima, sin datos de carga avanzados | Aceptado; normatividad pendiente del archivo |
| OD-006 | Idempotencia persistida y concurrencia transaccional | Aceptado; normatividad pendiente del archivo |
| OD-007 | Cuatro roles internos y autorización backend | Aceptado; normatividad pendiente del archivo |
| OD-008 | Credencial de servicio acotada y canal verificable | Aceptado; normatividad pendiente del archivo |
| OD-009 | Auditoría append-only de mutaciones críticas | Aceptado; normatividad pendiente del archivo |
| OD-010 | Outbox, estados de entrega y proveedor de pruebas | Aceptado; normatividad pendiente del archivo |
| OD-011 | Perfil académico reproducible de operación y recuperación | Aceptado para plataforma y aceptación; el SLO productivo queda fuera |
| OD-012 | Conteos diarios calculados por backend y polling de 30 segundos | Aceptado; normatividad pendiente del archivo |
| OD-013 | Gestión acotada, sin borrado físico ni geodatos | Aceptado; normatividad pendiente del archivo |

## OD-001 — Fórmula de prioridad

**Recomendación.** Ordenar solicitudes elegibles mediante una clave lexicográfica y estable:

1. fecha y hora de corte más antigua primero;
2. ante igualdad de minuto de corte, flota propia antes que flota de terceros;
3. ante nueva igualdad, solicitud recibida primero;
4. como desempate técnico final, identificador estable de la solicitud.

**Comportamiento de producto.** El backend asigna y reprograma usando ese orden y devuelve una explicación breve de los factores aplicados. Esto concreta los factores exigidos por [`turnos-solicitud-y-asignacion`](../../openspec/specs/turnos-solicitud-y-asignacion/spec.md) sin inventar un puntaje difícil de demostrar.

**Implementación técnica propuesta.** Comparación ordenada sobre datos persistidos; no almacenar un puntaje editable ni calcularlo en frontend o n8n.

**Rationale.** Es determinista, auditable y fácil de explicar con dos solicitudes. Evita pesos arbitrarios y conserva como factor principal la antigüedad de la caña.

**Alcance y no objetivos.** Incluye asignación inicial y reprogramación por interrupción. No incluye optimización predictiva, SLA por transportista, edición manual del orden ni aprendizaje automático.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Aplicar el orden sobre solicitudes elegibles y registrar los factores de la decisión. |
| API | Exponer el resultado y una explicación estable, no una fórmula recalculable por el consumidor. |
| Frontend | Mostrar prioridad/explicación recibida; no ordenar para decidir asignaciones. |
| Pruebas | Casos por antigüedad, tipo de flota y ambos desempates; misma salida en reintentos. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-002 — Cardinalidad transportista-camión

**Recomendación.** Adoptar una asociación explícita **muchos a muchos**, con estado activo, fecha de alta y fecha opcional de inhabilitación. Un transportista puede estar autorizado para varios camiones y un camión puede tener varios transportistas autorizados.

**Comportamiento de producto.** Solo una asociación activa habilita al transportista a solicitar o consultar turnos de ese camión, como exige [`turnos-consulta`](../../openspec/specs/turnos-consulta/spec.md). Inhabilitar una asociación no altera turnos históricos.

**Implementación técnica propuesta.** Entidad asociativa persistida y validada por el backend, con restricción de unicidad para el par activo; no una lista embebida ni una relación inferida por el frontend.

**Rationale.** Evita fijar una propiedad exclusiva que el relevamiento no confirma y permite representar reemplazos de chofer sin perder historia.

**Alcance y no objetivos.** Incluye autorización de uso y conservación histórica. No modela empleo, propiedad legal, licencias de conducir, horarios ni asignación de choferes por viaje.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Nueva asociación con vigencia e invariantes de autorización. |
| API | Consultar, crear e inhabilitar asociaciones dentro del ingenio autorizado. |
| Frontend | Gestionar asociaciones según OD-007/OD-013; mostrar solo confirmación del backend. |
| Pruebas | Múltiples asociaciones, asociación inactiva, duplicado y conservación de turnos históricos. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-003 — Elección de ventana

**Recomendación.** Permitir una **ventana preferida opcional**. Si no se informa, AgroFlow asigna la primera ventana compatible según capacidad y prioridad. Si se informa y no tiene lugar, aplica el fallback normativo a la próxima compatible.

**Comportamiento de producto.** El transportista puede expresar preferencia, pero no reservar ni forzar una ventana. La asignación final siempre pertenece al backend y puede diferir de la preferida.

**Implementación técnica propuesta.** Campo opcional en el contrato de solicitud y respuesta explícita con ventana solicitada, ventana asignada y motivo del fallback cuando corresponda.

**Rationale.** Conserva la intención de RN-016, permite un diálogo corto por WhatsApp y evita dos modos de asignación incompatibles.

**Alcance y no objetivos.** No incluye selección gráfica de múltiples franjas, negociación iterativa, lista de espera ni bloqueo temporal mientras el usuario responde.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Validar preferencia y resolver siempre la asignación efectiva. |
| API | Preferencia opcional y resultado autoritativo con motivo de sustitución. |
| Frontend | Si en el futuro inicia solicitudes, presenta disponibilidad informativa; no promete la preferencia. |
| Pruebas | Sin preferencia, preferencia disponible, preferencia completa y ausencia total de capacidad. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-004 — Modelo de capacidad

**Recomendación.** Usar ventanas fijas de **30 minutos**, con capacidad entera positiva configurable por ingenio y ventana. El calendario se carga explícitamente por fecha para el ambiente de demostración, sin motor de recurrencia. La zona horaria propuesta es `America/Argentina/Tucuman`; los intervalos son `[inicio, fin)` y no cruzan la fecha operativa.

**Comportamiento de producto.** Cada turno consume una unidad de capacidad. La fecha operativa se determina en la zona del ingenio. Una cancelación libera una unidad y una interrupción invalida las ventanas alcanzadas según [`interrupciones-operativas`](../../openspec/specs/interrupciones-operativas/spec.md). Solo supervisor y administrador pueden configurar ventanas y capacidad, conforme a la matriz propietaria de `acceso-y-autorizacion`.

**Implementación técnica propuesta.** Persistir calendario/capacidad y reservar dentro de la misma transacción de asignación. Los datos demo definirán horarios y capacidades concretos; no se codifican en el frontend.

**Rationale.** Treinta minutos es legible durante la demo y el calendario explícito elimina ambigüedad sin construir un planificador avanzado.

**Alcance y no objetivos.** No incluye capacidad por toneladas, múltiples docks, feriados recurrentes, ventanas solapadas, turnos que crucen medianoche ni optimización de recursos.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Calendario, consumo/liberación transaccional y fecha operativa local. |
| API | Fechas con offset, disponibilidad consultable y errores estables de capacidad. |
| Frontend | Mostrar horarios devueltos; no generar ventanas desde el reloj local. |
| Pruebas | Límites inclusivo/exclusivo, zona horaria, capacidad agotada, cancelación, concurrencia e interrupción. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-005 — Datos mínimos de una solicitud

**Recomendación.** Exigir: transportista identificado por el canal, camión/patente, finca, fecha y hora de corte de la caña y, opcionalmente, ventana preferida. El ingenio se deriva del contexto autorizado; canal, instante de recepción, correlación e idempotencia son metadatos técnicos. El tipo de flota proviene del camión persistido.

**Comportamiento de producto.** Transportista, camión, finca y asociación deben estar activos; la fecha de corte no puede ser futura; una solicitud incompleta o inconsistente no consume capacidad. La carga del MVP se considera caña y no requiere peso estimado, variedad ni documentación de transporte.

**Implementación técnica propuesta.** DTO validado en el límite, referencias por identificadores contractuales y normalización de patente/teléfono en backend; no confiar en nombres o tipo de flota enviados por el consumidor.

**Rationale.** Es el conjunto mínimo para identidad, autorización, origen y prioridad sin agregar captura que no participa en reglas vigentes.

**Alcance y no objetivos.** No incluye peso, humedad, variedad, coordenadas, ruta, remito, fotos ni geolocalización.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Validaciones de actividad, asociación, pertenencia, fecha y referencias. |
| API | Esquema mínimo de solicitud y errores por campo sin exponer datos sensibles. |
| Frontend | Si ofrece el flujo, captura solo campos aprobados y vuelve a mostrar el resultado del backend. |
| Pruebas | Campos ausentes, fecha futura, entidades inactivas, asociación inválida y solicitud válida. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-006 — Concurrencia e idempotencia

**Recomendación.** Cada operación de creación y mutación crítica del baseline lleva una clave idempotente creada por el consumidor/BFF/n8n y un identificador de correlación. Esto incluye alta, edición, inhabilitación, reactivación y asociación de datos maestros; solicitud y asignación de turnos; transiciones del ciclo de vida; cancelación; creación o actualización de interrupciones; y reintentos de notificaciones cuando corresponda. El backend persiste clave, actor, operación, hash de entrada y resultado; repetir la misma clave y entrada devuelve el resultado original, y reutilizarla con otra entrada se rechaza. Asignación, capacidad y exclusión de turno activo se resuelven en una transacción con restricciones de base y control optimista de versión.

**Comportamiento de producto.** Un reintento legítimo no duplica ninguna creación o mutación crítica del baseline. Ante conflicto concurrente, solo un resultado queda confirmado y el otro actor recibe el estado vigente permitido o un conflicto recuperable.

**Implementación técnica propuesta.** Registro idempotente persistido, índices únicos y token de versión; la correlación puede generarse en el ingreso si falta, pero no sustituye a la clave idempotente.

**Rationale.** La clave debe sobrevivir reinicios y reintentos de n8n. Una caché local o una clave generada recién después de recibir la solicitud no evita duplicados.

**Alcance y no objetivos.** Aplica a todas las operaciones de creación y mutaciones críticas enumeradas para el baseline, no solo a las que un contrato declare reintentables. No promete exactamente-una-vez en la red ni usa bloqueos distribuidos como primer mecanismo.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Atomicidad, deduplicación persistida, índices y resolución de conflictos. |
| API | Header/campo contractual de idempotencia, correlación y respuestas repetibles. |
| Frontend | El BFF conserva la clave durante reintentos; la UI no asume éxito optimista. |
| Pruebas | Reintento idéntico, clave reutilizada con otro cuerpo, solicitudes simultáneas y recuperación tras falla. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-007 — Matriz de roles y permisos

**Recomendación.** Usar cuatro roles internos, siempre acotados al ingenio efectivo. La matriz completa y sus concesiones pertenecen únicamente a la delta [`acceso-y-autorizacion`](../../openspec/changes/mvp-baseline/specs/acceso-y-autorizacion/spec.md); las demás capacidades deben referenciar sus permisos y no volver a declarar listas de roles.

**Comportamiento de producto.** Toda operación se vuelve a autorizar en backend según esa matriz. Como límites explícitos del baseline, la configuración de ventanas/capacidad y el reintento manual de notificaciones fallidas corresponden a supervisor y administrador.

**Implementación técnica propuesta.** Permisos declarados del lado servidor y comunicados en el contexto autenticado para adaptar la UI. No inferir permisos por el texto del rol ni por controles ocultos.

**Rationale.** Una única matriz cubre demo, separación de funciones y los cuatro roles ya considerados por la arquitectura, sin duplicar concesiones ni introducir un motor dinámico de políticas.

**Alcance y no objetivos.** No incluye roles personalizados, permisos por registro, delegaciones temporales, aprobación en dos pasos ni acceso multiingenio en una misma sesión.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Autorizar cada caso de uso y derivar el ingenio desde la identidad. |
| API | Contexto actual y permisos efectivos; respuestas sin revelar recursos ajenos. |
| Frontend | Navegación y acciones adaptadas, sin convertirse en control de seguridad. |
| Pruebas | Matriz positiva/negativa, cambio de rol y acceso cruzado entre ingenios. |

**Estado de aprobación.** Aceptado por el usuario como baseline; la matriz será normativa únicamente después de revisar y archivar el cambio OpenSpec. El límite de arquitectura frontend aceptado permanece en [ADR-FE-007](../architecture/decisions/ADR-FE-007-roles-autorizacion.md).

## OD-008 — Seguridad del canal n8n

**Recomendación.** El proveedor/adaptador valida la firma del webhook de WhatsApp; n8n llama a la API por TLS con una credencial de servicio propia, secreta y limitada a operaciones del canal. La API solo acepta identidad de remitente proveniente de esa integración autenticada. Cada mensaje lleva identificador del proveedor, correlación e idempotencia persistida; los reintentos reutilizan esos valores.

**Comportamiento de producto.** El teléfono del remitente identifica al transportista, pero no basta por sí solo: el backend valida transportista activo, patente y asociación. Una repetición no duplica una mutación y una falla no se comunica como éxito, conforme a [`integracion-whatsapp-n8n`](../../openspec/specs/integracion-whatsapp-n8n/spec.md).

**Implementación técnica propuesta.** Secreto solo en n8n/servidor, rotación manual documentada para el MVP, scopes de integración, validación temporal y deduplicación. Ninguna API key se comparte con el navegador.

**Rationale.** Separa sesión humana de integración, protege el límite real y sigue siendo operable en un ambiente académico.

**Alcance y no objetivos.** No incluye identidad federada del transportista, biometría, dispositivo confiable, mTLS obligatorio ni gestión automática de secretos empresarial.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Autenticar servicio, validar identidad contextual y deduplicar mensajes. |
| API | Esquema de seguridad separado y operaciones acotadas para integración. |
| Frontend | Ningún secreto ni flujo de transportista; solo puede mostrar resultados ya persistidos. |
| Pruebas | Firma inválida, credencial ausente/sin scope, remitente desconocido, replay y reintento. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-009 — Auditoría y conservación

**Recomendación.** Registrar en forma append-only toda alta/inactivación de datos maestros, asociación, asignación, transición, cancelación, reprogramación, interrupción y cambio de acceso. Cada evento conserva ingenio, actor o servicio, instante UTC, acción, entidad, resultado, motivo cuando se exige, correlación y referencias mínimas al antes/después. La consulta de solo lectura se autoriza mediante el permiso definido por la matriz propietaria de `acceso-y-autorizacion`. Para el MVP académico, conservar los eventos durante todo el ciclo de la entrega.

**Comportamiento de producto.** Una persona autorizada puede explicar quién produjo una mutación crítica, cuándo y con qué resultado. El historial no puede editarse desde la UI.

**Implementación técnica propuesta.** Tabla append-only escrita en la misma unidad transaccional que la mutación; valores sensibles, credenciales y cuerpos completos quedan excluidos.

**Rationale.** Satisface evidencia y demostración sin introducir una plataforma externa de eventos.

**Alcance y no objetivos.** No es event sourcing, no reconstruye todo el dominio, no reemplaza logs técnicos y no define retención legal de producción.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Emitir eventos consistentes e impedir actualización/borrado por operaciones normales. |
| API | Consulta filtrada y paginada para roles autorizados; sin endpoint de edición. |
| Frontend | Vista de historial solo lectura y sin datos sensibles. |
| Pruebas | Actor, momento, resultado, correlación, aislamiento y ausencia de secretos. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-010 — Entrega de notificaciones

**Recomendación.** Para aceptación usar un proveedor de pruebas o adaptador reproducible de WhatsApp. El backend crea una notificación/outbox persistida con destinatario, plantilla versionada, datos, correlación y clave de deduplicación. Estados mínimos: `PENDIENTE`, `ENVIADA`, `ENTREGADA` cuando el proveedor lo informe y `FALLIDA`. n8n entrega y reporta estado; se permiten hasta tres intentos con espera incremental y luego queda falla visible para reintento manual autorizado.

**Comportamiento de producto.** El backend determina destinatarios y cambios. Una notificación fallida no revierte una operación ya confirmada ni se presenta como entregada. Solo supervisor y administrador pueden ordenar su reintento manual, conforme a la matriz propietaria de `acceso-y-autorizacion`. Los mensajes cubren confirmación/reprogramación y la interrupción mínima exigida por aceptación.

**Implementación técnica propuesta.** Patrón outbox, plantillas fuera del flujo, callbacks idempotentes y adaptador sustituible; n8n no mantiene estado operativo del turno.

**Rationale.** Hace visible la entrega y sus fallas sin depender de un proveedor productivo para la demo.

**Alcance y no objetivos.** No incluye campañas, preferencias avanzadas, múltiples proveedores simultáneos, SLA de entrega ni editor de plantillas en la UI.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Crear outbox después de persistir el cambio y controlar deduplicación/estado. |
| API | Operaciones acotadas para obtener entregas y confirmar resultados. |
| Frontend | Mostrar estado de notificación donde aporte contexto; no enviar mensajes directamente. |
| Pruebas | Éxito, callback repetido, tres fallas, reintento y operación confirmada con entrega fallida. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-011 — Operación y calidad de servicio

**Recomendación.** Definir un perfil académico reproducible: SQL Server como persistencia canónica, un ambiente de demostración documentado, datos ficticios, health checks, logs estructurados con correlación, configuración por ambiente sin secretos versionados y procedimiento probado de backup/restore. Ejecutar restauración antes de aceptación y conservar su evidencia. El 99,9 % queda como objetivo futuro medible, no como puerta demostrada.

**Comportamiento de producto.** El estado aceptado sobrevive reinicios y puede restaurarse; fallas se muestran sin exponer información sensible. No se afirma disponibilidad real sin medición productiva.

**Implementación técnica propuesta.** Migraciones repetibles, seed controlado, respaldo nativo de SQL Server para el ambiente elegido y métricas básicas de salud/latencia/error. SQLite solo podrá usarse en pruebas aisladas, nunca como sustituto silencioso de la persistencia de aceptación.

**Rationale.** Prioriza repetibilidad y recuperación, que sí pueden demostrarse, frente a una promesa productiva imposible de probar en la entrega.

**Alcance y no objetivos.** No incluye alta disponibilidad, multirregión, failover automático, SRE 24x7, datos personales reales ni certificación de un SLO productivo.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Configuración segura, persistencia durable, salud, logs y migraciones. |
| API | Correlación y errores estables; endpoints de salud sin datos sensibles. |
| Frontend | Estados de indisponibilidad y correlación permitida; sin trazas internas. |
| Pruebas | Instalación limpia, reinicio, backup/restore, ausencia de secretos y datos reales. |

**Estado de aprobación.** Aceptado por el usuario para la plataforma y la evidencia de aceptación; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec. El diseño productivo posterior no integra este baseline.

## OD-012 — Indicadores y refresco del dashboard

**Recomendación.** Para la fecha operativa local seleccionada, el backend devuelve estos conteos del ingenio: total de turnos, `ASIGNADO`, `EN_CAMINO`, `EN_ESPERA`, en proceso (`INGRESADO + EN_DESCARGA`), `FINALIZADO` y `CANCELADO`, más la interrupción activa si existe. Un turno pertenece a la fecha de su ventana asignada. El frontend consulta cada **30 segundos** mientras la vista está visible y ofrece refresco manual.

**Comportamiento de producto.** Los conteos provienen del estado persistido y comparten una misma fecha de corte. El dashboard indica instante de actualización e interrupción activa. No deriva métricas desde filas parciales.

**Implementación técnica propuesta.** DTO agregado calculado por backend; polling con TanStack Query, pausa al ocultar la pestaña y nueva consulta al recuperar foco. WebSockets no son requisito, según [`monitoreo-operativo`](../../openspec/specs/monitoreo-operativo/spec.md) y [ADR-FE-010](../architecture/decisions/ADR-FE-010-dashboard-refresh.md).

**Rationale.** Los conteos por estado son verificables con un dataset conocido y el intervalo mantiene una demo dinámica sin infraestructura en tiempo real.

**Alcance y no objetivos.** No incluye promedio de espera, reducción del 30 %, predicción, series históricas, mapas, posición estimada ni acreditación de KPI productivos.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | Calcular todos los indicadores con una consulta consistente y aislada. |
| API | DTO con fecha operativa, zona, instante de cálculo, conteos e interrupción. |
| Frontend | Mostrar valores y frescura; polling configurable, sin cálculo de negocio. |
| Pruebas | Dataset conocido, cambio de estado, aislamiento, fecha local, interrupción y polling visible/oculto. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## OD-013 — Alcance de gestión de datos maestros

**Recomendación.** Incluir listado, detalle, alta, edición de campos permitidos, inhabilitación y reactivación; nunca borrado físico con historia. Alcance del baseline:

| Entidad | Campos funcionales mínimos | Operaciones del MVP |
|---|---|---|
| Transportista | nombre, DNI único global, WhatsApp único global, activo | listar, ver, alta, editar, inhabilitar/reactivar |
| Camión | patente normalizada única global, tipo de flota (`PROPIA` o `TERCEROS`), activo | listar, ver, alta, editar, inhabilitar/reactivar |
| Finca | código único por ingenio, nombre, referencia textual de ubicación, activo | listar, ver, alta, editar, inhabilitar/reactivar |
| Asociación | transportista, camión, vigencia activa | listar, alta, inhabilitar/reactivar |

**Comportamiento de producto.** El DNI, el número de WhatsApp y la patente normalizada son identificadores únicos globales de la plataforma. Un conflicto dentro del mismo ingenio puede identificar el campo; un conflicto con otro ingenio se rechaza sin revelar la existencia, el ingenio ni los datos del recurso ajeno. Todas las lecturas y mutaciones permanecen aisladas por ingenio. Inhabilitar impide nuevas solicitudes pero conserva referencias históricas. La gestión se autoriza mediante el permiso definido por la matriz propietaria de `acceso-y-autorizacion`.

**Implementación técnica propuesta.** Actualizaciones parciales explícitas, versión de concurrencia y auditoría. Catálogos devueltos por API; no hardcodear tipos de flota ni permisos en el frontend.

**Rationale.** Completa CU-009, CU-010 y CU-012 con el mínimo necesario para operar turnos y repetir la demo.

**Alcance y no objetivos.** No incluye borrado físico, importación masiva, adjuntos, geocodificación, coordenadas, polígonos/PostGIS, datos fiscales ni gestión avanzada de flota.

**Impacto.**

| Área | Impacto esperado |
|---|---|
| Backend | CRUD acotado, unicidad, vigencia, historial, autorización y aislamiento. |
| API | Filtros/listas, detalle y mutaciones explícitas con errores por conflicto. |
| Frontend | Pantallas administrativas conectadas a la API, sin asumir CRUD desde el prototipo. |
| Pruebas | Duplicados, permisos, inhabilitación/reactivación, concurrencia e historia preservada. |

**Estado de aprobación.** Aceptado por el usuario como baseline; su normatividad queda pendiente de revisar y archivar el cambio OpenSpec.

## Cómo convertir este baseline en comportamiento normativo

1. Revisar el cambio [`mvp-baseline`](../../openspec/changes/mvp-baseline/) contra las capacidades propietarias y este baseline aceptado.
2. Ajustar y aprobar las deltas mediante la revisión OpenSpec correspondiente.
3. Archivar el cambio para consolidar el comportamiento en [`openspec/specs/`](../../openspec/specs/).
4. Actualizar [`open-decisions.md`](open-decisions.md) al completar el archivo y registrar las decisiones técnicas en [`docs/architecture/decisions/`](../architecture/decisions/) cuando corresponda.
5. Actualizar OpenAPI, pruebas y documentación junto con cada porción de implementación posterior.
6. Recién entonces implementar las capacidades dependientes.

Hasta completar la revisión y el archivo, todos los valores de este documento están **aceptados como baseline de implementación, pero no son normativos**, y las decisiones de [`open-decisions.md`](open-decisions.md) permanecen pendientes.
