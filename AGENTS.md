# Instrucciones para asistentes de desarrollo

## Fuente de verdad

- Leer `openspec/specs/` antes de cambiar comportamiento.
- Leer los ADR aplicables en `docs/architecture/decisions/`.
- No tratar prototipos, mocks, código heredado ni PDF de referencia como requisitos vigentes si contradicen OpenSpec.
- No implementar decisiones marcadas como pendientes en `docs/planning/open-decisions.md`.

## Flujo obligatorio

- Un comportamiento nuevo o modificado requiere un cambio en `openspec/changes/`.
- Mantener una única especificación por capacidad; no duplicarla en frontend y backend.
- Actualizar contrato, pruebas y documentación en el mismo cambio.
- El backend es la fuente operativa de verdad; frontend y n8n no deben duplicar reglas de negocio.

## Límites

- `backend/` contiene la API y persistencia.
- `frontend/` contiene la aplicación web.
- `openspec/` contiene comportamiento normativo.
- `docs/` contiene contexto, decisiones y guías.
- Nunca versionar secretos, credenciales ni datos personales.
