# Flujo de trabajo del equipo

## Un repositorio, dos áreas de trabajo

El código permanece en un monorepo con `frontend/` y `backend/`. OpenSpec, contratos, issues y decisiones son compartidos. La separación del trabajo se realizará mediante responsables por ruta y, cuando el equipo los cree, dos GitHub Projects:

- **AgroFlow — Frontend**, filtrado por tareas del área frontend;
- **AgroFlow — Backend**, filtrado por tareas del área backend.

GitHub Projects son vistas de planificación, no repositorios de código. Una épica transversal se define una sola vez y puede tener subtareas por área visibles en ambos tableros.

## Campos recomendados para los Projects

| Campo | Valores iniciales |
|---|---|
| Estado | Backlog, Ready, In progress, In review, Blocked, Done |
| Área | Frontend, Backend, Shared, Docs/DevOps |
| Capacidad | Nombre del directorio en `openspec/specs/` |
| Caso de uso | CU-001 a CU-012 |
| Prioridad | Alta, Media, Baja |
| Entrega | Hito o demo objetivo |

## De una necesidad a código

1. Crear un issue con la plantilla de funcionalidad o error.
2. Vincular capacidad, caso de uso y reglas `RN-*`.
3. Confirmar que ninguna decisión pendiente bloquee el resultado.
4. Para comportamiento nuevo o modificado, crear `openspec/changes/<id>/`.
5. Revisar propuesta, escenarios, contrato y tareas con todas las áreas afectadas.
6. Crear una rama corta y desarrollar la porción asignada.
7. Abrir un pull request y completar la plantilla.
8. Integrar solo cuando cumpla la definición de terminado.

## División de tareas sin duplicar requisitos

Ejemplo para “cancelar turno”:

- issue o épica compartida: comportamiento CU-011 y cambio OpenSpec;
- tarea backend: autorización, transición, transacción, capacidad y pruebas;
- tarea frontend: acción, confirmación, estados de carga/error y pruebas;
- tarea de integración, si aplica: intención de WhatsApp y respuesta;
- una sola especificación y un solo contrato para todas las tareas.

## Ramas y revisiones

- `feature/backend-<descripcion>`
- `feature/frontend-<descripcion>`
- `feature/shared-<descripcion>`
- `fix/<area>-<descripcion>`
- `docs/<descripcion>`

Los propietarios por ruta se incorporarán en `CODEOWNERS` cuando el equipo confirme quién revisa cada área. Hasta entonces, todo cambio de contrato o OpenSpec requiere al menos una revisión de frontend y una de backend.

## Comunicación de bloqueos

Una tarea se marca `Blocked` cuando depende de una decisión abierta, un contrato no aprobado o una dependencia externa. El issue debe enlazar el bloqueo concreto; no se completa el vacío implementando una suposición local.
