# Contratos compartidos

Este directorio describe cómo se comunican frontend, backend e integraciones. Los contratos expresan técnicamente el comportamiento definido por OpenSpec; no agregan reglas de negocio por cuenta propia.

## Artefactos

- [`openapi.yaml`](openapi.yaml): contrato HTTP canónico. Para la demo local de la issue #49 define salud, alta, listado, detalle y transiciones de turnos.
- futuros esquemas de eventos o webhooks: deben documentar versión, autenticación, correlación, idempotencia y errores.

Las issues y documentos de frontend, backend, dashboard o chatbot deben enlazar OpenAPI y no copiar DTOs, filtros, códigos de error ni reglas de transición. Si un consumidor necesita cambiar una forma observable, primero se actualizan el cambio OpenSpec y OpenAPI y luego sus implementaciones.

Las issues `B10`–`B39` del [Project Backend](https://github.com/users/agustinvallante/projects/2) nombran rutas propuestas para repartir el trabajo. Las cuatro rutas de turnos publicadas en OpenAPI son las canónicas para la demo local. Las rutas restantes continúan como propuestas hasta su aprobación. Si el diseño aprobado cambia un path, método, campo, permiso o código de respuesta, el equipo actualiza OpenAPI y las issues/consumidores afectados; no se conserva un nombre de issue por inercia.

## Perfil de demo local

- API desde el host: `http://localhost:5000`.
- API desde n8n en Docker Desktop: `http://host.docker.internal:5000`.
- Salud técnica: `GET /health`, deliberadamente fuera de `/api/v1`.
- Negocio: `/api/v1/appointments` y sus subrecursos publicados.
- Alta de turno con `carrierPhone`, `truckPlate` y `farmCode` del seed; consulta CU-003 con el filtro opcional `phone`.
- n8n toma el teléfono del mensaje entrante y no requiere endpoints de datos maestros ni UUIDs hardcodeados; las respuestas pueden conservar referencias internas con UUID.
- Sin autenticación ni selección de ingenio: esta simplificación es sólo local y no define seguridad productiva.

La salvedad de Docker/Linux y el recorrido reproducible están en el [runbook](../development/local-demo-runbook.md). El alcance y sus exclusiones están en [local-demo-scope.md](../planning/local-demo-scope.md).

## Reglas

1. No implementar un endpoint nuevo antes de acordar su comportamiento observable.
2. Mantener nombres, tipos, códigos HTTP y errores coherentes entre contrato y código.
3. Tratar todo cambio incompatible como una decisión explícita de versión o migración.
4. No incluir ejemplos con secretos ni datos personales reales.
5. Incluir en cada operación documentada el esquema de solicitud/respuesta, validaciones observables, contexto de ingenio, errores relevantes y conducta de reintentos cuando aplique.
6. No interpretar la ausencia de seguridad en la demo local como resolución de autenticación, autorización o segregación del MVP.
