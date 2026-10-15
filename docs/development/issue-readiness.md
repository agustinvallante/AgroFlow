# Preparar issues para trabajo autónomo

Un agente que usa Pi, Gentle-AI, Claude u OpenCode **no necesita el historial de este chat**. Sí necesita acceso al repositorio y a la versión concreta de la documentación que rige la tarea. La fuente de verdad es `openspec/specs/`, luego `docs/contracts/openapi.yaml`, los ADR y las guías (ADR-002); el texto de una issue no puede sobrescribirlos.

## Formato del backlog backend y frontend

Las issues `Bxx` y `Fxx` se redactan en Markdown con clave estable, objetivo, base integrada y PRs condicionales, trabajo incluido, archivos existentes/propuestos, criterios de aceptación, dependencias/decisiones, fuera de alcance y fuentes normativas. La [actualización del 2026-10-15](../planning/mvp-backlog-sync-2026-10-15.md) amplía también `B25`–`B32` y elimina dependencias circulares. **Actualizar una descripción no la convierte en una especificación ejecutable ni en Ready**: rutas, DTOs y nombres marcados como propuestos necesitan su contrato aprobado.

## Puerta «Ready for implementation»

Antes de asignar una issue de endpoint a una persona o agente autónomo, su responsable debe comprobar y enlazar explícitamente:

1. **Base exacta**: commit o rama de `main` y PRs previas requeridas. Si una spec aún vive en una PR, no presentarla como aprobada en `main`.
2. **Resultado y límites**: comportamiento observable, actor, ingenio, permisos, invariantes, operaciones incluidas y excluidas; identificar cualquier compatibilidad con la demo.
3. **Contrato**: método/ruta, DTOs y campos obligatorios/opcionales, ejemplos de éxito/error, códigos HTTP, filtros/paginación y enlace a la sección concreta de OpenAPI. B00 debe resolver el contrato antes de cerrar una tarea que dependa de él.
4. **Persistencia y concurrencia**: entidades, relaciones, migración, seed si aplica, transacción, idempotencia y efecto ante reintentos; no inferirlos del prototipo.
5. **Mapa de código**: rutas existentes a modificar, archivos nuevos previstos, pruebas y consumidores afectados. Los nombres son orientación hasta comparar contra la rama base real.
6. **Criterios verificables**: escenarios felices, límites, validación, autorización, aislamiento entre ingenios, conflicto, error, persistencia y comandos de prueba/validación proporcionados por el repositorio.
7. **Decisiones**: estado de cada `OD-*` aplicable en `docs/planning/open-decisions.md`; si falta una decisión que cambia el contrato o la regla, la issue queda **no lista** y no se rellena el vacío con una suposición del agente.

El agente debe leer `AGENTS.md`, las specs y ADR enlazados y el contrato del commit base. Ante contradicción, detener el cambio de comportamiento y solicitar coordinación al responsable de contrato; no tomar el texto de una issue, una PR draft ni este chat como norma superior.

### Plantilla para el refinamiento de una issue

```markdown
## Base y autoridad
- Commit/PR base:
- OpenSpec (requisitos y escenarios):
- OpenAPI (operaciones y schemas):
- ADR y decisiones OD aplicables:

## Resultado y fuera de alcance
- Actor, ingenio, permisos e invariantes:
- Resultado observable:
- Compatibilidad requerida:
- Exclusiones:

## Implementación
- Archivos existentes a cambiar:
- Archivos previstos a crear:
- Modelo/migración y efecto sobre datos existentes:
- Integraciones y dependencias de otras issues:

## Pruebas y aceptación
- Casos de éxito, validación, autorización, conflicto y reintento:
- Comandos de restore/build/test/validación:
- Evidencia a adjuntar en la PR:

## Bloqueos
- Decisiones o PRs pendientes (o «ninguno»):
```

**Estado al 2026-10-15:** B01 #8 está cerrada y #76/#77 integradas; no rehacer el renombre/retiro del e-commerce. B00 #7 sigue siendo el primer trabajo contractual: cerrar las decisiones y operaciones del slice, sin inventar pendientes ni exigir completar todo el MVP antes de cualquier avance. Las fundaciones de #47, el empaquetado #79 y los controles #80 siguen condicionados a revisión/integración; el documento #88 no aprueba historial conversacional. Antes de delegar una implementación, aplicar esta puerta sobre main y las PRs efectivamente incorporadas. No mover estados/assignees/Projects por el solo hecho de refrescar la descripción.
