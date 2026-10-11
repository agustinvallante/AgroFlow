# ADR-FE-010: Propuesta sobre actualización periódica

> **Estado: evaluación sujeta a OpenSpec.** La especificación de monitoreo
> permite consultas periódicas y no exige WebSockets; no se fija intervalo.

PR #47 recomendó TanStack Query para polling, pero la librería no fue adoptada.
La necesidad funcional de actualizar vistas debe implementarse con la solución
adecuada a React/Vite y al contrato aprobado. OD-012 mantiene pendientes la
fecha operativa, fórmulas, horizonte y cadencia de refresco.

El backend entrega el estado operativo vigente; la UI no recalcula indicadores
a partir de respuestas parciales. Este documento no autoriza frecuencia,
política de reintentos ni comportamiento en segundo plano.
