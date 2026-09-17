# AgroFlow

Proyecto académico de **Administración de Sistemas** para coordinar turnos y operaciones logísticas de la zafra azucarera. El objetivo es convertir el prototipo visual en una aplicación funcional con una API .NET, un frontend web y una integración controlada con WhatsApp mediante n8n.

## Organización del repositorio

```text
AgroFlow/
|- openspec/   Especificaciones normativas y cambios propuestos
|- docs/       Producto, arquitectura, contratos y guías
|- backend/    API y persistencia
`- frontend/   Aplicación web
```

OpenSpec no reemplaza las carpetas de código. `backend/` y `frontend/` permanecen separados para que cada equipo trabaje con claridad, mientras `openspec/specs/` define el comportamiento común que ambos deben respetar.

## Fuente de verdad

La jerarquía documental del proyecto es:

1. [`openspec/specs/`](openspec/specs/) para comportamiento y criterios verificables.
2. [`docs/contracts/`](docs/contracts/) para contratos técnicos compartidos.
3. [`docs/architecture/decisions/`](docs/architecture/decisions/) para decisiones y sus motivos.
4. [`docs/`](docs/) para contexto, diseño, planificación y guías.
5. Los README de cada componente para instalación y operación local.

Los documentos académicos originales son fuentes históricas. Una vez incorporado un requisito a OpenSpec, la especificación versionada es la referencia vigente.

## Cómo empezar

- Leer la [guía de incorporación](docs/onboarding/getting-started.md).
- Consultar la [visión y alcance del MVP](docs/product/vision-and-scope.md).
- Revisar el [catálogo de casos de uso](docs/product/use-case-catalog.md).
- Consultar la [matriz de 61 reglas de negocio](docs/product/business-rules.md).
- Resolver las [decisiones abiertas](docs/planning/open-decisions.md) antes de implementar los puntos bloqueados.
- Leer el [flujo de trabajo del equipo](docs/development/team-workflow.md).
- Antes de cambiar comportamiento, crear un cambio en `openspec/changes/`.

## Estado técnico

- El backend parte de una copia independiente de [ICS2026-backend](https://github.com/agustinvallante/ICS2026-backend), commit `dc40f7cfa515a1425f8709c73ae917e9c004266a`.
- El frontend se integrará desde [AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard) una vez estabilizado el contrato inicial.
- El backend heredado todavía conserva nombres y módulos del e-commerce original; no representa aún el dominio objetivo de AgroFlow.

## Seguridad

No se deben versionar claves, credenciales ni datos personales. La clave JWT debe configurarse mediante variables de entorno o secretos de usuario de .NET.
