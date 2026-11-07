# Frontend de AgroFlow

Aplicación web de AgroFlow, incorporada desde el prototipo
[AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard),
basada en React, TypeScript y Vite. La [ADR-005](../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)
confirma la continuidad de esta base para el MVP. Las propuestas anteriores de
Next.js y sus librerías asociadas no son decisiones vigentes; véanse los
[documentos históricos de frontend](../docs/architecture/frontend/frontend-architecture.md).

## Estado y alcance actual

El dashboard muestra **únicamente datos de la API**; no hay repositorios mock ni
datos de ejemplo. Alta, listado, filtros, detalle, transiciones y cancelación de
turnos usan las rutas de appointments publicadas en
[`docs/contracts/openapi.yaml`](../docs/contracts/openapi.yaml). Esto acredita
únicamente esa integración de demo, no todos los recorridos del MVP.

| Sección | Fuente |
|---|---|
| Cola de turnos | Turnos de la API. |
| Panel general | Camiones en curso de la API. Indicadores, franjas, chatbot, molienda y avisos no tienen endpoint: conservan su lugar, sin datos ni acciones. |
| Chatbot, transportistas, reportes y configuración | Conservan su diseño, sin datos y con las acciones deshabilitadas: el contrato no tiene operaciones para ellas. |

No hay requisito de conectar todas las pantallas ahora. El backend sigue siendo
autoridad sobre reglas y estado.

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

Copiar `.env.example` como `.env` y configurar `VITE_API_URL`; reiniciar Vite
después de cambiarlo. El dashboard necesita la API en ejecución. Vite reenvía
`/api/...` a la URL configurada durante `dev` y `preview`; servir `dist/` desde
otro servidor requiere su propio proxy.

## Organización actual

El código existente está organizado en `domain/`, `application/`,
`infrastructure/`, `composition/`, `presentation/` y `shared/`.
`HttpTurnoRepository` y sus DTOs/mappers consumen appointments. Esta estructura
vigente se describe aquí como inventario, no como una regla para rehacerla ni
como autorización para sumar dependencias.

La frontera de dependencias actual es:

```text
presentation → application → domain ← infrastructure
```

`composition/container.ts` conecta las implementaciones concretas. Ninguna capa
frontend debe decidir prioridad, capacidad, autorizaciones o transiciones. Cuando
el contrato incorpore otra capacidad, se agrega su repositorio HTTP y se conecta
la vista que hoy se muestra vacía.

El registro de la integración de la demo está en
[`frontend/docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md); lo
que describe sobre el modo mock quedó sin efecto.
