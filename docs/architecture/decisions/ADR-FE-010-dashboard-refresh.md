# ADR-FE-010: Refresco periódico del dashboard

- Estado: aceptada en el mecanismo permitido; configuración bloqueada por OD-012
- Alcance: vigencia de datos del dashboard
- Implementación: pendiente

## Contexto

La especificación de monitoreo permite que el frontend actualice el dashboard mediante consultas periódicas y no exige WebSockets para el MVP. Sin embargo, siguen abiertos el intervalo, la fecha operativa y las fórmulas de indicadores.

## Decisión

El frontend SHALL soportar polling como mecanismo inicial de actualización del dashboard. TanStack Query coordinará las consultas periódicas cuando la vista esté activa y de acuerdo con una configuración aprobada.

Este ADR no fija frecuencia, indicadores ni comportamiento en segundo plano. El backend seguirá calculando y devolviendo el estado vigente; el navegador no derivará indicadores operativos desde filas parciales ni desde su reloj local.

## Alternativas consideradas

- **WebSockets obligatorios:** ofrecen actualizaciones push, pero OpenSpec no los exige para el MVP y agregan operación prematura.
- **Actualización manual únicamente:** es simple, pero no cumple por sí sola la capacidad de refresco periódico permitida para la vista operativa.
- **Polling configurable:** reduce complejidad inicial y conserva una ruta futura hacia otros mecanismos.

## Consecuencias

- La frecuencia podrá ajustarse sin modificar componentes de negocio.
- Se deberá evitar duplicar solicitudes al cambiar visibilidad, foco o montaje.
- Una respuesta nueva reemplazará la vista local con estado confirmado.
- Se medirán errores y latencia antes de considerar un mecanismo en tiempo real.

## Dependencias no resueltas

- OD-012: fórmulas, fecha operativa e intervalo;
- contrato de consulta del dashboard;
- política de reintentos, pausa en segundo plano y degradación;
- objetivos de operación de OD-011.

## Referencias

- [Especificación de monitoreo operativo](../../../openspec/specs/monitoreo-operativo/spec.md)
- [ADR-FE-004](ADR-FE-004-tanstack-query.md)
