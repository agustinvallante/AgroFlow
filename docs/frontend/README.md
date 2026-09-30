# Guías del frontend

La aplicación web existente está en [`../../frontend/`](../../frontend/) y usa React, TypeScript y Vite. La [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md) mantiene esa base para el MVP. El [README del componente](../../frontend/README.md) contiene comandos de instalación, estructura y configuración local; esta guía ubica su alcance frente a las especificaciones.

El prototipo orienta la experiencia visual, pero no reemplaza OpenSpec ni el contrato. La interfaz debe representar el estado confirmado por el backend y no calcular por su cuenta prioridad, capacidad, permisos o transiciones.

Para el MVP, la cola visual debe mostrar las franjas del día —incluidas las vacías— y los cupos restantes obtenidos de la API. La duración y el cupo se preparan por ingenio al iniciar el ambiente; su edición desde la pantalla de configuración no forma parte del MVP. El calendario y la zona horaria siguen pendientes de `OD-004`. La interfaz interna permite a operador y supervisor gestionar los tres catálogos y crear o cancelar turnos de su ingenio conforme a las specs aprobadas.

## Estado actual y trabajo pendiente

Las PR [#66](https://github.com/agustinvallante/AgroFlow/pull/66) y [#65](https://github.com/agustinvallante/AgroFlow/pull/65) importaron el dashboard y conectaron la cola de turnos a la API de la demo. Con `VITE_DATA_SOURCE=http`, alta, listado/filtros, detalle, avance y cancelación utilizan `/api/v1/appointments`; existe polling y pruebas automatizadas. Con `VITE_DATA_SOURCE=mock`, el recorrido usa datos en memoria. El [contrato de la demo](../contracts/openapi.yaml) no incluye todavía autenticación ni todos los recursos del MVP.

Panel general, catálogos, chatbot, reportes y configuración permanecen en mock incluso en modo HTTP. Sólo las vistas que pertenecen al MVP deben pasar gradualmente a datos reales; mapa, reportes y edición de configuración no se convierten en obligaciones por aparecer en el prototipo. Los issues `F01`–`F09` del [Project Frontend](https://github.com/users/agustinvallante/projects/1) describen los deltas: sesión, catálogos/asociaciones, turnos, dashboard, interrupciones y experiencia/QA.

Cada conexión nueva debe esperar un contrato aprobado y mostrar estados de carga, error, vacío y permiso denegado sin predecir un resultado que aún no confirmó la API. La UI debe distinguir claramente cualquier dato simulado. La [replanificación tras la demo](../planning/mvp-replan-2026-09-29.md) indica las puertas aún no satisfechas y enlaza `F01`–`F09`.
