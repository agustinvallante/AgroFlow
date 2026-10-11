# Contratos compartidos

Este directorio describe cómo se comunican frontend, backend e integraciones. Los
contratos expresan técnicamente el comportamiento definido por OpenSpec; no
agregan reglas de negocio por cuenta propia.

## Artefactos y alcance activo

- [`openapi.yaml`](openapi.yaml): contrato HTTP activo de la demo local. Publica
  `GET /health` y las operaciones de alta, listado, detalle y transición de
  `/api/v1/appointments`. Mantiene versión `0.3.0`, esquemas de errores de la
demo y seguridad deshabilitada explícitamente por operación.
- [`master-data-foundation-proposal.md`](master-data-foundation-proposal.md):
  inventario no normativo de ideas técnicas de PR #47 para evaluar después de
  resolver B00/B02/B03 y las decisiones correspondientes.
- Futuros esquemas de eventos o webhooks: deben documentar versión,
autenticación, correlación, idempotencia y errores una vez aprobados.

OpenAPI **no** publica actualmente operaciones de datos maestros, autenticación,
sesión, asociaciones, dashboard ni paginación. Sus rutas, DTO y reglas de
idempotencia requieren acuerdo explícito; la propuesta adjunta no es una API
utilizable. La operación de demo sin autenticación tampoco define la seguridad
del MVP.

Las issues y documentos de cada equipo deben enlazar el contrato aprobado y no
copiar DTOs, filtros, códigos de error ni reglas de transición. Un cambio
observable requiere el cambio OpenSpec de la capacidad propietaria, contrato,
pruebas y documentación coherentes.

## Perfil de demo local

- API desde el host: `http://localhost:5000`.
- API desde n8n en Docker Desktop: `http://host.docker.internal:5000`.
- Salud técnica: `GET /health`, fuera de `/api/v1`.
- Negocio: `/api/v1/appointments` y sus subrecursos publicados.
- El alta resuelve teléfono, patente y código de finca del seed; el listado
  admite los filtros descritos en OpenAPI.
- Sin autenticación ni selección de ingenio: simplificación local exclusiva de
  la demo, no una decisión de seguridad MVP.

El contrato de la demo conserva versión `0.3.0`. La salvedad de Docker/Linux y
el recorrido reproducible están en el [runbook](../development/local-demo-runbook.md);
el alcance y exclusiones, en [local-demo-scope.md](../planning/local-demo-scope.md).

## Reglas

1. No implementar un endpoint nuevo antes de acordar su comportamiento observable.
2. Mantener nombres, tipos, códigos HTTP y errores coherentes entre contrato y código.
3. Tratar todo cambio incompatible como una decisión explícita de versión o migración.
4. No incluir ejemplos con secretos ni datos personales reales.
5. Documentar solicitudes, respuestas, validaciones, contexto, errores y reintentos
   solo cuando estén aprobados.
6. No interpretar ausencia de autenticación en la demo local como resolución de
   autenticación, autorización o segregación del MVP.
