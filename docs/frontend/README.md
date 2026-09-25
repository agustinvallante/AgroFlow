# Documentación del frontend

Esta página sirve como punto de navegación. No define comportamiento ni reemplaza las fuentes con mayor autoridad.

## Arquitectura objetivo

- [Arquitectura canónica del frontend](../architecture/frontend/frontend-architecture.md): responsabilidades, límites, estructura objetivo e integración contract-first.
- [Decisiones de la fundación del frontend](../architecture/frontend/frontend-decisions.md): estado confirmado, propuesto o bloqueante de cada decisión y enlaces a sus ADR.

La arquitectura describe el objetivo Next.js con TypeScript, App Router, TanStack Query y Zustand. No afirma que exista una implementación: `frontend/` todavía contiene solo este README. OpenAPI ya publica contratos parciales de datos maestros y recibos de mutación, pero aún no incluye autenticación/sesión/contexto ni consultas de turnos.

## Jerarquía de autoridad

Ante una diferencia, se aplica la jerarquía establecida por [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md):

1. [`openspec/specs/`](../../openspec/specs/) define el comportamiento vigente.
2. [`docs/contracts/`](../contracts/) define la forma técnica aprobada de interacción.
3. [`docs/architecture/decisions/`](../architecture/decisions/) registra decisiones técnicas y su estado.
4. La arquitectura y las guías del frontend explican cómo orientar una implementación sin crear requisitos funcionales.
5. Los README facilitan navegación e instrucciones rápidas.
6. El prototipo y otros antecedentes son referencia histórica o visual no normativa.

El modelo de sesión aceptado es un identificador opaco en cookie `HttpOnly` y una sesión del lado servidor en el BFF de Next.js; no hay implementación y su ciclo operativo y contrato siguen pendientes. El backend continúa siendo la fuente operativa de verdad para reglas, permisos, aislamiento y estado confirmado.
