# ADR-FE-003: Organización por capacidad

- Estado: aceptada para la arquitectura objetivo
- Alcance: estructura y dependencias internas
- Implementación: pendiente

## Contexto

Una separación exclusivamente técnica tiende a dispersar cada cambio funcional entre muchas carpetas y facilita dependencias cruzadas. AgroFlow ya organiza su comportamiento normativo por capacidades y necesita mantener trazabilidad entre especificación, contrato, interfaz y pruebas.

## Decisión

El frontend SHALL organizar el código específico bajo `features/<capacidad>/`. `app/` compondrá rutas y layouts; `shared/` contendrá solo infraestructura o primitivas reutilizadas por más de una capacidad.

Una feature podrá contener componentes, consultas, mapeos, validación estructural, tipos de vista y pruebas. No contendrá una copia de reglas operativas del backend.

## Alternativas consideradas

- **Capas globales por tipo de archivo:** hace visibles las tecnologías, pero dispersa una capacidad completa.
- **Clean Architecture replicada en cada detalle:** ofrece aislamiento fuerte, pero puede añadir abstracciones prematuras a una interfaz todavía sin contratos.
- **Estructura libre por ruta:** reduce decisiones iniciales, pero dificulta límites y trazabilidad.

## Consecuencias

- Los cambios de una capacidad serán más fáciles de revisar en conjunto.
- `shared/` necesitará un criterio estricto para no transformarse en un depósito genérico.
- Las importaciones entre features deberán evitar acoplamientos circulares.
- Las abstracciones se extraerán después de observar reutilización real.

## Dependencias no resueltas

- nombres definitivos de features al seleccionar la primera porción vertical;
- alias y reglas automáticas de importación;
- ubicación final de artefactos generados desde OpenAPI.

## Referencias

- [Arquitectura canónica](../frontend/frontend-architecture.md)
- [ADR-002: jerarquía documental](ADR-002-jerarquia-documental-y-openspec.md)
