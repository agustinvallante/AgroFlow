# Registro de propuestas históricas de arquitectura frontend

> **Estado: evaluable, no vinculante.** ADR-005 mantiene React/Vite para el MVP.
> Esta matriz reubica las recomendaciones de PR #47 sin aprobar sus tecnologías.

| Tema propuesto | Estado actual | Posible criterio de evaluación |
|---|---|---|
| Next.js y App Router | No adoptado; contradictorio con ADR-005 como reemplazo de React/Vite. | Solo reconsiderar mediante decisión arquitectónica explícita y plan de migración/paridad. |
| Organización feature-first | No es convención obligatoria. | Evaluar gradualmente contra estructura existente sin refactorizar por anticipado. |
| TanStack Query | No adoptado. | Comparar necesidad con herramientas y patrones actuales antes de agregar dependencia. |
| Zustand | No adoptado. | Mantener estado local/remoto conforme a implementación actual; no sumar store por documento histórico. |
| Sesión web mediante BFF/cookie opaca | Sin decisión. | Analizar custodia, amenazas, contrato y operación junto con alternativas; no seleccionar silenciosamente. |
| Integración contract-first | Principio consistente y evaluable; OpenAPI activo es solo la demo de turnos. | Toda integración nueva espera contrato aprobado. |
| Clasificación de errores, accesibilidad y pruebas por capas | Ideas útiles de calidad, no herramientas ni arquitectura aprobadas. | Aplicar proporcionalmente a cada incremento y runner existente. |
| Polling | Permitido por la spec de monitoreo; cadencia pendiente de OD-012. | No fijar intervalo ni librería desde esta matriz. |
| Tokens/primitivas visuales | Propuesta sin aprobación de librería/estilos. | Evaluar necesidad y accesibilidad sin hacer del prototipo fuente normativa. |

El estado real del frontend y de cada vista está en el
[README del componente](../../../frontend/README.md). El backend mantiene la
autoridad de reglas, seguridad y datos. La sesión no se resuelve aquí: ninguna
alternativa de custodia ha sido seleccionada.
