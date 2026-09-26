# Diseño: corrección documental del baseline de PR #47

## Resumen

Este es un artefacto documental de reconciliación de decisiones, no una autorización de diseño para implementar. Alinea las notas de PR con `main`, ADR-004 y B00, y conserva las opciones abiertas y las propuestas futuras. OpenSpec sigue siendo normativo para el comportamiento; este cambio no contiene especificaciones de capacidades ni modifica `openspec/specs/`.

## Fuentes y autoridad

- [ADR-004](../../../docs/architecture/decisions/ADR-004-politicas-operativas-del-mvp.md) define las políticas aprobadas y deja explícitamente abiertos varios detalles.
- [`open-decisions.md`](../../../docs/planning/open-decisions.md) registra los límites vigentes de las decisiones.
- [`mvp-defaults.md`](../../../docs/planning/mvp-defaults.md) es una referencia sobre el estado de las decisiones y sus propuestas, no un baseline normativo.
- [ADR-002](../../../docs/architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md) establece la autoridad de OpenSpec sobre el comportamiento.
- [ADR-003](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md) establece la autoridad operativa del backend.

Esta corrección documental no puede aprobar comportamiento. Cada tema abierto requiere una decisión explícita, actualizar B00 o un ADR cuando corresponda y crear o actualizar el cambio OpenSpec de la capacidad propietaria antes de implementarlo.

## Decisiones aprobadas, dentro de sus límites

- **OD-001:** prioridad por mayor tiempo transcurrido desde el corte de caña; luego flota propia y, finalmente, solicitud registrada antes. El formato y la validación del corte, así como la concurrencia, siguen abiertos.
- **OD-002:** como máximo un transportista autorizado activo por camión; se conservan las asociaciones históricas. Los detalles de gestión siguen abiertos.
- **OD-003:** en el MVP no se elige una ventana preferida; el backend asigna la próxima ventana compatible.
- **OD-004:** la duración y la capacidad se configuran por ingenio mediante datos iniciales. Los 30 minutos y el cupo 2 se usan únicamente como datos de demostración, no como reglas de negocio. Siguen abiertos el calendario, la zona horaria, los límites y el horizonte de búsqueda.
- **OD-005:** se requieren la fecha y hora del corte y la carga estimada. Siguen abiertos las unidades, los formatos, los rangos y las validaciones.
- **OD-007 (parcial):** operador y supervisor pueden gestionar los tres catálogos y crear/cancelar turnos internos dentro de su ingenio, según ADR-004. La matriz completa de roles y los permisos adicionales siguen abiertos.
- **OD-012 (parcial):** la cola incluye las franjas vacías y los cupos restantes según el estado de la API. Siguen abiertas las fórmulas de indicadores, la fecha operativa, la zona horaria, el horizonte y la cadencia de actualización.
- **OD-013 (parcial):** están aprobadas las operaciones de listar, crear, editar e inhabilitar transportistas, camiones y fincas para operador y supervisor. Siguen abiertos los campos editables, sus validaciones, la gestión de asociaciones y la reactivación.

## Decisiones abiertas y propuestas conservadas

OD-006 y OD-008 a OD-011 siguen abiertos. Los enfoques detallados aún no aprobados para esos temas, así como los detalles sin resolver de OD-012 y OD-013, son únicamente propuestas y no autorizan su implementación.

Se conservan como temas propuestos no normativos la idempotencia durable y los controles de concurrencia; la integración autenticada con n8n y la deduplicación de mensajes; la auditoría append-only y su conservación; el outbox, los estados de entrega y el tratamiento de reintentos; un perfil académico de operación y recuperación; los indicadores y mecanismos de actualización del dashboard; las matrices de roles y permisos; y el historial y la gestión de asociaciones. Cualquiera de estas ideas puede modificarse, rechazarse o aprobarse mediante el proceso de decisión. En particular, este documento no aprueba una matriz completa de cuatro roles, permisos para reintentar notificaciones, actualización cada 30 segundos ni reactivación de asociaciones.

## Límites de API e implementación

Las rutas, DTO y convenciones de paginación de B00 siguen abiertos. Este artefacto no define endpoints, DTO, encabezados, códigos de estado ni semántica de API. Ninguna propuesta futura de este documento modifica la API ni autoriza trabajo en backend, frontend, persistencia, migraciones o n8n.

## Alternativas e ideas históricas

Las preferencias históricas por una ventana solicitada o por varias asociaciones activas de transportistas no describen el comportamiento vigente del MVP y no se convierten en requisitos del contrato. Las demás opciones detalladas que se conservan en `mvp-defaults.md` también son no normativas hasta que se aprueben específicamente.

## Resultado esperado de la revisión

Quienes revisen deben comprobar que las notas reflejen fielmente `main`, ADR-004 y B00; que las aprobaciones parciales no se presenten como completas; y que las opciones abiertas sigan explícitas. Todo cambio posterior de comportamiento debe proponerse en el cambio OpenSpec de la capacidad propietaria e incluir el contrato, las pruebas y la documentación correspondientes, de acuerdo con la política del proyecto.
