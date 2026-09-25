# ADR-FE-002: App Router y límites de render

- Estado: aceptada para la arquitectura objetivo
- Alcance: rutas, layouts y composición servidor/cliente
- Implementación: pendiente

## Contexto

Next.js permite distintos modelos de enrutamiento y render. Sin una convención, es fácil enviar JavaScript innecesario al navegador, mezclar secretos con configuración pública o convertir toda la aplicación en componentes cliente.

## Decisión

La futura aplicación SHALL usar App Router. Los Server Components serán el punto de partida para composición y contenido sin interacción del navegador; los Client Components se limitarán al borde que necesite eventos, APIs del navegador o hooks cliente.

Los Route Handlers solo se usarán cuando exista una responsabilidad servidor explícita. No implementarán reglas de negocio y no constituyen por sí solos la aprobación de un BFF.

## Alternativas consideradas

- **Pages Router:** estable y conocido, pero no es la dirección elegida para la nueva base.
- **Todo como Client Components:** simplifica el modelo mental inicial, a costa de ampliar bundle y exposición de datos.
- **Todo como Server Components:** no cubre las interacciones necesarias de una aplicación operativa.

## Consecuencias

- Cada ruta deberá declarar de forma visible su límite interactivo.
- La obtención de datos deberá evitar saltos servidor-a-Route-Handler-servidor innecesarios.
- La serialización hacia el navegador se reducirá a los datos requeridos para la vista.
- Las decisiones de caché del framework no reemplazarán a TanStack Query ni al contrato de vigencia de cada capacidad.

## Dependencias no resueltas

- sesión web y uso eventual de Route Handlers;
- estrategia de despliegue con runtime servidor;
- política concreta de render y caché para cada capacidad futura.

## Referencias

- [ADR-FE-001](ADR-FE-001-nextjs.md)
- [ADR-FE-006](ADR-FE-006-auth-session.md)
