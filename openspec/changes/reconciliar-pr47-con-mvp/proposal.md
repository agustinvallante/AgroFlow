# Reconciliar la base de PR #47 con el MVP vigente

## Objetivo y estado

Este cambio documenta la integración de ideas útiles de PR #47 con la estructura
AgroFlow de main y la guía actualizada. La opción autorizada para esta entrega es
**fundación compatible, todavía no habilitada**: se conserva una base técnica
para revisar y probar, pero no se publica CRUD de datos maestros ni se declara
lista una API MVP.

El cambio aún no está cerrado ni acredita su aceptación. Las especificaciones
canónicas y ADR-004/005/006 mantienen su autoridad. Cualquier comportamiento
nuevo pendiente de aprobación permanece como propuesta en este cambio, no como
política operativa adoptada.

## Fundamento recuperable y límites

Se preservan servicios, pruebas y estructura compatibles con AgroFlow y SQLite,
y se mantiene el modelo/las migraciones existentes como única persistencia
operativa. Identity/JWT de main continúa como andamiaje; B00, B02, B03 y B05 son
dependencias para habilitar la fundación como producto.

Los campos de metadatos `CreatedAtUtc` e `InactivatedAtUtc` de cuatro entidades
maestras no existen en las migraciones SQLite de main. La corrección de backend
ignora explícitamente esos ocho campos no migrados en el modelo vivo. Por tanto,
son metadatos **no persistentes** y no forman parte de contrato ni de una
capacidad activa hasta que B02 defina y migre el esquema. No se debe afirmar
historial durable de creación/inhabilitación a partir de propiedades CLR.

La propuesta de normalización de WhatsApp en el delta de datos maestros queda
pendiente de revisión OD-013; no se presenta como acuerdo aprobado por la guía.

## Fuera de alcance

- Exponer rutas, DTO, seguridad, paginación o CRUD de catálogos en OpenAPI.
- Aprobar una política pública de Idempotency-Key, auditoría completa o permisos
  de asociación/capacidad.
- Completar el MVP, declarar B38/G01–G12 terminados o atribuir a metadatos no
  migrados persistencia durable.
- Modificar ADRs u otras decisiones pendientes por inferencia.

## Aceptación documental

- [ ] Documentar el fundamento como compatible pero no habilitado.
- [ ] Mantener maestras fuera del OpenAPI activo hasta que se aprueben B00/B02/B03/B05.
- [ ] Distinguir propiedades no persistentes de columnas migradas.
- [ ] Mantener normalización de WhatsApp como propuesta pendiente si no hay acuerdo explícito.
- [ ] Registrar solo validaciones observadas; no afirmar pruebas backend aprobadas.
