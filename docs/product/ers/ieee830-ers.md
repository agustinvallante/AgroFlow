# Especificación de requisitos de software (ERS) — AgroFlow

> **BORRADOR COMPLETO PARA REVISIÓN — No aprobado.** Esta ERS cubre los 17 apartados académicos y describe el corte funcional identificado por el commit `8873a2644b2fd427e571c832b04f5dfa83647a9a` en la rama `main`. La revisión y aceptación académica siguen pendientes; este documento no acredita la aceptación del MVP ni sustituye las especificaciones normativas vigentes. Se distingue entre comportamiento normativo, implementación LocalDemo observada, limitaciones y trabajo futuro.

## 1. Introducción

### 1.1. Propósito del documento

Esta Especificación de Requisitos de Software (ERS) documenta, con fines académicos y para revisión, el sistema AgroFlow y el alcance funcional observado en un corte identificado del repositorio. Su propósito es ofrecer una descripción organizada y trazable del producto, sus actores, contexto, límites y requisitos específicos que se incorporarán en el capítulo 3.

La audiencia prevista comprende personas que revisan la documentación académica y quienes analizan o mantienen el producto. El documento debe permitir distinguir lo que está especificado como comportamiento vigente, lo que se observó implementado en el corte citado y lo que permanece parcial, no implementado, no verificado o abierto. No propone desarrollar un sistema nuevo, no sustituye las especificaciones normativas del proyecto y no constituye aceptación formal del MVP ni aprobación de requisitos.

### 1.2. Alcance del sistema

AgroFlow es un producto B2B orientado a coordinar la recepción de camiones en ingenios durante la zafra. Su alcance previsto comprende una aplicación web para la operación interna, una API backend que centraliza las reglas y el estado operativo, y un canal conversacional de WhatsApp mediado por n8n. Entre las capacidades definidas para el MVP están la gestión de datos maestros, solicitud, asignación, consulta y ciclo de vida de turnos, aislamiento por ingenio, monitoreo operativo e integración conversacional. La inclusión de una capacidad en este alcance previsto no significa que esté completa o implementada en el corte revisado.

En el corte funcional identificado en este documento, la superficie web observada está conectada a la API para consultar, filtrar, crear, transicionar y cancelar turnos; listar, registrar e inhabilitar transportistas; y presentar conversaciones e historial persistidos. La API ofrece además lectura del catálogo de fincas. La edición de transportistas desde la API no equivale a una función disponible en la interfaz web. El panel muestra camiones en curso derivados de turnos, pero sus métricas, cola visual, feed, molienda y avisos carecen de datos API; las secciones de reportes y configuración son presentaciones sin datos operativos ni acciones habilitadas. Estas diferencias limitan lo que puede afirmarse sobre el producto realizado y no reducen ni amplían por sí solas el alcance normativo aprobado.

El perfil LocalDemo observado no implementa un inicio de sesión web operativo ni la autenticación individual completa del MVP; las claves de integración no representan identidades de usuarios. Por ello, este documento no presenta el corte como un servicio multiusuario o multiingenio listo para operación real. Geolocalización en tiempo real, ruteo cartográfico avanzado, polígonos de fincas/PostGIS, integración con ERP, analítica predictiva y obligación de WebSockets están fuera del alcance declarado del MVP. Las capacidades futuras o sujetas a decisión se tratan como tales, no como funcionalidades disponibles.

Los objetivos de producto —como reducir tiempos de espera o alcanzar una disponibilidad determinada— son objetivos que requieren una línea base y observación operativa. No se presentan aquí como beneficios medidos, resultados alcanzados ni garantías de servicio.

### 1.3. Definiciones, acrónimos y abreviaturas

| Término | Definición en esta ERS |
|---|---|
| **AgroFlow** | Sistema B2B para coordinar operaciones de recepción de camiones en ingenios. |
| **API** | Interfaz HTTP del backend mediante la que los consumidores consultan o solicitan operaciones de AgroFlow. |
| **Camión** | Vehículo identificado por una patente normalizada y clasificado por tipo de flota. |
| **ERS** | Especificación de Requisitos de Software. |
| **Finca** | Origen o procedencia de la carga; en el alcance del MVP es una referencia descriptiva, sin polígonos geográficos. |
| **Ingenio** | Establecimiento industrial que recibe y procesa caña; es el límite de aislamiento previsto para datos y operación. |
| **LocalDemo** | Perfil de ejecución local de demostración. Sus credenciales y límites no representan por sí mismos la seguridad del MVP ni un despliegue productivo. |
| **n8n** | Adaptador de integración que comunica el canal conversacional con la API; no es autoridad de reglas ni de estado operativo. |
| **RF / RNF** | Requisito funcional / requisito no funcional. Los identificadores concretos se asignarán en el capítulo 3. |
| **Turno** | Reserva operativa para recibir un camión en una ventana determinada; conserva datos, estado e historial operativo. |
| **Transportista** | Persona o entidad asociada explícitamente a uno o más camiones autorizados conforme a las reglas vigentes. |
| **Ventana horaria** | Intervalo de recepción con capacidad limitada. Duración, cupo y calendario dependen de configuración y decisiones de negocio; los valores de demostración no son reglas generales. |
| **WhatsApp** | Canal de mensajería usado por el flujo conversacional integrado; la existencia de componentes de integración no prueba una entrega extremo a extremo con proveedores reales. |
| **Zafra** | Período anual de cosecha y procesamiento de la caña; no sustituye la fecha operativa de un turno. |

Los estados y transiciones de turnos se rigen por la especificación vigente de ciclo de vida y el glosario del proyecto. La unidad de la carga estimada, el calendario de recepción, la zona horaria y otros detalles enumerados en las decisiones abiertas no se completan por inferencia.

### 1.4. Referencias

Las referencias se identifican mediante códigos estables para usarlos en los requisitos y en la trazabilidad. Las rutas de fuentes del repositorio apuntan a una versión inmutable del corte funcional, no a `main` mutable. Los materiales académicos privados se citan por su función y título genérico, sin publicar copias, datos personales ni rutas locales.

| ID | Fuente | Autoridad y uso en esta ERS |
|---|---|---|
| **R-01** | Consigna académica «Documentación de requerimientos de software», Trabajo Final Integrador, edición 2026. Material privado de la asignatura. | Define las 17 subsecciones solicitadas, el mínimo de diez RF y que la ERS describa el sistema realizado. Fuente académica del encargo; no es especificación del producto. |
| **R-02** | Ejemplo académico de ERS con estructura IEEE 830, material de referencia privado. | Referencia ilustrativa de organización y nivel de detalle. Corresponde a otro sistema y no aporta requisitos de AgroFlow. |
| **R-03** | Plantilla académica genérica de ERS, material de referencia privado. | Referencia ilustrativa subordinada a R-01; no define requisitos de AgroFlow. |
| **R-04** | [Corte funcional de AgroFlow: commit `8873a2644b2fd427e571c832b04f5dfa83647a9a`](https://github.com/agustinvallante/AgroFlow/tree/8873a2644b2fd427e571c832b04f5dfa83647a9a). | Fuente del código y documentación del sistema observado; la inspección estática no equivale a una prueba de ejecución. |
| **R-05** | [Especificaciones vigentes de OpenSpec](https://github.com/agustinvallante/AgroFlow/tree/8873a2644b2fd427e571c832b04f5dfa83647a9a/openspec/specs). | Fuente normativa del comportamiento aprobado. En particular: acceso y autorización, datos maestros, integración WhatsApp/n8n, interrupciones operativas, monitoreo, plataforma y segregación, y capacidades de turnos. |
| **R-06** | [Contrato OpenAPI](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/contracts/openapi.yaml). | Define la superficie y los esquemas HTTP publicados en el corte; no demuestra por sí solo disponibilidad ni uso desde cada vista. |
| **R-07** | [ADR-002: Jerarquía documental y OpenSpec](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md). | Establece autoridad de las fuentes y resolución de contradicciones. |
| **R-08** | [Glosario del dominio](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/domain/glossary.md). | Fuente terminológica del dominio y de los estados de turno. |
| **R-09** | [Catálogo de casos de uso](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/product/use-case-catalog.md). | Referencia para relacionar los requisitos específicos con capacidades y actores. |
| **R-10** | [Registro de decisiones abiertas](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/planning/open-decisions.md). | Identifica límites aún no resueltos; una cuestión abierta no se convierte aquí en una regla supuesta. |
| **R-11** | [Visión y alcance del MVP](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/product/vision-and-scope.md). | Describe contexto, alcance previsto, exclusiones y objetivos todavía no medidos. |
| **R-12** | [ADR-005: Continuidad de React/Vite para el MVP](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md). | Decisión aceptada sobre la continuidad de la tecnología frontend. |
| **R-13** | [ADR-006: SQLite para persistencia del MVP](https://github.com/agustinvallante/AgroFlow/blob/8873a2644b2fd427e571c832b04f5dfa83647a9a/docs/architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md). | Decisión de persistencia por etapa y destino previsto antes de operación con un cliente real. |

**Precedencia de fuentes.** Para describir comportamiento vigente prevalece OpenSpec (R-05), seguido por los contratos técnicos (R-06), las decisiones aceptadas (R-07) y la documentación explicativa del producto y dominio (R-08 a R-11), según la jerarquía acordada en ADR-002. R-01 determina la forma de la entrega académica; R-02 y R-03 sólo ilustran estructura. Una diferencia entre requisito normativo e implementación se registra como diferencia, no se resuelve suponiendo que el código modifica la norma.

### 1.5. Visión general del documento

La ERS se organiza según la estructura académica solicitada: el capítulo 1 presenta propósito, alcance, vocabulario, referencias y organización; el capítulo 2 describe contexto, funciones, usuarios, restricciones, dependencias y futuro; el capítulo 3 especifica requisitos funcionales, interfaces, rendimiento, restricciones de diseño, atributos de calidad y otros requisitos aplicables. La matriz de trazabilidad relaciona requisitos con fuentes normativas y, cuando existe, evidencia del corte funcional.

Este documento es un borrador completo para revisión académica, no una aprobación formal. En cada requisito se distinguen la autoridad normativa, la implementación observada, la evidencia estática disponible y las decisiones abiertas. La mención de pruebas existentes no afirma que se hayan ejecutado durante esta redacción; una capacidad no verificada no se presenta como probada.

## 2. Descripción general

### 2.1. Perspectiva del producto

AgroFlow se compone conceptualmente de una aplicación web para operación interna, una API backend y un flujo conversacional integrado. La API y su estado persistido son la autoridad operativa: frontend y n8n consumen sus resultados y no deben mantener reglas alternativas de prioridad, capacidad, autorización o transición. Este principio es normativo (R-05) y no implica que todos los controles del MVP estén implementados en el perfil LocalDemo.

En el corte observado, el backend está desarrollado sobre .NET 8 y la interfaz web con React, TypeScript y Vite. Los datos de negocio del perfil LocalDemo se persisten en SQLite. PostgreSQL y Redis son componentes auxiliares del canal y no deben confundirse con la base operativa de negocio descrita para ese perfil. La continuidad de React/Vite y las decisiones de persistencia se documentan en R-12 y R-13; esta descripción tecnológica registra el corte, no impone una nueva restricción ni afirma preparación para producción.

La interfaz web consume la API HTTP a través de rutas del mismo origen. El canal de mensajería conecta Evolution API con n8n y, desde allí, con la API de AgroFlow; n8n adapta mensajes y comunica respuestas, mientras el backend determina identidad, estado y resultado operativo. Hay componentes implementados de esta integración, pero su presencia no acredita que se haya verificado una conversación completa con WhatsApp y proveedores externos reales. Los detalles de las rutas, los mensajes y las condiciones de integración se especificarán en el capítulo 3 a partir de las fuentes citadas.

El alcance documentado es un único ingenio en el LocalDemo observado. La segregación por ingenio forma parte del comportamiento requerido para el MVP, pero no debe confundirse con una autenticación web individual ni con una demostración de aislamiento productivo en este corte. Las claves estáticas usadas por consumidores técnicos identifican integraciones, no usuarios humanos.

### 2.2. Funciones del producto

La siguiente síntesis separa las capacidades del producto de las superficies efectivamente conectadas en el corte. «Prevista/normativa» indica que la capacidad pertenece al alcance o a una especificación vigente; «observada» indica evidencia estática de implementación o conexión en la versión citada, no certificación de funcionamiento en producción.

| Área | Capacidad prevista o normativa | Superficie observada en el corte |
|---|---|---|
| Turnos | Solicitud, asignación, consulta, ciclo de vida y cancelación según las reglas de negocio vigentes. | La web consulta, filtra, muestra detalle, permite alta manual, transición y cancelación mediante la API. |
| Transportistas | Gestión de datos maestros conforme a permisos y validaciones especificados. | La web lista, busca, registra e inhabilita transportistas; la API permite edición, pero la interfaz observada no ofrece esa operación. |
| Fincas | Catálogo de fincas y uso como origen en solicitudes según el alcance aprobado. | Se observó una lectura del catálogo en la API; no se observó una interfaz operativa para gestionar fincas. |
| Conversaciones | Interacción del transportista mediante WhatsApp, con decisiones operativas delegadas al backend. | La web presenta conversaciones e historial persistidos. Existen componentes de integración n8n/Evolution, sin evidencia aquí de una prueba extremo a extremo con proveedores reales. |
| Panel operativo | Monitoreo de la operación, sujeto a las definiciones vigentes de indicadores y cola. | El panel muestra camiones en curso derivados de turnos. Métricas, cola visual, feed, molienda y avisos no tienen datos API en la vista observada. |
| Reportes y configuración | Funciones sujetas al alcance aprobado y a las decisiones vigentes. | Las vistas conservan presentación, pero no ofrecen datos operativos ni acciones habilitadas. No se documentan como funciones disponibles. |

No se atribuyen al sistema reportes o exportación, configuración operativa desde pantalla, gestión de usuarios, métricas de chatbot, avisos automáticos ni CRUD de camiones, fincas o asociaciones, salvo que evidencia posterior del corte lo respalde. Tampoco se infiere que una capacidad normativa sea funcionalmente completa por el solo hecho de figurar en la visión del MVP.

### 2.3. Características de los usuarios

Los perfiles siguientes describen actores de negocio y de integración en términos generales, sin datos personales. La disponibilidad de una interfaz o credencial en LocalDemo no demuestra que el sistema implemente toda la autenticación y autorización previstas.

| Perfil | Responsabilidad o interacción | Estado y límite relevante |
|---|---|---|
| Operador del ingenio | Atiende operaciones internas, datos maestros y turnos dentro del ámbito autorizado. | OpenSpec reconoce operaciones acordadas para este rol; el LocalDemo observado no ofrece inicio de sesión individual operativo. |
| Supervisor del ingenio | Supervisa y realiza las operaciones internas que le autoriza el alcance aprobado. | Los permisos confirmados son acotados; no se extrapolan facultades administrativas pendientes. |
| Transportista | Solicita o consulta turnos y comunica información operativa por el canal conversacional previsto. | La identificación y autorización dependen de las reglas del backend. La existencia de componentes de canal no acredita entrega real de extremo a extremo. |
| Administrador del sistema | Perfil pertinente para la administración general del producto. | Sus permisos específicos continúan abiertos; esta ERS no le asigna operaciones por inferencia. |
| Sistemas consumidores | Navegador, n8n y servicios de mensajería intercambian solicitudes y resultados con la API. | Las claves de servicio o del dashboard no son autenticación individual ni prueba de identidad de una persona. |

No se establecen perfiles de habilidad, formación, frecuencia de uso ni permisos distintos de los respaldados por las fuentes normativas. Los permisos aún no definidos se mantienen como límite de especificación, no como roles implícitos.

### 2.4. Restricciones

- **Autoridad de reglas:** el backend y su estado persistido son la fuente operativa; frontend y n8n no deben recalcular reglas ni confirmar cambios no persistidos (R-05).
- **Aislamiento requerido:** las operaciones con datos deben respetar el ámbito del ingenio en el comportamiento aprobado. La existencia de esta regla no acredita que LocalDemo implemente el aislamiento completo del MVP.
- **Perfil de datos observado:** LocalDemo utiliza SQLite para los datos de negocio en un único entorno de demostración. PostgreSQL es el destino previsto antes de la operación con un cliente real; no se afirma que esa migración ya esté desplegada. Redis, cuando interviene en el canal, no es la base de negocio.
- **Interfaz:** la aplicación web del corte se basa en React, TypeScript y Vite conforme a la decisión aceptada de continuidad tecnológica (R-12). Esta referencia describe la implementación existente, no una exigencia académica independiente.
- **Canales y credenciales:** las claves técnicas del perfil local protegen rutas de consumidores, pero no reemplazan autenticación individual ni autorizan a describir LocalDemo como el modelo de seguridad del MVP. La verificación del origen del webhook y la seguridad productiva de servicios externos tienen límites documentados.
- **No desarrollo:** esta ERS documenta el sistema realizado; no introduce cambios de aplicación ni convierte capacidades futuras en restricciones implementadas.

Las diferencias entre las reglas normativas y las limitaciones observadas se desarrollarán con sus referencias en el capítulo de requisitos específicos, sin reinterpretarlas como cambios aprobados.

### 2.5. Suposiciones y dependencias

La descripción de la solución depende de los contratos y especificaciones del corte citado; si una fuente explicativa contradice OpenSpec, se aplica la jerarquía documental de R-07 y la discrepancia debe quedar identificada. La interfaz web y el canal conversacional dependen de la disponibilidad y respuesta de la API para mostrar o confirmar operaciones. El flujo de WhatsApp depende además de Evolution, n8n y el proveedor de modelo configurado; esa dependencia no implica disponibilidad, entrega ni funcionamiento extremo a extremo comprobados.

El modo LocalDemo supone un único ingenio y persistencia de negocio local con SQLite. Las claves de servicio y del dashboard son configuración de integración, no identidades de operadores o transportistas. La consigna académica pide describir el sistema realizado; por ello, los objetivos de la visión se presentan como objetivos pendientes de medición cuando no exista evidencia de resultados.

Permanecen abiertos detalles que pueden afectar requisitos verificables: calendario de recepción y zona horaria; horizonte de búsqueda y límites de ventanas; formato, unidad y validaciones de corte y carga estimada; concurrencia y protección frente a asignaciones simultáneas; partes de la matriz de permisos; campos y operaciones de asociaciones; fórmulas e interpretación temporal de indicadores; y políticas de respaldo, recuperación y servicio. Se remite al registro de decisiones abiertas (R-10). Este documento no selecciona valores ni comportamientos para esos puntos.

### 2.6. Requisitos futuros, si corresponde

Las capacidades fuera del MVP declaradas por la visión del producto —geolocalización en tiempo real, ruteo cartográfico avanzado, polígonos de fincas/PostGIS, integración con ERP, analítica predictiva y obligación de actualización mediante WebSockets— no se describen como funciones actuales ni como requisitos comprometidos de esta ERS. Su eventual incorporación requiere una decisión y especificación explícitas.

PostgreSQL está previsto como destino de persistencia antes de operar con un cliente real, mientras que SQLite corresponde a la etapa académica de demostración; esto es una dirección por etapa, no evidencia de migración realizada. Otras capacidades de la visión del MVP que no estén conectadas en el corte se mantienen diferenciadas como alcance normativo con implementación parcial, no observada o pendiente de verificación.

Las decisiones todavía abiertas —incluidos permisos restantes, detalles de calendario, capacidad y concurrencia, indicadores, seguridad productiva, conservación de datos y recuperación— no se elevan a requisitos futuros hasta que exista una resolución formal. La evaluación de esas opciones queda fuera de esta descripción y no constituye una promesa de producto.

## 3. Requisitos específicos

### 3.1. Requisitos funcionales

Los requisitos siguientes describen operaciones HTTP observadas estáticamente en el perfil **LocalDemo** del corte R-04 y contrastadas con el contrato R-06. No equivalen a la cobertura funcional completa del MVP normativo. Las rutas de turnos son anónimas únicamente bajo LocalDemo; transportistas requieren la clave del dashboard; la consulta de fincas admite las claves de servicio o dashboard según el perfil. Estas claves autentican consumidores técnicos, no usuarios individuales. Los códigos y validaciones indicados son los publicados por el contrato y la implementación; no se afirma que sus pruebas se hayan ejecutado durante esta redacción.

#### RF-001 — Crear y asignar un turno

**Actor y canal.** Cliente HTTP de dashboard o integración que invoque `POST /api/v1/appointments`; en LocalDemo la ruta no exige autenticación individual.

**Entrada y condiciones.** Cuerpo JSON con `carrierPhone`, `truckPlate`, `farmCode`, `cutAt` y `estimatedLoadTons`. El teléfono debe tener formato E.164; la patente, entre 6 y 10 caracteres; el código de finca, entre 1 y 50 caracteres permitidos; `cutAt` debe ser fecha y hora ISO 8601 con desplazamiento explícito, y `estimatedLoadTons` un número finito mayor que cero. No se envía UUID de recursos ni ventana preferida. Las referencias deben resolverse y existir una asociación activa transportista-camión.

**Resultado y errores.** Responde `201 Created` con el turno persistido en estado `ASIGNADO`, ventana asignada y encabezado `Location`. Errores de formato o validación responden `400`; referencias ausentes o inactivas y asociación no encontrada, `404`; conflicto de negocio o falta de capacidad, `409`. El asignador selecciona la primera ventana futura compatible con cupo; esto no demuestra la regla normativa completa de prioridad entre solicitudes competidoras.

**Alcance y evidencia.** Operación de LocalDemo, sin autenticación individual. R-04: `backend/AgroFlow.Api/Controllers/AppointmentsController.cs`, `backend/AgroFlow.Api/Contracts/Appointments/AppointmentHttpModels.cs`, `backend/AgroFlow.Application/Appointments/AppointmentService.cs`; pruebas existentes `AppointmentsHttpTests.Post_creates_an_assigned_appointment_with_location_and_canonical_fields` y `AppointmentServiceTests.Create_selects_the_first_future_window_that_has_capacity` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-002 — Listar y filtrar turnos

**Actor y canal.** Cliente HTTP de dashboard o integración mediante `GET /api/v1/appointments`; anónimo únicamente en LocalDemo.

**Entrada y condiciones.** Admite `date` en formato `yyyy-MM-dd`, `status` reconocido, `truckPlate` de 6 a 10 caracteres, `phone` E.164 y `active=true`. Rechaza claves de filtro desconocidas, valores vacíos o repetidos y cualquier valor de `active` distinto de `true`. Todos los filtros provistos se aplican a la consulta; `active` no admite filtrar por inactivos. Si no se indica fecha, el comportamiento de fecha local queda a cargo del almacén y `active=true` permite consultar activos sin fecha.

**Resultado y errores.** Responde `200 OK` con una lista de resúmenes ordenados; sin coincidencias devuelve una lista vacía. Filtros no admitidos o inválidos responden `400`; un fallo interno, `500`.

**Alcance y evidencia.** Consulta HTTP de LocalDemo, sin autenticación individual. R-04: `backend/AgroFlow.Api/Controllers/AppointmentsController.cs` y contrato de consulta en `backend/AgroFlow.Api/Contracts/Appointments/AppointmentHttpModels.cs`; pruebas existentes `AppointmentsHttpTests.Get_list_parses_combined_filters_and_returns_summaries`, `AppointmentsHttpTests.Get_list_with_phone_but_no_date_leaves_local_today_to_the_store` y `AppointmentsHttpTests.Get_list_with_active_true_requests_active_turnos_without_a_date` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-003 — Consultar el detalle vigente de un turno

**Actor y canal.** Cliente HTTP de dashboard o integración mediante `GET /api/v1/appointments/{id}`; anónimo únicamente en LocalDemo.

**Entrada y condiciones.** `id` debe ser el UUID de un turno.

**Resultado y errores.** Responde `200 OK` con el estado vigente y los datos devueltos para transportista, camión, finca, corte, carga estimada, ventana y creación. Un identificador que no sea UUID válido responde `400`; un turno inexistente, `404`; un error interno, `500`.

**Alcance y evidencia.** Lectura del recurso persistido en LocalDemo, sin autenticación individual. R-04: `backend/AgroFlow.Api/Controllers/AppointmentsController.cs` y `backend/AgroFlow.Api/Contracts/Appointments/AppointmentHttpModels.cs`; prueba existente `AppointmentsHttpTests.Get_detail_returns_the_current_appointment` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-004 — Avanzar o cancelar el estado de un turno

**Actor y canal.** Cliente HTTP de dashboard o integración mediante `POST /api/v1/appointments/{id}/transitions`; anónimo únicamente en LocalDemo.

**Entrada y condiciones.** UUID de turno y cuerpo JSON con `newStatus` reconocido. La operación solicita el siguiente estado permitido de la secuencia o `CANCELADO` cuando el estado actual admite cancelación; no permite saltar transiciones.

**Resultado y errores.** Responde `200 OK` sólo después de persistir el nuevo estado. UUID o estado/cuerpo inválido responde `400`; turno inexistente, `404`; transición no permitida o conflicto con otro cambio de estado, `409`; error interno, `500`.

**Alcance y evidencia.** El servicio local aplica la secuencia observada; el contrato no implica que se haya demostrado aquí toda autorización normativa por rol. R-04: `backend/AgroFlow.Api/Controllers/AppointmentsController.cs`, `backend/AgroFlow.Application/Appointments/AppointmentService.cs`; pruebas existentes `AppointmentsHttpTests.Post_transition_returns_the_persisted_state`, `AppointmentServiceTests.Transition_accepts_only_the_immediate_sequence_and_persists_each_step` y `AppointmentServiceTests.Concurrent_state_change_is_reported_as_conflict` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-005 — Listar transportistas

**Actor y canal.** Consumidor del dashboard mediante `GET /api/v1/drivers`, autenticado con la clave de dashboard de LocalDemo.

**Entrada y condiciones.** Filtro opcional `active`, que admite exactamente `true` o `false`; sin filtro se consultan transportistas de ambos estados. No es una autenticación de operador individual.

**Resultado y errores.** Responde `200 OK` con transportistas ordenados por nombre; la respuesta incluye estado y patentes de asociaciones activas devueltas por el servicio, además de un indicador derivado de si puede solicitar turnos. Filtro inválido responde `400`; ausencia o invalidez de la clave, `401`; error interno, `500`.

**Alcance y evidencia.** API de LocalDemo; la clave del dashboard no representa roles de usuario ni autorización productiva por ingenio. R-04: `backend/AgroFlow.Api/Controllers/DriversController.cs` y `backend/AgroFlow.Api/Contracts/Drivers/DriverHttpModels.cs`; prueba existente `DriverLifecycleHttpTests.Editing_a_seeded_whatsapp_survives_restarts_without_duplicates_and_keeps_its_truck` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-006 — Consultar un transportista

**Actor y canal.** Consumidor del dashboard mediante `GET /api/v1/drivers/{id}`, autenticado con la clave de dashboard de LocalDemo.

**Entrada y condiciones.** `id` debe ser el UUID del transportista.

**Resultado y errores.** Responde `200 OK` con sus datos, estado y patentes activas; el indicador de posibilidad de solicitar turnos depende de que esté activo y tenga al menos una asociación/camión activo. UUID inválido responde `400`; recurso inexistente, `404`; clave ausente o inválida, `401`; error interno, `500`.

**Alcance y evidencia.** Consulta HTTP de LocalDemo. R-04: `backend/AgroFlow.Api/Controllers/DriversController.cs`, en particular la construcción de la respuesta a partir de asociaciones activas; prueba existente `DriverLifecycleHttpTests.A_driver_registered_by_the_api_needs_an_active_truck_association_to_request_appointments` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-007 — Registrar un transportista

**Actor y canal.** Consumidor del dashboard mediante `POST /api/v1/drivers`, autenticado con la clave de dashboard de LocalDemo.

**Entrada y condiciones.** Requiere encabezado `Idempotency-Key` (hasta 200 caracteres) y cuerpo JSON con `name`, `dni` y `whatsApp`. El nombre debe tener entre 1 y 150 caracteres; el DNI, hasta 20 caracteres y al menos un dígito; `whatsApp` debe ser E.164. La identidad y el WhatsApp tienen restricciones de unicidad en la implementación. El alta no crea camión ni asociación; sin asociación activa el transportista no puede solicitar turnos.

**Resultado y errores.** Responde `201 Created` con recibo de la mutación; repetición de la misma clave y datos devuelve el recibo original. Entrada inválida o clave faltante responde `400`; clave de dashboard inválida/ausente, `401`; duplicado o conflicto por reutilizar clave con datos distintos, `409`; error interno, `500`.

**Alcance y evidencia.** Alta HTTP de LocalDemo, no CRUD integral de datos maestros. La idempotencia aplica a esta mutación y no implica idempotencia global de turnos. R-04: `backend/AgroFlow.Api/Controllers/DriversController.cs` y `backend/AgroFlow.Api/Contracts/Drivers/DriverHttpModels.cs`; prueba existente `DriverLifecycleHttpTests.A_driver_registered_by_the_api_needs_an_active_truck_association_to_request_appointments` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-008 — Editar un transportista mediante la API

**Actor y canal.** Consumidor del dashboard mediante `PUT /api/v1/drivers/{id}`, autenticado con la clave de dashboard de LocalDemo.

**Entrada y condiciones.** UUID, encabezado `Idempotency-Key` y cuerpo JSON con `name`, `dni` y `whatsApp`, con las mismas validaciones de formato y unicidad del alta. La operación HTTP no equivale a una función de edición disponible en la interfaz web observada.

**Resultado y errores.** Responde `200 OK` con recibo de edición. UUID inválido, campos inválidos o clave ausente responden `400`; transportista inexistente, `404`; clave de dashboard inválida/ausente, `401`; duplicado o conflicto de idempotencia, `409`; error interno, `500`.

**Alcance y evidencia.** Edición disponible en API de LocalDemo; no se presenta como edición desde pantalla ni como autorización individual. R-04: `backend/AgroFlow.Api/Controllers/DriversController.cs`; prueba existente `DriverLifecycleHttpTests.Editing_a_seeded_whatsapp_survives_restarts_without_duplicates_and_keeps_its_truck` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-009 — Inhabilitar un transportista

**Actor y canal.** Consumidor del dashboard mediante `POST /api/v1/drivers/{id}/inactivation`, autenticado con la clave de dashboard de LocalDemo.

**Entrada y condiciones.** Requiere el UUID del transportista y el encabezado `Idempotency-Key`, de hasta 200 caracteres. La operación cambia su condición de actividad; no elimina el registro, su historial ni crea o modifica asociaciones con camiones.

**Resultado y errores.** Responde `200 OK` con el recibo de la mutación. UUID inválido o clave de idempotencia faltante o inválida responden `400`; transportista inexistente, `404`; clave de dashboard ausente o inválida, `401`; conflicto de datos o de reutilización de la clave, `409`; error interno, `500`. Un transportista inactivo no puede solicitar nuevos turnos, incluso si tiene un camión asociado.

**Alcance y evidencia.** Inhabilitación disponible en la API de LocalDemo; no equivale a borrado físico ni a una operación disponible en una interfaz administrativa general. La clave técnica no identifica a una persona ni acredita autorización productiva por ingenio. R-04: `backend/AgroFlow.Api/Controllers/DriversController.cs` y `backend/AgroFlow.Api/Contracts/Drivers/DriverHttpModels.cs`; prueba existente `DriverLifecycleHttpTests.An_inactivated_driver_cannot_request_appointments_even_with_a_truck` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-010 — Consultar el catálogo de fincas

**Actor y canal.** Consumidor autorizado del catálogo —dashboard o servicio de canal— mediante `GET /api/v1/farms`, con la clave técnica admitida por la política FarmCatalogRead de LocalDemo.

**Entrada y condiciones.** Puede indicar `active=true|false` y `search` para buscar por nombre o código; la búsqueda respeta el límite de longitud definido por el contrato. Se rechazan parámetros desconocidos y valores inválidos. La consulta sólo presenta fincas ofrecibles o coincidentes; no administra sus datos.

**Resultado y errores.** Responde `200 OK` con una lista de fincas que contiene identificador, código, nombre y estado; sin coincidencias devuelve una lista vacía. Filtros inválidos responden `400`; clave ausente o inválida, `401`; fallo interno, `500`.

**Alcance y evidencia.** Lectura de catálogo en LocalDemo: no hay en este requisito interfaz de gestión ni operaciones de alta, edición o inhabilitación de fincas. Las claves identifican consumidores técnicos, no personas ni autorización multiingenio productiva. R-04: `backend/AgroFlow.Api/Controllers/FarmsController.cs` y `backend/AgroFlow.Api/Contracts/Farms/FarmHttpModels.cs`; pruebas existentes `FarmsHttpTests.Exact_and_natural_names_return_the_single_seeded_farm` y `FarmsHttpTests.An_unknown_name_returns_an_empty_list_and_an_inactive_farm_is_not_offered` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-011 — Registrar un mensaje entrante del canal

**Actor y canal.** Servicio de integración —n8n en el flujo observado— mediante `POST /api/v1/conversation-events`, autenticado con la clave de servicio de LocalDemo.

**Entrada y condiciones.** El cuerpo identifica proveedor, instancia registrada, ID de mensaje del proveedor, dirección remota y texto; `contentType` debe ser `text`. `providerTimestamp` es opcional y, si se informa, debe ser fecha y hora ISO 8601 con desplazamiento explícito. El ingenio asociado se obtiene de la instancia registrada, nunca de un valor enviado en el cuerpo. El proveedor y la instancia deben corresponder a una configuración admitida.

**Resultado y errores.** El primer registro persistido responde `201 Created` con disposición `process` y los identificadores y datos de autorización de procesamiento que devuelve la API. La repetición deduplicada responde `200 OK` con disposición `duplicate_stop`, sin volver a autorizar el procesamiento. Cuerpo, tipo o fecha inválidos responden `400`; instancia no habilitada, `403`; mismo mensaje ya registrado con otro contenido, `409`; clave de servicio ausente o inválida, `401`; error interno, `500`.

**Alcance y evidencia.** El requisito cubre la recepción y persistencia del evento por la API, no una garantía de entrega extremo a extremo por WhatsApp, n8n o un proveedor externo, ni que el mensaje haya sido procesado o respondido. Las claves del canal son credenciales de consumidores técnicos, no identidad individual. R-04: `backend/AgroFlow.Api/Controllers/ChannelController.cs` y `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs`; pruebas existentes `ConversationsHttpTests.Full_channel_flow_registers_deduplicates_replies_and_is_visible_to_the_dashboard` y `ConversationStoreTests.First_message_creates_conversation_message_and_a_single_hashed_authorization` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-012 — Informar un fallo de procesamiento

**Actor y canal.** Servicio de integración autenticado con la clave de servicio de LocalDemo, mediante `PATCH /api/v1/conversation-events/{messageId}/processing`. La operación sólo puede completarla quien conserva el `claimToken` vigente que autoriza el procesamiento de ese mensaje.

**Entrada y condiciones.** `messageId` debe ser un UUID y el cuerpo debe incluir `claimToken`, `status: failed` y un `errorCode` del catálogo cerrado `AGENT_ERROR`, `REPLY_NOT_SENT` o `WORKFLOW_ERROR`. El mensaje debe seguir en estado de procesamiento y la autorización debe pertenecer a quien reclama el mensaje; el token no sustituye la clave de servicio.

**Resultado y errores.** Si la validación de estado y titularidad es correcta, responde `200 OK` con el mensaje marcado `FAILED` y el código informado. UUID, cuerpo, estado distinto de `failed` o código fuera del catálogo responden `400`; clave de servicio inválida o ausente, `401`; mensaje inexistente, `404`; autorización inválida, vencida o ajena, o mensaje en estado incompatible, `409`; error interno, `500`. Este reporte describe el estado de procesamiento del mensaje entrante y no el estado de envío de una respuesta.

**Alcance y evidencia.** Operación de procesamiento del canal en LocalDemo; no informa por sí misma un resultado de envío ni garantiza que una respuesta haya sido entregada. R-04: `backend/AgroFlow.Api/Controllers/ChannelController.cs`, `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs` y el catálogo cerrado de `backend/AgroFlow.Domain/Entities/MessageProcessing.cs`; prueba existente `ConversationsHttpTests.Processing_failure_route_accepts_only_the_catalog_and_the_holder_token` (no ejecutada aquí). Fuentes: R-05, R-06, R-09.

#### RF-013 — Registrar la intención de respuesta a un mensaje entrante

**Actor y canal.** Servicio de integración —n8n en el flujo observado— mediante `POST /api/v1/conversation-reply-intents`, autenticado con la clave de servicio de LocalDemo. Para completar la operación debe presentar el `claimToken` vigente del procesamiento del mensaje entrante.

**Entrada y condiciones.** El cuerpo contiene `inboundMessageId` (UUID), `claimToken` y `text` no vacío de hasta 4096 caracteres. El mensaje debe existir; una intención nueva requiere una autorización de procesamiento vigente. Una repetición debe corresponder al titular del token y conservar el mismo texto. El token identifica la autorización de ese mensaje; no reemplaza la clave de servicio.

**Resultado y errores.** La API persiste una única intención por mensaje entrante y devuelve `201 Created` para la nueva intención, con estado `PENDING_SEND`; una repetición del titular con el mismo texto devuelve `200 OK` con la intención existente. Texto, UUID o token mal formados responden `400`; clave de servicio ausente o inválida, `401`; instancia de canal no habilitada, `403`; mensaje inexistente, `404`; autorización inválida o vencida para una intención nueva, o repetición con texto distinto, `409`; error interno, `500`. La respuesta es una intención persistida para que la integración continúe el flujo, no evidencia de envío al proveedor.

**Alcance y evidencia.** Capacidad de API de LocalDemo; la unicidad por mensaje no equivale a idempotencia global de turnos ni garantiza entrega o lectura de la respuesta. R-04: `backend/AgroFlow.Api/Controllers/ChannelController.cs`, `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs`, `backend/AgroFlow.Application/Conversations/ConversationService.cs` y `backend/AgroFlow.Data/Appointments/EfConversationStore.cs`; pruebas existentes `ConversationsHttpTests.Full_channel_flow_registers_deduplicates_replies_and_is_visible_to_the_dashboard` y `ConversationStoreTests.Reply_intent_requires_the_holder_token_and_is_unique_per_inbound_message` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-014 — Informar el resultado de envío de una respuesta

**Actor y canal.** Servicio de integración autenticado con la clave de servicio de LocalDemo, mediante `PATCH /api/v1/conversation-reply-intents/{replyId}/send-status`.

**Entrada y condiciones.** Requiere el UUID de la intención y `status` igual a `accepted`, `failed` o `unknown`. `providerMessageId` es opcional y sólo se admite con `accepted`; si se informa, debe ser un identificador válido de hasta 128 caracteres. El estado reportado debe ser una transición permitida desde el estado actual.

**Resultado y errores.** Responde `200 OK` con la intención y el estado de envío actualizado; una repetición del mismo estado sin cambios incompatibles no modifica el resultado. Desde `PENDING_SEND` se admiten `ACCEPTED`, `FAILED` o `UNKNOWN`; desde `UNKNOWN` se puede resolver a `ACCEPTED` o `FAILED`. UUID, cuerpo, estado o identificador inválidos responden `400`; clave de servicio ausente o inválida, `401`; intención inexistente, `404`; transición no permitida, `409`; error interno, `500`. `ACCEPTED` indica aceptación reportada por el proveedor, no entrega ni lectura confirmadas; `UNKNOWN` se refleja como necesidad de atención hasta su resolución.

**Alcance y evidencia.** Reporte de estado de envío de la respuesta en LocalDemo, distinto del estado de procesamiento del mensaje entrante. No acredita funcionamiento de un proveedor externo ni entrega extremo a extremo. R-04: `backend/AgroFlow.Api/Controllers/ChannelController.cs`, `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs`, `backend/AgroFlow.Application/Conversations/ConversationService.cs` y `backend/AgroFlow.Domain/Entities/ConversationMessage.cs`; pruebas existentes `ConversationStoreTests.Accepted_send_status_completes_processing_and_rejects_later_failure` y `ConversationStoreTests.Unknown_send_result_needs_attention_until_it_is_resolved` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-015 — Listar conversaciones

**Actor y canal.** Consumidor del dashboard mediante `GET /api/v1/conversations`, autenticado con la clave de dashboard de LocalDemo. El backend obtiene el ingenio del contexto del consumidor; no lo toma de un identificador enviado por el cliente ni interpreta la clave como un rol de usuario.

**Entrada y condiciones.** Admite paginación opcional con `cursor` y `limit`; el límite predeterminado es 20 y el admitido va de 1 a 50. El cursor debe tener el formato opaco emitido por la API. El backend limita los resultados al ingenio de su contexto y los ordena por actividad más reciente, con desempate estable; el cursor permite continuar la página.

**Resultado y errores.** Responde `200 OK` con una página de resúmenes y un cursor siguiente cuando corresponda. Parámetros desconocidos, límite inválido o cursor inválido responden `400`; clave de dashboard ausente o inválida, `401`; error interno, `500`.

**Alcance y evidencia.** Consulta paginada de LocalDemo; la paginación de la API no implica que la interfaz web cargue automáticamente todo el historial. El contexto de ingenio del perfil no demuestra autenticación individual ni aislamiento multiingenio productivo. R-04: `backend/AgroFlow.Api/Controllers/ConversationsController.cs`, `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs`, `backend/AgroFlow.Application/Conversations/ConversationService.cs` y `backend/AgroFlow.Data/Appointments/EfConversationStore.cs`; pruebas existentes `ConversationStoreTests.Listing_is_scoped_to_the_ingenio_and_ordered_by_latest_activity` y `Conversation_pages_break_activity_ties_without_repeating_or_skipping` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

#### RF-016 — Consultar una conversación y paginar sus mensajes

**Actor y canal.** Consumidor del dashboard, autenticado con la clave de dashboard de LocalDemo, mediante `GET /api/v1/conversations/{id}` para los metadatos de la conversación y `GET /api/v1/conversations/{id}/messages` para sus mensajes.

**Entrada y condiciones.** `{id}` debe ser el UUID de una conversación del ingenio asociado al contexto del consumidor. La consulta de mensajes admite `cursor` y `limit` con el mismo límite predeterminado de 20 y rango de 1 a 50 que la lista; el cursor debe ser válido y estar anclado en un mensaje de esa conversación. No se admiten parámetros de consulta en el detalle de conversación.

**Resultado y errores.** Ambas rutas responden `200 OK` con los datos disponibles; la consulta de mensajes devuelve una página y cursor siguiente cuando corresponda. El detalle incluye metadatos de actividad; cada mensaje expone dirección, texto, marcas temporales del proveedor (si existen) y de persistencia, referencia de respuesta y estados de procesamiento o envío cuando aplican. UUID o parámetros inválidos y cursor inválido responden `400`; clave de dashboard ausente o inválida, `401`; conversación ausente o ajena al ingenio del contexto, `404`; error interno, `500`.

**Alcance y evidencia.** Las consultas son de sólo lectura y están acotadas al contexto de LocalDemo; el `404` no permite distinguir entre conversación inexistente y fuera de ese contexto. Los estados expuestos no certifican entrega o lectura ni demuestran aislamiento productivo multiingenio. La paginación de la API no garantiza que la interfaz cargue todos los mensajes. R-04: `backend/AgroFlow.Api/Controllers/ConversationsController.cs`, `backend/AgroFlow.Api/Contracts/Conversations/ConversationHttpModels.cs` y `backend/AgroFlow.Application/Conversations/ConversationService.cs`; pruebas existentes `ConversationsHttpTests.Unknown_conversation_is_not_found`, `ConversationStoreTests.Message_pages_never_repeat_or_skip_rows_while_messages_arrive` y `ConversationStoreTests.Manipulated_message_cursor_is_a_validation_error` (no ejecutadas aquí). Fuentes: R-05, R-06, R-09.

### 3.2. Requisitos de interfaces externas

Esta sección describe contratos e interfaces que delimitan la interacción. Las rutas y formatos de API corresponden al contrato R-06 y a LocalDemo; no implican autenticación individual, disponibilidad productiva ni una prueba extremo a extremo.

#### IU-01 — Interfaz web de operación

La aplicación web del corte usa React/TypeScript y consume la API HTTP de mismo origen. Presenta listados y detalle de turnos, un formulario de alta manual y acciones de transición y cancelación conectadas a RF-001–RF-004; para transportistas permite listar y buscar, registrar e inhabilitar (RF-005, RF-007 y RF-009), pero no ofrece edición desde pantalla (RF-008) ni una consulta individual dedicada (RF-006); presenta conversaciones e historial según RF-015–RF-016. La API es autoridad del estado y de las reglas; la interfaz no debe confirmar una mutación hasta recibir respuesta de persistencia. Paneles o vistas que sólo muestran presentación sin datos/acciones API no forman parte de la funcionalidad observada. La especificación de accesibilidad visual, navegador soportado y autenticación de usuario permanece fuera de lo demostrado por este corte.

#### IU-02 — API HTTP

La API expone recursos JSON versionados bajo `/api/v1/`, con métodos, parámetros, esquemas y respuestas definidos en OpenAPI (R-06). Las operaciones funcionales observadas se detallan en RF-001–RF-016. El contrato especifica códigos HTTP y errores de validación, autorización, ausencia de recurso y conflictos por operación. Las consultas no alteran estado; las mutaciones sólo se confirman después de persistir. El perfil LocalDemo deja algunas rutas anónimas y protege otras con claves técnicas de dashboard o servicio; estas claves no equivalen a cuentas o roles personales ni describen la seguridad prevista del MVP.

#### IU-03 — Integración conversacional

El flujo previsto conecta WhatsApp con Evolution API, n8n y la API AgroFlow. El contrato de integración comprende eventos entrantes, tokens de autorización de procesamiento, intenciones de respuesta y reporte de estado de envío (RF-011–RF-014). La identidad, las reglas de negocio y el estado operativo pertenecen al backend; n8n adapta mensajes y comunica únicamente resultados confirmados por la API. El corte contiene componentes de esta integración, pero no se afirma entrega real de WhatsApp, recepción completa con proveedor externo ni confirmación de lectura. Una aceptación del proveedor no es evidencia de entrega.

#### IU-04 — Persistencia

En LocalDemo, el backend persiste el estado AgroFlow en SQLite y expone los resultados mediante la API. La base es un detalle interno, no una interfaz directa para la web o n8n. ADR-006 identifica PostgreSQL como destino antes de operar con un cliente real, sujeto a migración y validación; no se afirma que tal transición haya ocurrido. La ERS no define formatos de archivo, respaldo/retención operativos ni acceso directo de usuarios a la base.

### 3.3. Requisitos de rendimiento

No se identificó en las fuentes vigentes un objetivo cuantitativo aprobado de latencia, capacidad, concurrencia, disponibilidad o tiempo de recuperación para estas interfaces. Tampoco se dispone aquí de mediciones representativas de producción; por ello no se establece SLA, porcentaje de disponibilidad ni umbral de respuesta inventado.

#### RNF-001 — Confirmación posterior a persistencia

Toda operación mutante que comunique éxito debe haber persistido el cambio en el backend; ante fallo de persistencia, no debe exponer el cambio como vigente. Es una condición funcional/de consistencia verificable por escenario, no un objetivo de tiempo. **Autoridad:** OpenSpec, plataforma-y-segregacion y ADR-003 (R-05, R-07). **Implementación observada:** LocalDemo descrito en RF-001, RF-004 y RF-007–RF-014; el cumplimiento normativo completo no se infiere de la presencia del código.

#### RNF-002 — Consulta sin efectos operativos

Las consultas de turnos, catálogo y conversaciones no deben alterar los datos consultados. La API paginada de conversaciones acepta `limit` de 1 a 50, con valor predeterminado 20, según el contrato observado; esos límites no definen un objetivo de rendimiento. **Autoridad/evidencia:** OpenSpec turnos-consulta y contrato R-05/R-06; RF-002, RF-003, RF-010, RF-015 y RF-016.

#### RNF-003 — Límites de concurrencia y rendimiento no cuantificados

La decisión de SQLite identifica un único escritor simultáneo y limita el perfil académico a un host/archivo local; WAL no se considera activado sin validación. Estas son restricciones del motor y de la decisión arquitectónica, no pruebas de rendimiento de AgroFlow. La carga concurrente sostenible, latencia bajo carga y escalabilidad no están medidas. Antes de una operación real deben validarse conforme a ADR-006 y definirse objetivos con el entorno y la carga esperada (R-13).

**Pendiente de decisión y medición:** acordar objetivos de tiempo de respuesta, volumen y concurrencia, disponibilidad, recuperación y condiciones de medición antes de tratarlos como criterios de aceptación. No hay SLA comprometido en esta ERS.

### 3.4. Restricciones de diseño

Las restricciones siguientes provienen de OpenSpec o ADR aceptados; no convierten decisiones abiertas de OD-004–OD-007 y OD-012 en valores definidos.

| ID | Restricción | Aplicación y estado |
|---|---|---|
| RD-001 | El backend y el estado persistido son la única autoridad operativa para identidad efectiva, pertenencia, disponibilidad, asignación, prioridad, transiciones y persistencia. | Normativa: OpenSpec plataforma-y-segregacion y ADR-003 (R-05, R-07). Frontend y n8n presentan/invocan resultados, no mantienen reglas paralelas. |
| RD-002 | El aislamiento por ingenio debe validarse en el backend para cada operación con datos; no basta filtrar u ocultar información en frontend. | Normativa: plataforma-y-segregacion y monitoreo-operativo (R-05). LocalDemo no demuestra el aislamiento multiingenio ni autenticación individual completos. |
| RD-003 | La interfaz no debe solicitar una ventana preferida en el MVP; la asignación corresponde al backend y debe considerar las políticas aprobadas. | Normativa: solicitud-y-asignacion y ADR-004 (R-05, R-07). Los detalles calendario, zona horaria y horizonte siguen abiertos; RF-001 describe el comportamiento LocalDemo observado, no sustituye la política normativa. |
| RD-004 | El frontend debe representar la cola y los indicadores recibidos de la API, sin recalcular cupos como una regla independiente; no se exige WebSocket para el MVP. | Normativa: monitoreo-operativo (R-05). En el corte, parte del panel carece de datos API, por lo que la función no se presenta como completa. |
| RD-005 | Persistencia LocalDemo: SQLite; PostgreSQL es destino acordado antes del primer cliente real y requiere transición validada, no cambio de cadena de conexión solamente. | ADR-006 (R-13). No se afirma que la migración esté realizada. |
| RD-006 | React/Vite continúa como tecnología de frontend del MVP. | Decisión aceptada ADR-005 (R-12), descriptiva del stack acordado; no prescribe una nueva implementación. |

### 3.5. Atributos de calidad

Los requisitos de esta sección se limitan a propiedades respaldadas por normas o contratos. Los objetivos sin umbral aprobado se identifican como asuntos a medir, no como garantías alcanzadas.

#### RNF-004 — Consistencia de estado

El estado vigente comunicado debe derivar del backend; una consulta no debe modificarlo y una mutación no debe confirmarse antes de persistir. **Criterio verificable:** consultar repetidamente sin cambios concurrentes no produce mutaciones; una persistencia fallida no confirma la operación. **Fuente:** OpenSpec turnos-consulta, turnos-ciclo-de-vida y plataforma-y-segregacion; R-05, R-06.

#### RNF-005 — Segregación de datos

Cada operación debe limitarse al ingenio autorizado y no revelar ni modificar recursos de otro ingenio. **Criterio normativo:** solicitudes sobre recursos ajenos son rechazadas sin exposición; el diseño debe aplicar la comprobación en backend. **Estado observado:** LocalDemo y sus claves técnicas no demuestran autenticación individual ni aislamiento productivo multiingenio. **Fuente:** OpenSpec plataforma-y-segregacion y monitoreo-operativo; R-05.

#### RNF-006 — Validación y errores accionables

La API debe validar entradas incluso si el consumidor ya realizó validación, responder con resultados estables para entradas inválidas o conflictos y no incluir secretos ni información interna innecesaria en errores. Los códigos concretos de cada operación se enumeran en RF-001–RF-016 conforme al contrato. **Fuente:** ADR-003 y OpenAPI, R-06/R-07; el detalle del contrato observado no equivale a una evaluación de seguridad independiente.

#### RNF-007 — Trazabilidad de mensajes e idempotencia acotada

La integración debe correlacionar el evento entrante con su procesamiento e intención de respuesta; el contrato deduplica un mensaje repetido y limita a una intención por evento entrante. Una aceptación reportada por el proveedor no debe describirse como entrega o lectura. **Criterio verificable:** conservar las identidades de mensaje/intención y distinguir estados `PENDING_SEND`, `ACCEPTED`, `FAILED` y `UNKNOWN` cuando apliquen. **Fuente:** contrato R-06 y RF-011–RF-014; alcance de entrega real no verificado.

#### RNF-008 — Mantenibilidad de la autoridad documental

Los cambios de comportamiento se expresan en OpenSpec por capacidad, con contrato y pruebas correspondientes; esta ERS es descripción académica trazable y no una fuente paralela normativa. **Criterio verificable:** requisitos de esta ERS enlazan con la fuente vigente y las discrepancias entre especificación e implementación se muestran como tales. **Fuente:** ADR-002 (R-07) y matriz de trazabilidad de esta ERS.

#### RNF-009 — Usabilidad y accesibilidad: sin umbral establecido

No se encontraron criterios de usabilidad, accesibilidad, navegadores admitidos o pruebas de experiencia que permitan formular un requisito cuantificable para el corte. La existencia de una interfaz web no acredita conformidad con una norma de accesibilidad ni facilidad de uso medida. Estos criterios requieren acuerdo y evaluación posterior.

#### RNF-010 — Disponibilidad, respaldo y recuperación: sin garantía demostrada

ADR-006 describe requisitos de validación para respaldo/restauración y transición de persistencia; no se reporta aquí un ejercicio completado ni un tiempo objetivo de recuperación. No se establece porcentaje de disponibilidad, RPO o RTO. Antes de uso con datos reales se deben completar las validaciones y procedimientos previstos por las fuentes aprobadas; esta ERS no certifica su cumplimiento.

### 3.6. Otros requisitos aplicables

#### OR-001 — Reglas de dominio y conservación

Las solicitudes requieren transportista, camión, finca, momento de corte y carga estimada, sin ventana preferida; la asignación y prioridad siguen las reglas vigentes de OpenSpec y ADR-004. Cancelaciones, transiciones y referencias históricas respetan el ciclo de vida normativo. Las unidades y límites aún abiertos no se completan por inferencia. **Fuentes:** R-05, R-07, R-08 y R-10.

#### OR-002 — Protección de información y material académico

Los datos personales, secretos y credenciales no deben agregarse a esta publicación. Las claves LocalDemo mencionadas son tipos de credencial, nunca valores. Los PDF fuente de la asignatura se mantienen privados; esta ERS usa títulos genéricos y referencias no identificatorias. Este criterio editorial no equivale a certificación legal o de privacidad del producto.

#### OR-003 — Datos y configuración de demostración

El comportamiento observado se limita al perfil LocalDemo y al corte citado. Seeds, configuración y datos de prueba no se describen como registros reales de clientes. El cambio a operación real requiere completar identidad, segregación, persistencia destino y validaciones de operación conforme a las decisiones aprobadas; esta ERS no declara esas tareas realizadas.

#### OR-004 — Casos de uso y cobertura

Los RF-001–RF-016 son requisitos de la superficie HTTP observada y se vinculan con los casos de uso correspondientes en R-09. No representan por sí solos la cobertura total de los casos de uso normativos. Las funciones de interrupciones, indicadores completos, gestión de asociaciones/camiones/fincas y autorización individual no se elevan a capacidades operativas del corte sin evidencia específica; sus requisitos normativos permanecen en OpenSpec.

### Matriz de trazabilidad

La matriz distingue la fuente normativa (qué comportamiento debe regir) de la evidencia del código/contrato (qué se observó implementado en LocalDemo). La referencia al código es inspección estática y las pruebas citadas en cada RF son nombres de pruebas existentes, no ejecución reportada. Los requisitos no funcionales de calidad se enlazan a sus fuentes y criterios explícitos en 3.3–3.5.

| Requisito | Capacidad / caso de uso | Fuente normativa y contrato | Evidencia del corte funcional |
|---|---|---|---|
| RF-001 | Solicitar y asignar turno (CU-002) | R-05 solicitud-y-asignacion; R-06 | `AppointmentsController`, `AppointmentService`; asignación LocalDemo |
| RF-002 | Listar y consultar turnos (CU-003/004) | R-05 turnos-consulta; R-06 | `AppointmentsController`, listado y filtros HTTP |
| RF-003 | Consultar detalle interno de turno (CU-005) | R-05 turnos-consulta; R-06 | `AppointmentsController`, detalle HTTP |
| RF-004 | Ciclo de vida/cancelación (CU-006/011) | R-05 turnos-ciclo-de-vida; R-06 | `AppointmentsController`, `AppointmentService`; transición LocalDemo |
| RF-005–009 | Gestionar transportistas (CU-009) | R-05 datos-maestros; R-06 | `DriversController`; listar, detalle, alta, edición e inactivación en API |
| RF-010 | Gestionar fincas (CU-012): subconjunto de sólo lectura | R-05 datos-maestros; R-06 | `FarmsController`; sólo lectura observada |
| RF-011–014 | Infraestructura transversal del canal que apoya CU-002/003/006/007; no implementa esos CU completos | R-05 integracion-whatsapp-n8n; R-06 | `ChannelController`, `ConversationService`; estados API, entrega externa no demostrada |
| RF-015–016 | Consulta técnica de conversaciones; sin CU específico en R-09 | R-05 integracion-whatsapp-n8n; R-06 | `ConversationsController`, paginación y contexto LocalDemo |
| RNF-001–003 | Persistencia y límites de rendimiento | R-05 plataforma-y-segregacion; R-13 | Contrato de persistencia; métricas de carga no disponibles |
| RNF-004–007 | Consistencia, segregación, errores y canal | R-05 plataforma-y-segregacion / ciclo de vida; R-06; R-07 | RF-001–016 y límites LocalDemo; no prueba de conformidad completa |
| RNF-008–010 | Autoridad documental, usabilidad, recuperación | R-05; R-07; R-13 | ADR y documentación; evaluación/garantía no demostrada |
| RD-001–006 | Arquitectura, aislamiento, asignación, dashboard y persistencia | R-05; R-07; R-12; R-13 | Código y vistas según 2.1–2.4; diferencias expresamente indicadas |
| OR-001–004 | Dominio, privacidad, datos demo y cobertura | R-05; R-07–R-11 | Alcance descrito; asuntos pendientes no se presentan como implementados |

Los requisitos sin evidencia de implementación directa continúan siendo normativos o restricciones aceptadas, no hechos demostrados del sistema. Los puntos abiertos de R-10 requieren resolución mediante el flujo de cambios del proyecto antes de convertirse en reglas adicionales.
