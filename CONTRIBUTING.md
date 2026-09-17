# Contribuir a AgroFlow

## Antes de comenzar

1. Leer `docs/onboarding/getting-started.md`.
2. Identificar la capacidad afectada en `openspec/specs/`.
3. Revisar `docs/planning/open-decisions.md` y no asumir una decisión marcada como pendiente.
4. Crear o vincular un issue con el caso de uso y el área `frontend`, `backend` o `shared`.

## Cambios de comportamiento

Todo cambio funcional debe comenzar en `openspec/changes/<id-descriptivo>/` y contener propuesta, delta de especificación, diseño y tareas antes de modificar código. Los arreglos que únicamente restauran un comportamiento ya especificado pueden enlazar directamente la especificación vigente.

## Ramas y commits

- Ramas: `feature/<area>-<descripcion>`, `fix/<area>-<descripcion>` o `docs/<descripcion>`.
- Commits breves y descriptivos, preferentemente con Conventional Commits.
- No realizar push directo a `main` cuando las reglas de protección estén habilitadas.

## Pull requests

Cada pull request debe:

- enlazar el issue y el cambio OpenSpec correspondiente;
- indicar qué escenarios satisface;
- incluir pruebas proporcionales al cambio;
- actualizar contrato y documentación cuando corresponda;
- evitar secretos, credenciales y datos personales;
- cumplir la definición de terminado de `docs/development/definition-of-done.md`.

Los cambios transversales deben dividir sus tareas entre frontend y backend, pero conservar una sola especificación compartida.
