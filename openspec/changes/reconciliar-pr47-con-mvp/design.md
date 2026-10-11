# Diseño documental de la reconciliación PR #47

## Enfoque

La reconciliación separa tres planos: las capacidades aprobadas en main, la
fundación técnica reutilizable que todavía no se habilita y las propuestas de
PR que siguen pendientes. No crea una capacidad funcional duplicada ni trata
la persistencia de la demo como esquema completo del MVP.

## Base y compatibilidad

- La API operativa de turnos mantiene el `AgroFlowDbContext` SQLite y sus
  migraciones vigentes. ADR-006 define SQLite académico; WAL solo se habilita
  después de validarlo, y PostgreSQL es etapa futura previa a cliente real.
- Identity/JWT existe como andamiaje de main, pero no equivale a Identity MVP,
  contexto de ingenio ni seguridad completa para catálogos.
- El frontend se mantiene en React/Vite por ADR-005. La cola de turnos tiene
  integración HTTP de demo; otras vistas permanecen mock.
- No se publican contratos incompletos de datos maestros. La forma de rutas,
  DTO, permisos pendientes, asociación externa e idempotencia depende de B00,
  B02, B03, B05 y las decisiones propietarias.

## Esquema maestro y metadatos

Las entidades maestras contienen propiedades CLR `CreatedAtUtc` e
`InactivatedAtUtc`, pero las cuatro tablas SQLite de main no tienen esas
columnas. La corrección de backend ignorará explícitamente esas ocho propiedades
hasta que B02 acuerde y migre el esquema. Esta decisión de compatibilidad evita
que el modelo EF afirme persistencia inexistente; no implica descartar el diseño
futuro ni establece auditoría. Las propiedades no persistidas no pueden
utilizarse como evidencia de historial durable.

## Seguridad, asociaciones y auditoría

La seguridad efectiva permanece en Identity/JWT/backend y requiere completar
B03. No se selecciona arquitectura de sesión web. Una asociación activa única
por camión es regla de dominio vigente, pero su CRUD externo, DTO y permisos
siguen pendientes. La existencia de una entidad `AuditEvent` no satisface la
política de auditoría, eventos, actor, conservación y acceso de OD-009.

La entidad/servicio de idempotencia no aprueba un encabezado público, semántica
de reintentos ni clave obligatoria. OD-006 sigue abierto.

## Propuesta de normalización de WhatsApp

El delta de `datos-maestros` se conserva como propuesta revisable, no como
requisito consolidado. OD-013 deja pendientes campos y validaciones; no aplicar
la normalización de WhatsApp como comportamiento normativo sin aprobación
explícita. El acuerdo DNI solo dígitos sí es vigente y está en la spec canónica.

## Evidencia

Esta documentación no afirma build o suite backend verde. El estado comunicado
por el padre es 177 de 205 pruebas ejecutadas con 28 fallas y una prueba de
bloqueo excluida por colgar; los detalles y la corrección pertenecen a la unidad
backend, no a este cambio documental.
