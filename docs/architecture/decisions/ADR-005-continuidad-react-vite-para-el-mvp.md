# ADR-005: Continuidad de React/Vite para el frontend del MVP

- Estado: aceptada
- Fecha: 2026-09-29
- Alcance: arquitectura de la aplicación web y continuidad del trabajo de la demo

## Contexto

Las PR [#66](https://github.com/agustinvallante/AgroFlow/pull/66) y [#65](https://github.com/agustinvallante/AgroFlow/pull/65) incorporaron a `main` un dashboard React, TypeScript y Vite. La cola de turnos ya tiene un adaptador HTTP para el contrato de la demo, pruebas y un modo mock explícito para las vistas todavía no conectadas. La [PR #47](https://github.com/agustinvallante/AgroFlow/pull/47), aún no integrada, propone otra base con Next.js. Mantener ambas como supuestas arquitecturas vigentes haría ambiguos el backlog, las revisiones y la documentación.

La decisión no cambia por sí sola requisitos de negocio. Las capacidades obligatorias siguen definidas en OpenSpec y la API conserva la autoridad operativa descrita en [ADR-003](ADR-003-backend-fuente-de-verdad.md).

## Decisión

El frontend del MVP **mantiene y evoluciona React + TypeScript + Vite** en `frontend/`. Se amplía gradualmente el adaptador HTTP y se sustituyen los mocks de recorridos obligatorios por respuestas de la API. La integración existente de turnos es el punto de partida, no evidencia de que todas las pantallas funcionen contra backend.

No se planifica una migración a Next.js para este MVP. La PR #47 no define la arquitectura vigente ni debe fusionarse como reemplazo del frontend actual sin una nueva decisión explícita, un plan de migración y verificación de paridad. Ideas puntuales reutilizables de esa PR pueden evaluarse por separado sin aceptar el cambio de plataforma.

El modo mock puede conservarse para desarrollo, pruebas y vistas fuera del MVP, pero debe quedar distinguible del modo HTTP. Ningún recorrido obligatorio se acepta usando datos simulados como fuente operativa. El navegador tampoco debe calcular prioridad, asignación, capacidad, autorización ni transiciones por su cuenta.

## Consecuencias

- Las tareas frontend `F01`–`F09` se planifican sobre los archivos y patrones presentes en `frontend/`, no sobre una aplicación Next.js hipotética.
- Los cambios observables de API se acuerdan primero mediante OpenSpec y `docs/contracts/openapi.yaml`; frontend y backend comparten un solo contrato.
- El equipo aprovecha la infraestructura existente de Vite, TypeScript, Vitest y el adaptador HTTP de turnos, mientras corrige sus limitaciones para el MVP.
- `docs/frontend/README.md`, los issues y las instrucciones de incorporación deben describir el estado real del código y separar demo, MVP y vistas fuera de alcance.

## Cuándo reconsiderar

Sólo si aparece una necesidad concreta no atendible razonablemente con la base actual y el equipo aprueba el costo de migrar, documentar una nueva ADR, actualizar OpenSpec/contrato cuando el comportamiento cambie y demostrar paridad funcional antes de retirar React/Vite.
