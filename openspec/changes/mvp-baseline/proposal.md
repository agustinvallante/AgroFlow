# Propuesta: corrección documental del baseline de PR #47

## Objetivo

Este cambio es un artefacto documental para revisar y reconciliar las decisiones de PR #47 con `main`, ADR-004 y las porciones aprobadas en B00. No agrega especificaciones normativas, no implementa comportamiento ni autoriza implementar decisiones aún abiertas.

OpenSpec sigue siendo la fuente normativa del comportamiento. Este cambio es exclusivamente documental y usa `skip_specs: true` porque no contiene deltas de capacidades. No modifica `openspec/specs/`.

## Autoridad y estado de las decisiones

- OD-001 a OD-005 siguen `main` y ADR-004; los detalles aún abiertos se mantienen como tales según `docs/planning/open-decisions.md`.
- OD-007 tiene aprobación parcial: están aprobados los permisos de operador y supervisor expresamente enumerados en ADR-004; el resto de la matriz de roles sigue abierto.
- OD-012 tiene aprobación parcial: la cola incluye franjas vacías y cupos restantes obtenidos de la API. Siguen abiertos las fórmulas, la fecha operativa, la zona horaria, el horizonte y la cadencia de actualización.
- OD-013 tiene aprobación parcial: está aprobado que operador y supervisor listen, creen, editen e inhabiliten transportistas, camiones y fincas. Siguen abiertos los campos editables, sus validaciones, la gestión de asociaciones y la reactivación.
- OD-006 y OD-008 a OD-011 siguen abiertos. Las propuestas detalladas para esos temas y las partes aún no resueltas de OD-012 y OD-013 son ideas no normativas. Antes de implementarlas requieren aprobación explícita mediante B00/ADR, según corresponda, y el cambio OpenSpec de la capacidad propietaria.

Se conservan, sin eliminarlas ni adoptarlas, las ideas previas sobre idempotencia, outbox, auditoría, comportamiento del dashboard, matrices de roles y gestión de asociaciones. Ninguna propuesta de este cambio cierra una decisión abierta.

## Alcance

- Corregir los metadatos y documentos explicativos de este cambio para distinguir con precisión las decisiones aprobadas de las opciones abiertas.
- Conservar enlaces y propuestas futuras útiles, identificándolas como no normativas.
- Mantener las tareas como instancias de decisión y controles documentales, no como instrucciones para implementar reglas aún no aprobadas.

## Fuera de alcance

- Cambios en código fuente, API, contrato, pruebas o especificaciones normativas.
- Autorización para implementar OD-006, OD-008, OD-009, OD-010, OD-011 ni detalles aún abiertos de OD-012 u OD-013.
- Una regla de negocio fija de 30 minutos, una zona horaria aprobada, actualización cada 30 segundos, una matriz completa de cuatro roles o permisos para reintentar notificaciones.
- Decidir rutas, DTO o paginación de B00; siguen abiertos.

## Criterios de aceptación

- El cambio está identificado explícitamente como exclusivamente documental y es estructuralmente válido con `schema: spec-driven` y `skip_specs: true`.
- Cada aprobación vigente se limita a lo que `main`, ADR-004 y B00 aprueban realmente; los detalles no resueltos siguen abiertos.
- Se conservan las propuestas futuras y se identifican claramente como no normativas, sin insinuar autorización para implementarlas.
- No se presenta una preferencia de ventana como parte esperada del contrato del MVP ni se describen como aprobadas una ventana fija de 30 minutos, una zona horaria o una actualización cada 30 segundos.
- El documento no afirma que el contenido aceptado sea normativo ni que archivar este cambio documental vuelva vinculantes las decisiones abiertas.
