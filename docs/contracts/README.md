# Contratos compartidos

Este directorio describe cómo se comunican frontend, backend e integraciones. Los
contratos expresan técnicamente el comportamiento definido por OpenSpec; no
agregan reglas de negocio por cuenta propia.

## Contratos por perfil

- [`openapi.yaml`](openapi.yaml): contrato HTTP activo de `LocalDemo` (0.5.0).
  Describe salud, turnos, canal WhatsApp, conversaciones, transportistas y
  consulta de fincas. Cada operación declara su propia seguridad.
- [`openapi.mvp-appointment-reads.yaml`](openapi.mvp-appointment-reads.yaml):
  contrato objetivo de listado y detalle internos del MVP, con autenticación
  Bearer y ámbito por ingenio. Reutiliza los esquemas comunes del contrato
  de la demo y se rige por el
  [cambio OpenSpec de lecturas](../../openspec/changes/definir-lecturas-turnos-mvp/proposal.md).
  Las operaciones `listarTurnosMvp` y `obtenerTurnoMvp` quedan pendientes de
  implementación hasta que estén listas sus dependencias.
- [`master-data-foundation-proposal.md`](master-data-foundation-proposal.md):
  inventario no normativo de ideas técnicas de PR #47 para evaluar después de
  resolver B00/B02/B03 y las decisiones correspondientes.
- Futuros esquemas de eventos o webhooks: deben documentar versión,
autenticación, correlación, idempotencia y errores una vez aprobados.

El contrato de la demo describe la API disponible. El contrato del MVP en este
corte abarca exclusivamente las dos lecturas internas de turnos. Las
operaciones de sesión y las demás capacidades del MVP se definen en sus propios
cambios OpenSpec antes de implementarse.

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
- Salud y turnos funcionan sin autenticación en un solo ingenio sembrado.
  Las operaciones de canal y gestión requieren las claves descritas en
  OpenAPI.

La salvedad de Docker/Linux y el recorrido reproducible están en el
[runbook](../development/local-demo-runbook.md);
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
