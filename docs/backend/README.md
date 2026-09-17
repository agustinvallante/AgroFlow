# Guías del backend

Aquí se documentarán aspectos exclusivos de la implementación de la API: ejecución local, persistencia, migraciones, estructura interna, configuración y despliegue.

Las reglas de negocio no se duplican aquí. Deben consultarse en `../../openspec/specs/`; el contrato HTTP vive en `../contracts/openapi.yaml` y las decisiones arquitectónicas en `../architecture/decisions/`.

## Estado actual

La solución .NET 8 es una base reutilizada de otro dominio. Debe renombrarse y reemplazar progresivamente productos, pedidos y clientes por capacidades de AgroFlow. El código heredado no constituye una decisión del modelo objetivo.
