# OpenSpec en AgroFlow

Este directorio contiene el comportamiento normativo compartido por frontend, backend e integraciones. No reemplaza las carpetas de código: evita que cada área mantenga una versión distinta de las mismas reglas.

## Estructura

```text
openspec/
|- config.yaml
|- specs/                 Comportamiento vigente por capacidad
`- changes/               Propuestas en revisión y cambios archivados
```

## Flujo de trabajo

1. Leer la capacidad vigente en `specs/` y las decisiones abiertas en `../docs/planning/open-decisions.md`.
2. Crear un directorio descriptivo en `changes/`.
3. Documentar propuesta, diseño, tareas y delta de especificación.
4. Revisar el cambio con las áreas afectadas antes de implementar.
5. Implementar contrato, backend, frontend, integración y pruebas según corresponda.
6. Validar y archivar el cambio cuando todas las tareas estén terminadas.

Un cambio típico tendrá esta forma:

```text
changes/agregar-confirmacion-de-arribo/
|- proposal.md
|- design.md
|- tasks.md
`- specs/turnos-ciclo-de-vida/spec.md
```

Las delta specs usan secciones `## ADDED Requirements`, `## MODIFIED Requirements`, `## REMOVED Requirements` o `## RENAMED Requirements`. Las especificaciones vigentes de `specs/` usan `## Requirements`.

## Convenciones

- Una capacidad describe comportamiento observable, no componentes internos.
- Cada requisito usa `SHALL` o `MUST` y tiene al menos un escenario `GIVEN/WHEN/THEN`.
- Los identificadores `CU-*` y `RN-*` mantienen trazabilidad con el relevamiento.
- Una decisión pendiente no se completa con una suposición; se resuelve y aprueba antes de programarla.
- El contrato HTTP se publica en `../docs/contracts/openapi.yaml` cuando se defina.

## Validación

Desde la raíz del repositorio:

```powershell
openspec validate --all --strict --no-interactive
openspec validate --archived --no-interactive
```
