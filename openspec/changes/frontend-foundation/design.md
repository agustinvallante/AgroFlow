# Registro de diseño histórico: fundación frontend PR #47

> **Estado: supersedido/evaluable; no es arquitectura aprobada.** ADR-005
> mantiene React/Vite y el código existente.

PR #47 recomendaba Next.js/App Router, organización feature-first, TanStack Query,
Zustand, integración contract-first y BFF. La selección de plataforma y librerías
no se trasladó a main. Cualquier idea se evalúa contra la aplicación presente
sin asumir migración o agregar dependencias.

La custodia de sesión web está sin decisión. No se selecciona BFF, cookie,
token de navegador ni sesión del backend. OpenAPI activo describe la demo local
de turnos; los contratos de futuras capacidades esperan aprobación.

Las propuestas generales de separar responsabilidades, mantener al backend
como autoridad, mostrar errores sin exponer información sensible y usar pruebas
proporcionadas pueden revisarse en incrementos concretos. No crean requisitos
normativos nuevos ni reemplazan la autoridad de OpenSpec.
