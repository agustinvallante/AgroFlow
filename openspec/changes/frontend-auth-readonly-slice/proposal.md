# Propuesta histórica: porción web autenticada de solo lectura

> **Estado: propuesta no implementada y parcialmente supersedida.** Se redactó
> bajo la arquitectura Next.js/BFF de PR #47, que ADR-005 no adoptó. No elige
> cómo custodiar sesión ni autoriza construir esta porción.

La idea de planificar autenticación, contexto y consultas de turnos de forma
vertical puede conservarse como un posible trabajo futuro. Sus detalles de
sesión, BFF, cookie opaca, endpoints, DTO, roles, errores, almacenamiento y
despliegue son pendientes, no decisiones seleccionadas. La base actual es
React/Vite; turnos solamente cuenta con la integración HTTP de demo.

La autenticación/contexto corresponde a `acceso-y-autorizacion`; listado y
detalle, a `turnos-consulta`; garantías transversales, a la capacidad existente
de plataforma. Este documento no añade specs, rutas ni requisitos. Cualquier
trabajo funcional requiere resolver primero las decisiones correspondientes,
aprobar contrato OpenAPI y actualizar pruebas y documentación.

Consultar [ADR-005](../../../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md),
[decisiones abiertas](../../../docs/planning/open-decisions.md) y la
[matriz de alineación PR47](../../../docs/planning/pr47-mvp-guide-alignment.md).
