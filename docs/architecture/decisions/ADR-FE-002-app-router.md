# ADR-FE-002: Propuesta histórica de App Router

> **Estado: supersedida/no adoptada.** Dependía de la propuesta Next.js de
> ADR-FE-001, incompatible con la continuidad React/Vite aprobada por ADR-005.

## Contexto y propuesta conservada

PR #47 recomendó App Router, Server Components por defecto y Client Components
solo para interacción. Era una guía condicionada a Next.js, no una necesidad de
producto ni una arquitectura desplegada.

## Estado vigente

El frontend existente permanece en React/Vite. No se adoptan App Router,
Route Handlers ni límites de render específicos de Next.js. La idea general de
mantener secretos del lado servidor es una consideración de seguridad, no una
selección de arquitectura de sesión: esa decisión permanece abierta.
