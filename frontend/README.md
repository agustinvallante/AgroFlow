# Frontend de AgroFlow

`frontend/` es por ahora un espacio reservado: no contiene una aplicación ejecutable ni funcionalidades implementadas. La arquitectura objetivo usa Next.js con TypeScript, App Router, TanStack Query para estado remoto y Zustand únicamente para estado efímero de interfaz.

La documentación canónica para iniciar la base está en:

- [arquitectura del frontend](../docs/architecture/frontend/frontend-architecture.md);
- [matriz de decisiones del frontend](../docs/architecture/frontend/frontend-decisions.md).

Antes de implementar una pantalla o flujo, también se deben consultar:

- [especificaciones vigentes](../openspec/specs/), fuente normativa del comportamiento;
- [contratos compartidos](../docs/contracts/README.md), incluida la forma HTTP aprobada;
- [ADR](../docs/architecture/decisions/), que registran decisiones técnicas y sus estados;
- [decisiones abiertas](../docs/planning/open-decisions.md), que no deben completarse mediante supuestos.

El prototipo [AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard) es solo una referencia visual. No es código aplicado ni una fuente de reglas, contratos o datos para esta carpeta.

El frontend no debe recrear reglas de asignación, prioridad, autorización o transiciones de estado. El backend conserva esas responsabilidades y debe consumirse mediante contratos OpenAPI aprobados. El modelo de sesión web aceptado es una cookie `HttpOnly` con identificador opaco y una sesión del lado servidor en el BFF de Next.js; no hay implementación y sus detalles operativos y contrato siguen pendientes. OpenAPI ya contiene rutas de datos maestros (`drivers`, `trucks`, `farms` y `driver-truck-associations`) y recibos de mutación, pero aún no contiene rutas de autenticación/sesión/contexto ni de turnos. La implementación funcional de cada capacidad depende de sus contratos y decisiones pendientes.
