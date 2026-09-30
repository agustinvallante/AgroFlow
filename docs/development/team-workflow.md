# Flujo de trabajo del equipo

## Un repositorio, dos áreas de trabajo

El código permanece en un monorepo con `frontend/` y `backend/`. OpenSpec, contratos y decisiones son compartidos. La separación de la planificación ya utiliza dos GitHub Projects:

- [**AgroFlow — Frontend**](https://github.com/users/agustinvallante/projects/1), para tareas del área frontend;
- [**AgroFlow — Backend**](https://github.com/users/agustinvallante/projects/2), para tareas del área backend.

GitHub Projects son tableros de planificación, no repositorios de código. Las decisiones y el contrato compartidos no se copian como fuentes normativas separadas para cada tablero. Una necesidad transversal puede descomponerse en issues por área y enlazarse, manteniendo una sola especificación por capacidad.

Los tableros contienen issues de implementación, pero el estado de cada una cambia con el trabajo del equipo. El Project Backend incluye `B00`–`B07` y `B10`–`B39`; el frontend también tiene tareas planificadas. Un issue en Backlog es una propuesta a refinar, no evidencia de que el endpoint exista ni autorización para resolver decisiones pendientes. La [hoja de ruta](../planning/implementation-roadmap.md) describe las dependencias lógicas entre las del backend.

## Estados y metadatos

| Mecanismo | Uso |
|---|---|
| Estado del Project | `Backlog`, `To do`, `In progress`, `In review`, `Done`. |
| Etiqueta de área | `area:backend`, `area:frontend`, `area:shared` o `area:docs-devops`, según el trabajo real. |
| Trazabilidad del issue | Capacidad de `openspec/specs/`, casos `CU-*`, reglas `RN-*`, dependencias y puerta `MVP-G*` cuando correspondan. |

No se debe asumir que Área, Capacidad, Caso de uso, Prioridad o Entrega existan como campos personalizados del Project. Si el equipo quiere añadirlos, debe definir valores y mantenimiento antes de depender de ellos para automatizaciones o filtros.

## De una necesidad a código

1. Crear o tomar un issue del área correspondiente con la plantilla de funcionalidad o error. Antes de delegarlo a un agente sin contexto de esta conversación, comprobar la [guía de preparación autónoma](issue-readiness.md).
2. Vincular capacidad, caso de uso y reglas `RN-*`.
3. Confirmar que ninguna decisión pendiente bloquee el resultado.
4. Para comportamiento nuevo o modificado, crear `openspec/changes/<id>/`.
5. Revisar propuesta, escenarios, contrato OpenAPI y tareas con todas las áreas afectadas. Las rutas en títulos de issues backend son propuestas, no contrato aprobado, hasta cerrar `B00`.
6. Crear una rama corta y desarrollar la porción asignada.
7. Abrir un pull request y completar la plantilla.
8. Integrar solo cuando cumpla la definición de terminado.

## División de tareas sin duplicar requisitos

Ejemplo para “cancelar turno”:

- issue o épica compartida: comportamiento CU-011 y cambio OpenSpec;
- tarea backend: autorización, transición, transacción, capacidad y pruebas;
- tarea frontend: acción, confirmación, estados de carga/error y pruebas;
- integración conversacional de cancelación solo si se aprueba explícitamente como ampliación mediante OpenSpec; no forma parte del MVP vigente;
- una sola especificación y un solo contrato para todas las tareas.

## Ramas y revisiones

- `feature/backend-<descripcion>`
- `feature/frontend-<descripcion>`
- `feature/shared-<descripcion>`
- `fix/<area>-<descripcion>`
- `docs/<descripcion>`

Los propietarios por ruta se incorporarán en `CODEOWNERS` cuando el equipo confirme quién revisa cada área. Hasta entonces, todo cambio de contrato o OpenSpec requiere al menos una revisión de frontend y una de backend.

## Comunicación de bloqueos

Una tarea recibe la etiqueta `status:blocked` cuando depende de una decisión abierta, un contrato no aprobado o una dependencia externa. Conserva su estado actual y el issue debe enlazar el bloqueo concreto; no se completa el vacío implementando una suposición local.

Las reglas de acceso, etiquetado, automatización de los tableros y CI/CD están detalladas en [Automatización de GitHub](github-automation.md).
