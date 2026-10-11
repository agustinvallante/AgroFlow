# ADR-FE-004: Propuesta histórica de TanStack Query

> **Estado: no adoptada; librería pendiente de evaluación.** No agregar la
> dependencia por esta nota. La integración React/Vite existente es la base.

PR #47 propuso TanStack Query para caché, carga, error e invalidación de datos
remotos. Son preocupaciones válidas que deben atenderse en los adaptadores y
vistas de cada incremento, pero no requieren una librería predeterminada.

Si una decisión futura evalúa esta opción, debe considerar patrones y
herramientas ya presentes, contratos aprobados, reintentos y consistencia con
el estado confirmado por backend. Ninguna mutación se presenta como exitosa
antes de confirmación del backend.
