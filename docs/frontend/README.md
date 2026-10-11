# Documentación del frontend

La aplicación existente en [`../../frontend/`](../../frontend/) usa React,
TypeScript y Vite. La [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)
mantiene esa base para el MVP. El [README del componente](../../frontend/README.md)
documenta comandos y estado local; esta guía delimita qué está implementado y
qué sigue siendo planificación.

## Estado comprobado en el repositorio

- El flujo de turnos ofrece alta, listado y filtros, detalle, transiciones y
  cancelación contra el contrato de demo `/api/v1/appointments` cuando se usa
  `VITE_DATA_SOURCE=http`.
- Panel general, chatbot, transportistas, reportes y configuración permanecen
  conectados a repositorios mock; esos datos deben presentarse como demostración.
- La integración de turnos no acredita autenticación, aislamiento MVP entre
  ingenios, CRUD maestro ejecutable ni aceptación completa del MVP.
- No se exige conectar todas las pantallas en esta etapa. Cada ampliación espera
  el comportamiento y contrato aprobados de su capacidad propietaria.

El frontend presenta respuestas confirmadas; el backend decide estado operativo,
permisos, aislamiento, capacidad y reglas. Los mocks pueden servir para desarrollo
claramente identificado, pero no sustituyen recorridos reales de aceptación.

## Arquitectura y propuestas

La arquitectura vigente es la aplicación React/Vite ya incorporada y sus
patrones actuales. Los documentos `ADR-FE-*`, `frontend-architecture.md` y
`frontend-decisions.md` conservan ideas de PR #47 como propuestas o registro
histórico para evaluación; no establecen Next.js, App Router, BFF, TanStack Query,
Zustand ni una organización obligatoria. La custodia de sesión web sigue sin
selección: no se elige aquí una alternativa.

Para políticas de negocio, consultar [`openspec/specs/`](../../openspec/specs/);
para operaciones HTTP aprobadas, [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml);
y para bloqueos vigentes, [`docs/planning/open-decisions.md`](../planning/open-decisions.md).
Las rutas y DTO de maestros, sesión y capacidades MVP no se deben deducir de
borradores descartados ni introducir como si estuvieran expuestos.
