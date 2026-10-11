# Frontend de AgroFlow

Aplicación web de AgroFlow, incorporada desde el prototipo
[AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard),
basada en React, TypeScript y Vite. La [ADR-005](../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)
confirma la continuidad de esta base para el MVP. Las propuestas anteriores de
Next.js y sus librerías asociadas no son decisiones vigentes; véanse los
[documentos históricos de frontend](../docs/architecture/frontend/frontend-architecture.md).

## Estado y alcance actual

La cola de turnos puede consumir la API de la demo local (`VITE_DATA_SOURCE=http`)
o repositorios en memoria (`VITE_DATA_SOURCE=mock`). En modo HTTP, alta, listado,
filtros, detalle, transiciones y cancelación usan las rutas de appointments
publicadas en [`docs/contracts/openapi.yaml`](../docs/contracts/openapi.yaml).
Esto acredita únicamente esa integración de demo, no todos los recorridos del MVP.

Panel general, chatbot, transportistas, reportes y configuración todavía usan
mocks y deben identificarse como datos de demostración. No hay requisito de
conectar todas las pantallas ahora. El mock no es estado operativo ni evidencia
de aceptación MVP. El backend sigue siendo autoridad sobre reglas y estado.

Antes de ampliar un flujo, consultar las
[especificaciones vigentes](../openspec/specs/), los
[contratos compartidos](../docs/contracts/README.md), las decisiones técnicas
aceptadas y las [decisiones abiertas](../docs/planning/open-decisions.md). La
integración de datos maestros, autenticación web y demás recorridos espera sus
contratos y dependencias aprobados; sus propuestas documentales no publican
rutas utilizables.

## Cómo correrlo

```bash
npm ci
npm run dev       # entorno de desarrollo
npm run build     # build de producción (queda en dist/)
npm run preview   # sirve el build de producción localmente
npm test          # tests (Vitest + Testing Library)
npm run typecheck # verificación de tipos
npm run lint      # oxlint
```

Copiar `.env.example` como `.env`, elegir `VITE_DATA_SOURCE=http` o `mock` y
configurar `VITE_API_URL`; reiniciar Vite después de cambiarlo. Vite reenvía
`/api/...` a la URL configurada durante `dev` y `preview`; servir `dist/` desde
otro servidor requiere su propio proxy.

## Organización actual

El código existente está organizado en `domain/`, `application/`,
`infrastructure/`, `composition/`, `presentation/` y `shared/`. En HTTP, el
`HttpTurnoRepository` y sus DTOs/mappers consumen appointments; los demás
repositorios de dashboard y catálogo siguen siendo mocks. Esta estructura
vigente se describe aquí como inventario, no como una regla para rehacerla ni
como autorización para sumar dependencias.

La frontera de dependencias actual es:

```text
presentation → application → domain ← infrastructure
```

`composition/container.ts` selecciona implementaciones HTTP o mock. Ninguna
capa frontend debe decidir prioridad, capacidad, autorizaciones o transiciones.
El detalle de la integración de la demo está en
[`frontend/docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md).
