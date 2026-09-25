# ADR-FE-001: Next.js como framework del frontend

- Estado: aceptada para la arquitectura objetivo
- Alcance: base técnica del frontend web
- Implementación: pendiente; este ADR no afirma que exista una aplicación Next.js aplicada

## Contexto

`frontend/` contiene únicamente documentación de reserva. El prototipo disponible bajo `docs/contexto/` usa React y Vite, pero es material de referencia y no una implementación normativa. La aplicación interna necesita enrutamiento, composición de servidor y cliente, y una posible capa servidor para la sesión web.

## Decisión

La futura aplicación web SHALL usar Next.js con TypeScript. React seguirá siendo la capa de interfaz dentro del framework. La versión exacta se fijará de manera reproducible al crear la base ejecutable.

Esta elección no autoriza importar el prototipo ni asumir que una capacidad funcional está implementada.

## Alternativas consideradas

- **React con Vite:** menor infraestructura de servidor, pero no ofrece de forma integrada los límites de render ni la posible intermediación de sesión que se quieren evaluar.
- **Otro framework React:** agrega una decisión tecnológica sin ventaja documentada para el alcance actual.
- **Aplicación sin framework:** aumenta trabajo de integración y convenciones propias.

## Consecuencias

- Se requerirá definir un modelo de despliegue compatible con las funciones de servidor utilizadas.
- El código deberá separar claramente lo ejecutado en servidor de lo enviado al navegador.
- El prototipo podrá inspirar interacción y estilos, pero no se importará como base Vite.
- CI, dependencias y código quedan fuera de este cambio documental.

## Dependencias no resueltas

- estrategia de sesión de [ADR-FE-006](ADR-FE-006-auth-session.md);
- proveedor y topología de despliegue;
- versiones de Node.js, Next.js y React al iniciar la implementación.

## Referencias

- [Arquitectura canónica](../frontend/frontend-architecture.md)
- [ADR-002: App Router](ADR-FE-002-app-router.md)
