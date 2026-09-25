# Guías del backend

Aquí se documentarán aspectos exclusivos de la implementación de la API: ejecución local, persistencia, migraciones, estructura interna, configuración y despliegue.

Las reglas de negocio no se duplican aquí. Deben consultarse en `../../openspec/specs/`; el contrato HTTP vive en `../contracts/openapi.yaml` y las decisiones arquitectónicas en `../architecture/decisions/`.

## Estado actual

La solución .NET 8 es una base reutilizada de otro dominio. Debe renombrarse y reemplazar progresivamente productos, pedidos y clientes por capacidades de AgroFlow. El código heredado no constituye una decisión del modelo objetivo.

El [backlog del backend](https://github.com/users/agustinvallante/projects/2) distingue la base compartida (B00–B05), las tareas por endpoint y la integración n8n. Antes de tomar un endpoint, comprobar sus dependencias: B01 renombra y retira el dominio heredado; B02 es dueño del modelo y de la migración inicial; B03–B05 proveen identidad, errores, pruebas, transacciones e idempotencia compartidas. Los nombres de rutas y archivos futuros de las issues son propuestas hasta que B00 apruebe el contrato OpenAPI.
