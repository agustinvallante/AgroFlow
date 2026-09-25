# ADR-FE-005: Zustand limitado al estado de interfaz

- Estado: aceptada para la arquitectura objetivo
- Alcance: estado local y efímero
- Implementación: pendiente

## Contexto

La aplicación necesitará compartir algunos estados visuales entre componentes. Sin un límite explícito, una store global podría terminar conservando credenciales, permisos o entidades del backend, duplicando el estado remoto y mezclando datos entre solicitudes renderizadas en servidor.

## Decisión

Zustand SHALL usarse únicamente para estado efímero de interfaz que no tenga a la API como propietaria. Ejemplos admisibles son la apertura de un panel o una selección visual transitoria.

No almacenará tokens, sesión autoritativa, permisos, ingenio efectivo, turnos, indicadores ni otros datos operativos. Las stores con participación en render de servidor se crearán por instancia y no como singletons compartidos entre solicitudes.

## Alternativas consideradas

- **Context y estado React exclusivamente:** válido para estado pequeño, pero puede producir proveedores complejos al crecer la interfaz.
- **Redux Toolkit:** ofrece convenciones fuertes, con mayor superficie que la necesaria para estado visual previsto.
- **Store global para todo:** simplifica accesos iniciales, pero duplica estado remoto y aumenta riesgo de contaminación.

## Consecuencias

- TanStack Query seguirá siendo propietario de las respuestas remotas.
- Cada store deberá documentar su alcance y ciclo de vida.
- Persistir una store en el navegador requerirá una decisión específica y no podrá incluir datos sensibles u operativos.
- La hidratación deberá evitar diferencias y fugas entre usuarios.

## Dependencias no resueltas

- necesidades reales de estado transversal al implementar las primeras vistas;
- política de persistencia de preferencias no sensibles, si se solicita.

## Referencias

- [ADR-FE-004](ADR-FE-004-tanstack-query.md)
- [Arquitectura canónica](../frontend/frontend-architecture.md)
