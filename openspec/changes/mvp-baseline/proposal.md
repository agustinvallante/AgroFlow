# Propuesta: baseline funcional del MVP

## Objetivo

Formalizar como cambio OpenSpec revisable el baseline del MVP de AgroFlow aprobado por el usuario, de modo que la implementación posterior disponga de reglas deterministas para permisos, datos maestros, asignación, operación, monitoreo, integración y garantías transversales.

Este cambio convierte en comportamiento propuesto los defaults OD-001 a OD-013 documentados en [`docs/planning/mvp-defaults.md`](../../../docs/planning/mvp-defaults.md). Su aprobación por el usuario lo acepta como baseline de implementación, pero no le otorga todavía carácter normativo. Las decisiones permanecen registradas como pendientes en [`docs/planning/open-decisions.md`](../../../docs/planning/open-decisions.md) hasta que este cambio sea revisado y archivado; la propuesta no modifica las especificaciones vigentes antes de ese archivo.

## Alcance

- prioridad lexicográfica estable para asignación y reprogramación;
- asociación explícita muchos a muchos entre transportistas y camiones;
- ventana preferida opcional con fallback a la próxima compatible;
- ventanas fijas de 30 minutos, capacidad positiva por ingenio y zona operativa `America/Argentina/Tucuman`;
- campos mínimos y validaciones de una solicitud de turno;
- idempotencia persistida para todas las creaciones y mutaciones críticas del baseline, mutaciones transaccionales y conflictos de concurrencia optimista;
- matriz única de cuatro roles internos, propiedad de `acceso-y-autorizacion`, con autorización efectiva en backend;
- credencial de servicio acotada para n8n, autenticidad del canal y deduplicación de mensajes;
- auditoría append-only de mutaciones críticas;
- entrega reproducible de notificaciones con estados observables y reintentos acotados;
- perfil académico reproducible de operación, persistencia y recuperación;
- indicadores diarios calculados por backend y refresco permitido cada 30 segundos;
- gestión acotada de datos maestros con identificadores globales de plataforma, aislamiento por ingenio, inhabilitación, reactivación e historia, sin geodatos.

## Fuera de alcance

- implementar backend, frontend, n8n, persistencia, migraciones o CI;
- modificar en esta etapa `docs/contracts/openapi.yaml` o fijar endpoints y DTO definitivos;
- crear una capacidad normativa propiedad del frontend;
- geolocalización, mapas operativos, PostGIS, rutas o posición estimada;
- optimización predictiva, puntajes configurables o edición manual de prioridad;
- roles personalizados, permisos por registro o sesiones multiingenio;
- alta disponibilidad, failover automático, SLO productivo o certificación de producción;
- campañas, múltiples proveedores simultáneos o garantías de entrega exactamente una vez.

## Capacidades afectadas

- **acceso-y-autorizacion:** es la única propietaria de los cuatro roles internos y de la matriz completa de operaciones, incluida la configuración de ventanas/capacidad y el reintento manual de notificaciones.
- **datos-maestros:** define gestión acotada, identificadores globales con conflictos opacos entre ingenios, campos mínimos y asociación transportista-camión muchos a muchos.
- **turnos-solicitud-y-asignacion:** define solicitud mínima, prioridad, preferencia, capacidad y asignación concurrente.
- **turnos-ciclo-de-vida:** concreta los actores autorizados para transiciones y cancelaciones.
- **interrupciones-operativas:** concreta permisos y reprogramación conforme a la prioridad aprobada.
- **monitoreo-operativo:** define conteos diarios, fecha operativa y refresco cada 30 segundos.
- **integracion-whatsapp-n8n:** define autenticación de servicio, deduplicación, entrega observable y exige para el reintento manual el permiso propiedad de `acceso-y-autorizacion`.
- **plataforma-y-segregacion:** define el alcance completo de idempotencia para creaciones y mutaciones críticas, concurrencia, auditoría y recuperación reproducible.

No se modifica `turnos-consulta`: la asociación autorizada que esa capacidad ya exige queda definida por `datos-maestros`. Tampoco se modifica `aceptacion-del-mvp`: sus recorridos y su exigencia de decisiones aprobadas ya integran estas capacidades sin duplicar sus reglas.

## Casos de uso relacionados

- CU-001: acceso de usuarios internos;
- CU-002: solicitud y asignación de turnos;
- CU-003, CU-004 y CU-005: consulta de turnos y vistas internas;
- CU-006 y CU-011: avance y cancelación del ciclo operativo;
- CU-007: interrupciones y reprogramación;
- CU-008: monitoreo diario;
- CU-009, CU-010 y CU-012: gestión de datos maestros.

## Impacto

| Área | Impacto esperado después de la aprobación |
|---|---|
| Backend | Será la autoridad de permisos, prioridad, capacidad, transiciones, indicadores, destinatarios y estado persistido. |
| Persistencia | Requerirá asociaciones con vigencia, calendario/capacidad, idempotencia durable, control de versión, outbox y auditoría append-only. |
| Contrato API | Deberá publicar entradas mínimas, preferencias opcionales, versiones, claves idempotentes, conflictos, permisos efectivos, indicadores y estados de entrega. Se realizará en un cambio posterior. |
| Frontend | Consumirá permisos, resultados e indicadores autoritativos; podrá consultar el dashboard cada 30 segundos sin recalcular negocio. |
| n8n/WhatsApp | Usará una identidad de servicio acotada, conservará correlación/idempotencia y comunicará solo resultados confirmados por la API. |
| Pruebas | Deberán cubrir éxito, validaciones, permisos, aislamiento, conflictos, reintentos, fallas, recuperación y recorridos integrados. |
| Operación | Requerirá un ambiente académico repetible, datos ficticios, salud, logs correlacionados y backup/restore probado. |

## Criterios de aceptación del cambio

- cada default aprobado tiene una única capacidad normativa propietaria;
- las deltas usan requisitos y escenarios observables con SHALL/MUST;
- no se agregan endpoints, DTOs ni detalles de un proveedor concreto a las especificaciones;
- el backend conserva la autoridad operativa y los consumidores no duplican reglas;
- las tareas posteriores enlazan especificación, contrato, implementación, pruebas y evidencia;
- `docs/planning/open-decisions.md` permanece sin cambios hasta la revisión y el archivo de esta propuesta, momento en que el baseline pasa a ser normativo.
