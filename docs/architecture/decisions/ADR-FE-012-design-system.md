# ADR-FE-012: Fundación del sistema de diseño

- Estado: propuesta; pendiente de aprobación de herramientas y lenguaje visual
- Alcance: tokens, primitivas, accesibilidad y relación con el prototipo
- Implementación: pendiente

## Contexto

El prototipo aporta referencias visuales, pero también contiene estructura y comportamientos que no son normativos. Importarlo completo fijaría accidentalmente reglas, estilos y componentes antes de validar accesibilidad, mantenimiento y ajuste al nuevo framework.

## Recomendación propuesta

Se recomienda comenzar con tokens semánticos y un conjunto pequeño de primitivas accesibles para tipografía, color, espaciado, foco y estados. Los componentes específicos de negocio se construirían dentro de sus features usando esas primitivas.

El prototipo podrá orientar composición visual, pero no se importará ni será fuente de contratos, permisos, datos o reglas. La librería de componentes y la estrategia de estilos permanecen sin seleccionar.

## Alternativas consideradas

- **Importar el prototipo completo:** acelera una apariencia inicial, pero arrastra Vite, mocks y decisiones no aprobadas.
- **Adoptar una librería completa de inmediato:** ofrece cobertura amplia, pero fija dependencias antes de conocer las necesidades reales.
- **Primitivas y tokens mínimos:** exige trabajo inicial deliberado, pero permite validar accesibilidad y evolucionar con las capacidades.

## Consecuencias si se aprueba la recomendación

- Los estados de carga, vacío, error, deshabilitado y foco compartirán lenguaje visual.
- La accesibilidad será criterio de los componentes, no una corrección posterior.
- Los componentes de negocio no se promoverán a `shared` sin reutilización comprobada.
- La referencia visual podrá cambiar para ajustarse a OpenSpec y al contrato.

## Dependencias no resueltas

- identidad visual y tokens aprobados;
- estrategia de estilos y librería de primitivas;
- alcance de navegadores y requisitos de accesibilidad verificables;
- proceso de documentación y revisión visual.

## Referencias

- [Arquitectura canónica](../frontend/frontend-architecture.md)
- [ADR-002: jerarquía documental](ADR-002-jerarquia-documental-y-openspec.md)
