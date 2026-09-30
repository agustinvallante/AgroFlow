# Frontend de AgroFlow

Aplicación web de AgroFlow, incorporada desde el prototipo
[AgroFlow-Dashboard](https://github.com/GabrielBurieque/AgroFlow-Dashboard).

Antes de implementar una pantalla o flujo, consultar:

- [especificaciones vigentes](../openspec/specs/);
- [documentación específica del frontend](../docs/frontend/README.md);
- [contratos compartidos](../docs/contracts/README.md);
- [catálogo de casos de uso](../docs/product/use-case-catalog.md).

El frontend no debe recrear reglas de asignación, prioridad o transiciones de
estado. Esas decisiones pertenecen al backend y deben consumirse mediante
contratos explícitos.

---

## Panel del Ingenio

Prototipo de frontend para el panel de administración de AgroFlow, construido
con **React + TypeScript + Vite** y organizado en **Clean Architecture**.

La cola de turnos puede consumir AgroFlow API (`VITE_DATA_SOURCE=http`) o
repositorios "mock" en memoria (`VITE_DATA_SOURCE=mock`, sin backend). El resto
de las vistas (panel general, chatbot, transportistas, reportes y
configuración) usa mocks y se identifica como datos de demostración.

## Cómo correrlo

```bash
npm install
npm run dev       # entorno de desarrollo
npm run build     # build de producción (queda en dist/)
npm run preview   # sirve el build de producción localmente
npm test          # tests (Vitest + Testing Library)
npm run typecheck # verificación de tipos
npm run lint      # oxlint
```

## Estructura de carpetas (Clean Architecture)

```
src/
├── domain/                  → Entidades y contratos (puertos). No depende de nada.
│   ├── entities/             Turno, Transportista, Conversacion, MetricasPanel, Reporte, Configuracion
│   └── repositories/         Interfaces: TurnoRepository, TransportistaRepository, etc.
│
├── application/              → Casos de uso. Orquestan reglas de negocio usando los puertos.
│   └── usecases/              Uno por acción: ObtenerTurnosDelDia, CrearTurnoManual,
│                               AlternarEstadoMolienda, ObtenerReporte, etc.
│
├── infrastructure/            → Implementaciones concretas de los puertos.
│   ├── http/                  Cliente, DTOs, mappers y HttpTurnoRepository contra
│   │                           AgroFlow API (docs/contracts/openapi.yaml).
│   └── mock/                  Repositorios en memoria (sin backend).
│       ├── data/               Datos semilla (fincas, choferes, turnos, etc).
│       └── repositories/       MockTurnoRepository, MockTransportistaRepository, etc.
│
├── composition/
│   └── container.ts           Raíz de composición: ÚNICO archivo que decide qué
│                               implementación de cada repositorio se usa. Es el
│                               punto donde se conectaría el backend real.
│
├── presentation/               → React. Todo lo visual.
│   ├── layout/                 Sidebar, definición de vistas
│   ├── views/                  Una por sección: PanelGeneralView, ColaTurnosView,
│                                ChatbotView, TransportistasView, ReportesView,
│                                ConfiguracionView
│   ├── components/             Piezas reutilizables (badges, tablas, gráficos, etc)
│   ├── hooks/                  usePanel, useColaTurnos, useTransportistas, useChatbot,
│                                useReportes, useConfiguracion — conectan React con
│                                los casos de uso a través del container
│   └── styles/                 Tokens de diseño (variables CSS) y hoja global
│
└── shared/
    └── utils/                  Utilidades sin lógica de negocio (fechas, delay, ids)
```

## La regla de dependencia

```
presentation  →  application  →  domain  ←  infrastructure
```

- `domain` no importa nada de las otras capas: son las reglas del negocio y los
  contratos (interfaces) puros.
- `application` (casos de uso) sólo conoce las interfaces de `domain`, nunca una
  implementación concreta.
- `infrastructure` implementa esas interfaces (hoy con mocks en memoria).
- `presentation` (componentes y hooks de React) sólo llama a los casos de uso
  a través de `composition/container.ts`; no sabe si los datos vienen de un
  mock o de una API real.

## Integración existente de la demo local

La integración mínima con AgroFlow API está definida en
[`docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md). Ese perfil
conecta únicamente el flujo de turnos, sin autenticación, CRUD de datos maestros
ni interrupciones. Estas exclusiones no reducen el [MVP](../docs/product/mvp-acceptance.md):
la [ADR-005](../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md)
mantiene React/Vite para ampliar gradualmente la aplicación. El
[replanificación tras la demo](../docs/planning/mvp-replan-2026-09-29.md) enumera lo pendiente.

### Configuración

Copiar `.env.example` como `.env` y elegir la fuente de datos:

```env
VITE_DATA_SOURCE=http          # mock = respaldo en memoria, sin backend
VITE_API_URL=http://localhost:5000
```

Reiniciar `npm run dev` después de cambiar el `.env`.

El navegador no llama directo a la API: pide `/api/...` a Vite, que lo
reenvía a `VITE_API_URL` (ver `vite.config.ts`). Por eso no depende de la
política CORS del backend ni de que Vite abra en el puerto 5173. Si la API
arrancó en otro puerto (por ejemplo `5142`, sin `--urls`), basta con cambiar
`VITE_API_URL` y reiniciar. El proxy funciona con `npm run dev` y
`npm run preview`; servir `dist/` desde otro servidor requiere su propio proxy.

### Qué consume de la API (`VITE_DATA_SOURCE=http`)

`src/infrastructure/http/repositories/HttpTurnoRepository.ts` implementa el
puerto `TurnoRepository` contra `docs/contracts/openapi.yaml` del repositorio
AgroFlow:

| Acción del dashboard | Operación |
|---|---|
| Alta manual (teléfono, patente, código de finca, corte, carga) | `POST /api/v1/appointments` |
| Cola, filtros de estado/fecha/patente/teléfono y polling cada 4 s | `GET /api/v1/appointments` |
| Detalle (clic en una fila) | `GET /api/v1/appointments/{id}` |
| Cambiar estado (elegido por el operador) / Cancelar | `POST /api/v1/appointments/{id}/transitions` |

El dashboard no calcula prioridad, ventana, capacidad ni transiciones válidas:
el operador elige el estado y la API lo valida (`409` si no corresponde). Ante
un error (`400`, `404`, `409`, `500` o sin conexión) muestra el mensaje y
recarga el estado vigente. Los filtros incompletos, los fallos de carga, los
datos desactualizados y la zona horaria se describen en
[`docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md). Panel general (métricas, timeline, molienda), chatbot,
transportistas, reportes y configuración siguen en mock y se identifican como
datos de demostración.

## Convenciones de la UI

- Los estilos siguen los tokens de diseño definidos en
  `src/presentation/styles/tokens.css` (paleta agroindustrial: verde caña,
  ámbar azúcar, papel crema) y la tipografía Fraunces (títulos) + IBM Plex
  Sans/Mono (datos).
- Cada vista tiene su propio hook en `presentation/hooks/`, que centraliza el
  estado de carga, error y las acciones (crear, reasignar, cancelar, etc).
