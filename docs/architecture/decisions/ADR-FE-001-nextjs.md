# ADR-FE-001: Propuesta histórica de Next.js

> **Estado: supersedida como decisión vigente por ADR-005 (React/Vite).** Se
> conserva para registrar una alternativa considerada en PR #47; no autoriza
> migrar ni agregar dependencias.

## Contexto

PR #47 propuso Next.js como framework web, motivado por render servidor y una
posible mediación de sesión. Desde entonces main incorporó una aplicación
React/TypeScript/Vite y ADR-005 decidió mantenerla para el MVP.

## Propuesta conservada

La propuesta recomendaba Next.js con TypeScript. Su razonamiento sobre separar
servidor/cliente puede revisarse si surge una necesidad concreta, pero no es una
política aprobada ni evidencia de código existente.

## Estado vigente y reconsideración

Se conserva y evoluciona `frontend/` conforme a ADR-005. Reconsiderar framework
requiere decisión explícita, costo/beneficio, plan de migración y verificación
de paridad. El despliegue y la custodia de sesión web no quedan decididos por
esta propuesta.
