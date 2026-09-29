# Replanificación del MVP tras la demo local

- Corte de revisión: `main` en `ef4cc5b` (2026-09-29).
- Propósito: distinguir trabajo **integrado**, brechas y evidencia pendiente para la [épica #46](https://github.com/agustinvallante/AgroFlow/issues/46). Este documento es un mapa de implementación, no modifica las especificaciones vigentes.
- Fuentes normativas: [`openspec/specs/`](../../openspec/specs/), [OpenAPI](../contracts/openapi.yaml) y [guía de aceptación](../product/mvp-acceptance.md). Las simplificaciones de la [demo local](local-demo-scope.md) no reducen el MVP.

## Qué quedó reutilizable

| Área | Base integrada en `main` | Límite comprobable |
|---|---|---|
| Contrato | PR [#55](https://github.com/agustinvallante/AgroFlow/pull/55), [#56](https://github.com/agustinvallante/AgroFlow/pull/56), [#57](https://github.com/agustinvallante/AgroFlow/pull/57), [#58](https://github.com/agustinvallante/AgroFlow/pull/58) y [#60](https://github.com/agustinvallante/AgroFlow/pull/60): alcance, runbook y OpenAPI `0.3.0` para la demo de turnos. | El contrato publicado cubre salud y cuatro operaciones de turnos **sin autenticación**. No describe todo el MVP. |
| Modelo y API | PR [#50](https://github.com/agustinvallante/AgroFlow/pull/50) y [#61](https://github.com/agustinvallante/AgroFlow/pull/61): entidades iniciales, migraciones y seed SQLite; alta, listado, detalle y transiciones/cancelación de turnos con pruebas. | `LocalDemo:Enabled` activa esta API y persistencia sólo en el perfil local. Conviven módulos heredados de comercio electrónico; falta identidad/segregación, prioridad completa, idempotencia, CRUD, interrupciones y lecturas operativas. |
| Frontend | PR [#66](https://github.com/agustinvallante/AgroFlow/pull/66) y [#65](https://github.com/agustinvallante/AgroFlow/pull/65): React/Vite, cola y detalle conectables por HTTP, alta, filtros, transición, cancelación, polling y pruebas. | Sólo el recorrido de turnos usa HTTP con `VITE_DATA_SOURCE=http`. Panel general, transportistas, chatbot, reportes y configuración siguen en mock. La [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md) fija continuidad de esta base; la PR #47 no está integrada. |
| n8n y QA | Existen un [runbook de demo](../development/local-demo-runbook.md) y un repositorio separado de chatbot: [agroflow-chatbot](https://github.com/BraianMedrano/agroflow-chatbot). | La presencia de un workflow o un runbook no demuestra por sí sola las puertas de aceptación ni dos ejecuciones integradas reproducibles. Verificar el export, su seguridad, reintentos y evidencia de extremo a extremo. |

La demo añadió una porción vertical valiosa, pero **no está aceptada como MVP**. El equipo eligió SQLite para el MVP en la [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md); esto no traslada el supuesto de un único ingenio, la ausencia de login ni el perfil `LocalDemo` al producto final. WAL sólo se habilitará si supera las pruebas acordadas.

## Brechas por puerta de aceptación

`Parcial` indica una base técnica reutilizable, no que la puerta esté cumplida. Ninguna puerta se marca `Cumple` sin la evidencia exigida en la [guía](../product/mvp-acceptance.md).

| Puerta | Base actual | Trabajo/evidencia que falta |
|---|---|---|
| G01 Preparación reproducible | Parcial: migraciones y seed SQLite del perfil local. | Perfil SQLite del MVP, configuración/seed por ingenio y ejecución desde clon limpio por otra persona. |
| G02 Acceso y segregación | No demostrado: demo sin login ni contexto de ingenio autenticado. | Identidad, roles, denegación de token inválido, aislamiento entre al menos dos ingenios y prueba desde UI. |
| G03 Datos maestros | Parcial: entidades, seed y asociaciones iniciales. | Contratos, CRUD/inhabilitación y asociación administrable, validaciones, permisos, historial y UI real. |
| G04 Solicitud y asignación | Parcial: alta, primera ventana con cupo y rechazo de turno activo duplicado en demo. | Prioridad acordada, calendario por ingenio, concurrencia, idempotencia, aislamiento y pruebas de competencia/cupo. |
| G05 Consulta, ciclo y cancelación | Parcial: API y cola HTTP de demo permiten recorridos básicos. | Seguridad y filtros del MVP, pruebas integradas de ambos ciclos y liberación de cupo en el perfil SQLite MVP. |
| G06 Dashboard y franjas | No: panel general usa mock. | API de indicadores y ventanas vacías/cupos, cálculo por ingenio y pantalla HTTP coherente. |
| G07 Interrupciones | No. | Registro/cierre, tratamiento por estado, reprogramación, destinatarios y visualización desde UI. |
| G08 WhatsApp/n8n | Parcial por integración de demo fuera de este checkout. | Verificar flujo importable, canal controlado, autenticación, correlación, deduplicación, aviso `EN_CAMINO` y notificación de interrupción. |
| G09 Contrato e integración | Parcial: OpenAPI `0.3.0` de demo y consumidor HTTP de turnos. | Contrato completo del MVP, compatibilidad verificada de frontend/API/n8n y eliminación de mocks en recorridos obligatorios. |
| G10 Calidad, seguridad y trazabilidad | Parcial: CI y pruebas específicas de demo. | Matriz requisito–escenario–evidencia, casos de seguridad e integración, revisión de secretos/datos y defectos. |
| G11 Auditoría y recuperación | No demostrado. | Política aprobada, pruebas de auditoría y ejercicio de respaldo/restauración. |
| G12 Presentación repetible | No hay acta de aceptación del MVP. | Dos ejecuciones fechadas del guion completo sobre la misma revisión, reinicio y datos controlados. |

## Mapa de trabajo y dependencias

Los `Bxx` son issues existentes del [Project Backend](https://github.com/users/agustinvallante/projects/2). Los `Fxx` identifican el nuevo backlog del [Project Frontend](https://github.com/users/agustinvallante/projects/1). Sus descripciones deben formular **deltas sobre la demo**, no repetir como pendiente lo que ya está integrado. La asignación exacta y el estado vivo se consultan en GitHub Projects; esta tabla sólo fija la secuencia lógica.

| Frontend | Issue | Delta principal |
|---|---|---|
| F01 | [#67](https://github.com/agustinvallante/AgroFlow/issues/67) | Acceso y sesión. |
| F02 | [#68](https://github.com/agustinvallante/AgroFlow/issues/68) | Transportistas. |
| F03 | [#69](https://github.com/agustinvallante/AgroFlow/issues/69) | Camiones. |
| F04 | [#70](https://github.com/agustinvallante/AgroFlow/issues/70) | Fincas. |
| F05 | [#71](https://github.com/agustinvallante/AgroFlow/issues/71) | Asociaciones transportista–camión. |
| F06 | [#72](https://github.com/agustinvallante/AgroFlow/issues/72) | Evolución del recorrido HTTP de turnos. |
| F07 | [#73](https://github.com/agustinvallante/AgroFlow/issues/73) | Dashboard, franjas y cupos reales. |
| F08 | [#74](https://github.com/agustinvallante/AgroFlow/issues/74) | Interrupciones. |
| F09 | [#75](https://github.com/agustinvallante/AgroFlow/issues/75) | Estados UX, límites de vistas no-MVP y QA integrada. |

| Frente | Issues | Inicio verificable |
|---|---|---|
| Contrato y plataforma | `B00`–`B05`, `B10` | Cerrar sólo los detalles `OD-*` que bloquean el primer recorrido, ampliar OpenSpec/OpenAPI, implementar el perfil SQLite MVP, retirar dominio heredado, identidad y aislamiento. `B01` puede avanzar con limpieza no conductual en paralelo. |
| Catálogos y asociaciones | `B11`–`B24`, `B38`; `F02`–`F05` | Modelos existentes sirven de base; acordar campos/validaciones y contratos antes de CRUD backend y UI. |
| Turnos del MVP | `B25`–`B29`; `F06` | Extender `/api/v1/appointments` y `/transitions` en vez de crear rutas paralelas heredadas de títulos antiguos; completar política, seguridad, idempotencia y experiencia UI. |
| Dashboard | `B35`, `B39`; `F07` | Backend provee métricas y franjas/cupos reales; frontend deja de alimentar panel desde mock. |
| Interrupciones y notificaciones | `B33`, `B34`, `B36`, `B37`; `F08` | Definir decisiones restantes, persistir/reprogramar, exponer lecturas y conectar UI y avisos. |
| Canal conversacional | `B30`–`B32`, `B07` | Reusar las operaciones canónicas cuando correspondan; cerrar seguridad y semántica del canal, adaptar el workflow en su repositorio real y probar reintentos. |
| Calidad y límites visuales | `B06`; `F09` | Pruebas de contrato/E2E, evidencia G01–G12, accesibilidad y distinción visible de vistas no-MVP; no convertir mapa/reportes/configuración en requisitos sin OpenSpec. |

## Primer incremento recomendado

1. Revisar `B00` con [OD-004 a OD-014](open-decisions.md): calendario/horizonte, validaciones, idempotencia, seguridad de n8n, auditoría y métricas. Para SQLite (`OD-014`), acordar pruebas del diario/WAL, migración y recuperación sin tratar estos detalles como ya implementados.
2. Preparar un cambio OpenSpec y una ampliación de OpenAPI para el primer recorrido autenticado, reutilizando las cuatro operaciones de turnos de la demo. Revisar el contrato con responsables de backend, frontend y n8n.
3. En paralelo, ejecutar `B01` para retirar el dominio heredado y `F01` para preparar la capa de sesión/UI contra el contrato acordado; `B02`/`B03` proveen persistencia e identidad para ese recorrido.
4. Completar un corte vertical pequeño —solicitud, consulta, avance y cancelación de turno autenticado y aislado— antes de abrir varias pantallas CRUD a la vez. Después incorporar catálogos y dashboard real, interrupciones, canal y verificación final.

La [hoja de ruta](implementation-roadmap.md) detalla esta secuencia. Una tarea no se considera cerrada por existir código de demo o una PR: debe cumplir su delta y la [definición de terminado](../development/definition-of-done.md).
