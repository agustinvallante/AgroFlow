# ADR-FE-008: Integración contract-first con OpenAPI

- Estado: aceptada para la arquitectura objetivo
- Alcance: tipos, adaptadores e integración HTTP
- Implementación: parcial; el contrato publica datos maestros, pero auth/sesión/contexto y consultas de turnos siguen pendientes

## Contexto

La API normativa se documenta en `docs/contracts/openapi.yaml`. Actualmente publica rutas de gestión de datos maestros (`drivers`, `trucks`, `farms` y sus asociaciones); todavía no publica las operaciones de autenticación/sesión/contexto ni el listado y detalle de turnos preparados para la primera porción frontend. El prototipo y otros materiales de referencia no tienen autoridad para definir endpoints. Escribir clientes contra rutas ausentes crearía contratos paralelos.

## Decisión

Toda integración HTTP del frontend SHALL partir de un contrato OpenAPI aprobado. Los tipos de transporte se generarán de forma reproducible cuando existan esquemas útiles; los modelos de vista podrán derivarse mediante mapeos explícitos sin redefinir el contrato.

No se inventarán endpoints, DTO, códigos de error, mecanismos de seguridad ni equivalencias de estados. La generación deberá formar parte de una verificación repetible y detectar divergencias.

## Alternativas consideradas

- **Tipos manuales independientes:** ofrecen control local, pero facilitan divergencias silenciosas.
- **Inferir contrato desde el backend:** convierte una implementación heredada en fuente normativa y rompe la jerarquía documental.
- **Consumir respuestas sin tipos:** reduce preparación inicial, pero desplaza errores a ejecución.

## Consecuencias

- La UI funcional espera la aprobación del contrato correspondiente.
- Los adaptadores aislarán detalles de transporte de los componentes.
- Los cambios incompatibles deberán revisarse junto con consumidores y pruebas.
- La herramienta de generación no se agrega hasta seleccionar el primer contrato real.

## Dependencias no resueltas

- rutas, esquemas, seguridad y errores para autenticación/sesión/contexto y consultas de turnos en OpenAPI;
- herramienta de generación y versión fijada;
- decisión sobre versionar artefactos generados o producirlos en CI;
- política de compatibilidad del contrato.

## Referencias

- [Contrato OpenAPI](../../contracts/openapi.yaml)
- [ADR-002: jerarquía documental](ADR-002-jerarquia-documental-y-openspec.md)
