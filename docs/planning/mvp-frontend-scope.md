# Alcance de planificación del frontend del MVP

> **Estado: orientación no normativa.** OpenSpec es la fuente del comportamiento;
> ADR-005 mantiene React/Vite. Esta nota separa la implementación existente de
> las brechas y dependencias futuras. No exige desarrollar todas las pantallas
> ni autoriza decisiones pendientes.

## Base existente

El dashboard React/TypeScript/Vite está en `frontend/`. La composición permite
seleccionar modo mock o HTTP. Solo turnos tiene un adaptador HTTP para la demo:
alta, listado/filtros, detalle y transiciones/cancelación. Panel general,
chatbot, transportistas, reportes y configuración continúan con repositorios
mock. Los mocks identifican datos de demostración; no son una implementación
funcional ni evidencia de aceptación MVP.

La base incluye `composition/container.ts`, `ApiClient`, DTOs/mappers,
`HttpTurnoRepository`, hooks, vistas y pruebas. Se preserva y evoluciona lo que
existe; no se sustituye por un framework distinto por la presencia de
propuestas en PR #47. No se requiere conectar todas las pantallas en esta PR.

## Qué puede orientar la planificación

Los recorridos y permisos se derivan de las capacidades OpenSpec y ADR-004.
React/Vite se conserva por ADR-005; SQLite, por ADR-006. El backend determina
identidad, autorización, ingenio, reglas y estado persistido. Las vistas nuevas
consumen contratos aprobados, marcan explícitamente cualquier mock y no
presentan datos locales como resultados operativos.

Next.js, App Router, TanStack Query, Zustand, BFF y propuestas de arquitectura
por feature de PR #47 quedan como ideas evaluables, no políticas aceptadas. Los
archivos `ADR-FE-*` y los documentos de arquitectura frontend son antecedentes
reclasificados para no contradecir main. La sesión web no tiene arquitectura
seleccionada: resolver su custodia requiere decisión explícita, no una elección
implícita por continuidad de esos documentos.

## Incrementos y dependencias

| Capacidad | Estado útil conocido | Dependencias antes de ampliar |
|---|---|---|
| Turnos | Flujo HTTP de demo existente en React/Vite. | Contrato y comportamiento aprobados para ampliar más allá de demo; no confundir la integración local con identidad/aislamiento MVP. |
| Datos maestros | Vistas/repositorios mock existentes. | B00/B02/B03/B05 y acuerdos de campos, rutas, DTO, seguridad y permisos; sin CRUD HTTP activo por esta guía. |
| Sesión y contexto | No hay flujo web MVP conectado descrito aquí. | Acordar custodia de sesión, ciclo de vida, contrato y operación; no elegir BFF ni token en navegador por defecto. |
| Dashboard/interrupciones | Vistas de demostración y necesidades en OpenSpec. | Contratos propietarios y detalles aún abiertos de OD-012/OD-007. |
| Canal WhatsApp/n8n | No lo sustituye una pantalla web. | El backend conserva autoridad y el canal sigue su capacidad propietaria. |

La lista ordena dependencias conceptuales, no promete secuencia ni fecha. Cada
cambio de comportamiento debe actualizar la capacidad OpenSpec propietaria,
contrato, pruebas y documentación en el mismo cambio. No se crea una segunda
especificación normativa “del frontend”.

## Límites

- No crear endpoints, DTO, paginación, idempotency key o política de seguridad
  desde una vista o propuesta antigua.
- No seleccionar quién custodia el JWT, cómo funciona la sesión web ni dónde se
  almacena, sin acuerdo explícito.
- No inferir permisos de asociaciones, reactivación, otros roles o capacidad.
- No convertir mocks de dashboard/catálogos en comportamiento aceptado.
- No declarar B38, CRUD, G01–G12 o el MVP completo a partir de archivos presentes.
