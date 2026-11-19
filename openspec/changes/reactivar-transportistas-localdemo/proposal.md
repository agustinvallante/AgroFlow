# Reactivar transportistas en LocalDemo

## Why

Durante los ensayos, una baja deja inutilizable un registro existente. Los
identificadores únicos impiden volver a darlo de alta con los mismos datos.

## What Changes

- Publicar la reactivación en [OpenAPI](../../../docs/contracts/openapi.yaml).
- Exponer Reactivar en las filas inactivas del dashboard de LocalDemo.
- Reutilizar la transición existente de datos maestros, con idempotencia y
  auditoría. Los turnos siguen sujetos a una asociación vigente.

## Impact

Capacidad existente `datos-maestros`. Entrega acotada al perfil LocalDemo.
La purga selectiva de datos personales sigue pendiente en [OD-009](../../../docs/planning/open-decisions.md).
