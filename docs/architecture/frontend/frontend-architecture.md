# Arquitectura frontend: estado y propuestas heredadas

> **Estado: documento de contexto, no arquitectura normativa.** La aplicación
> vigente usa React, TypeScript y Vite conforme a [ADR-005](../decisions/ADR-005-continuidad-react-vite-para-el-mvp.md).
> Esta página conserva ideas de PR #47 para evaluación, no fija framework,
> librerías, patrón de sesión ni estructura futura.

## Implementación actual

`frontend/` contiene una aplicación React/Vite. La cola de turnos puede operar
contra HTTP mediante `ApiClient`, DTOs/mappers y `HttpTurnoRepository`; existen
casos de uso, hooks, vistas y pruebas. Solo turnos consume HTTP. Panel general,
chatbot, transportistas, reportes y configuración permanecen en mock. El
[README del frontend](../../../frontend/README.md) describe el alcance presente.

Esta base se conserva; no hay requerimiento de sustituirla ni de implementar
ahora todas las pantallas. Los datos simulados deben distinguirse y nunca
presentarse como estado operativo aceptado.

## Límites vigentes

- OpenSpec define comportamiento; OpenAPI publica solo el contrato activo
  aprobado; ADRs registran decisiones de arquitectura.
- El backend conserva las reglas operativas, autorización efectiva, aislamiento
  por ingenio y estado persistido.
- Una nueva integración espera contrato aprobado y decisiones propietarias.
- Ninguna arquitectura frontend crea rutas, DTO, seguridad, permisos o reglas
  de negocio por inferencia.

## Ideas de PR #47 conservadas para evaluación

La propuesta original describía Next.js/App Router, organización feature-first,
TanStack Query, Zustand y mediación BFF. Ninguna de esas tecnologías o patrones
se adopta por este documento. Los fundamentos reutilizables —separación de
responsabilidades, consumo contract-first, manejo visible de carga/error y
pruebas— pueden evaluarse contra la aplicación React/Vite existente sin imponer
librerías ni reorganización.

La custodia de sesión web permanece sin decisión. No se elige BFF, cookie,
token administrado por navegador ni sesión directa del backend hasta acordar
contrato, amenazas, operación y alternativas.

## Próxima revisión

Antes de cambiar comportamiento, identificar la capacidad OpenSpec propietaria,
resolver las decisiones que la bloquean y actualizar contrato, pruebas y
 documentación junto con el cambio. Estas notas históricas no sustituyen ni
 duplican las especificaciones vigentes.
