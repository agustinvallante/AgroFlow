# Diseño histórico del baseline PR #47 (supersedido)

> **Estado: documento de archivo, no diseño activo.** La propuesta original no
> debe aplicarse como una especificación transversal. Las capacidades vigentes
> son las especificaciones canónicas de `openspec/specs/`.

La intención histórica era conectar capacidades funcionales, backend, frontend
 y evidencia en un baseline. La organización resultante duplicaba o anticipaba
 decisiones propietarias; `main` y las ADR-004/005/006 fijaron desde entonces
políticas específicas de operación, continuidad React/Vite y SQLite.

## Referencias vigentes

- Comportamiento: la especificación canónica propietaria de cada capacidad.
- Políticas aprobadas: ADR-004 y las decisiones `OD-*` vigentes.
- Frontend: ADR-005 y la implementación existente React/Vite.
- Persistencia: ADR-006; SQLite académico, WAL condicionado y PostgreSQL como
  etapa comercial futura.
- Reconciliación PR #47: [`docs/planning/pr47-mvp-guide-alignment.md`](../../../docs/planning/pr47-mvp-guide-alignment.md).

Este archivo no define framework, endpoints, DTO, autenticación web, paginación,
clave idempotente, matriz completa de roles ni otros detalles abiertos. La
fundación recuperada no expone CRUD HTTP. Las propuestas técnicas solo pueden
volver a considerarse mediante cambios de la capacidad propietaria y decisión
explícita.
