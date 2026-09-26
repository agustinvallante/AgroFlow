# Demo local de turnos básicos

## Why

La issue #49 aprueba una demostración local mínima para el lunes que permita integrar el recorrido principal de turnos sin confundirlo con el MVP completo. El equipo necesita un corte ejecutable y repartible entre seis personas, con un único contrato HTTP y sin adelantar capacidades que no son necesarias para comprobar solicitud, consulta y ciclo de vida.

## What Changes

- Define un perfil de demo exclusivamente local, sin login ni registro, despliegue, CRUD de datos maestros, interrupciones, mapa o reportes.
- Incluye CU-002, CU-003, CU-004, CU-005 y CU-006, más la cancelación de CU-011.
- Fija como contrato canónico `POST /api/v1/appointments`, `GET /api/v1/appointments`, `GET /api/v1/appointments/{id}` y `POST /api/v1/appointments/{id}/transitions`; conserva `GET /health` fuera de `/api/v1`.
- Define DTOs mínimos, filtros, estados, errores y supuestos reproducibles de seed para un único ingenio de demostración, con identificadores naturales para que n8n opere desde el teléfono remitente sin consultas de datos maestros ni UUID fijados en el workflow.
- Documenta URLs locales, integración entre n8n y el host Docker, secuencia de trabajo, división entre seis personas, límites de propiedad y criterios de aceptación.

## Capabilities

Se agregan requisitos acotados a `aceptacion-del-mvp`, `turnos-solicitud-y-asignacion`, `turnos-consulta`, `turnos-ciclo-de-vida` e `integracion-whatsapp-n8n`.

## Impact

- **OpenSpec:** agrega un perfil de demo local sin modificar ni rebajar el alcance del MVP vigente.
- **Contrato:** `docs/contracts/openapi.yaml` pasa a ser la única definición exacta de rutas, campos, estados, filtros, respuestas y errores HTTP de la demo.
- **Backend y persistencia:** deberá sostener asignación, consultas, transiciones, cancelación, capacidad y seed local para un único ingenio.
- **Frontend:** deberá consumir el contrato para alta, listado, detalle y transiciones; no implementará reglas de negocio.
- **n8n/chatbot:** deberá consumir las mismas operaciones para solicitar, consultar e informar `EN_CAMINO`; no mantendrá estado paralelo.
- **Pruebas y documentación:** deberán comprobar el recorrido local y mantener explícita la diferencia entre la demo y la aceptación del MVP completo.

## No alcance

La demo no incluye autenticación, registro, autorización por rol, segregación multiingenio demostrable, despliegue, CRUD de transportistas/camiones/fincas, gestión de asociaciones, interrupciones, dashboard de indicadores o cupos, mapa, reportes, WhatsApp real, auditoría productiva, recuperación, observabilidad productiva ni aceptación integral del MVP.

Estas exclusiones sólo recortan la demo local. No eliminan, reemplazan ni debilitan requisitos de `openspec/specs/`; la implementación completa deberá satisfacerlos mediante cambios posteriores.

## Trazabilidad

Issue #49; CU-002, CU-003, CU-004, CU-005, CU-006 y cancelación de CU-011; RN-008, RN-009, RN-012, RN-015 a RN-017, RN-019 a RN-030A y RN-050 a RN-052A.
