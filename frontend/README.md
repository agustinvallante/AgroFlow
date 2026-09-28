# AgroFlow — Panel del Ingenio (frontend)

Prototipo de frontend para el panel de administración de AgroFlow, construido
con **React + TypeScript + Vite** y organizado en **Clean Architecture**.

Actualmente funciona **sin conexión a un backend real**: toda la información
(turnos, transportistas, conversaciones del chatbot, reportes, configuración)
vive en repositorios "mock" en memoria, con latencia simulada, para que la UI
se comporte igual que el día en que exista una API real detrás.

## Cómo correrlo

```bash
npm install
npm run dev       # entorno de desarrollo
npm run build     # build de producción (queda en dist/)
npm run preview   # sirve el build de producción localmente
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
│   └── mock/                  Hoy: repositorios en memoria (sin backend).
│       ├── data/               Datos semilla (fincas, choferes, turnos, etc).
│       └── repositories/       MockTurnoRepository, MockTransportistaRepository, etc.
│                               Mañana: acá se agregaría infrastructure/http/
│                               con HttpTurnoRepository, HttpTransportistaRepository...
│                               implementando las MISMAS interfaces de domain/repositories.
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

## Integración para la demo local

La integración mínima con AgroFlow API está definida en
[`docs/LOCAL_DEMO_INTEGRATION.md`](docs/LOCAL_DEMO_INTEGRATION.md). Para el lunes
se conserva React/Vite y se conecta únicamente el flujo de turnos; autenticación,
CRUD de datos maestros, interrupciones, mapa, reportes y despliegue quedan fuera.

### Configuración

Copiar `.env.example` como `.env` y elegir la fuente de datos:

```env
VITE_DATA_SOURCE=http          # mock = respaldo en memoria, sin backend
VITE_API_URL=http://localhost:5000
```

Reiniciar `npm run dev` después de cambiar el `.env`.

### Qué consume de la API (`VITE_DATA_SOURCE=http`)

`src/infrastructure/http/repositories/HttpTurnoRepository.ts` implementa el
puerto `TurnoRepository` contra `docs/contracts/openapi.yaml` del repositorio
AgroFlow:

| Acción del dashboard | Operación |
|---|---|
| Alta manual (teléfono, patente, código de finca, corte, carga) | `POST /api/v1/appointments` |
| Cola, filtros de estado/fecha/patente/teléfono y polling cada 4 s | `GET /api/v1/appointments` |
| Detalle (clic en una fila) | `GET /api/v1/appointments/{id}` |
| Avanzar / Cancelar | `POST /api/v1/appointments/{id}/transitions` |

El dashboard no calcula prioridad, ventana ni capacidad. Ante un error
(`400`, `404`, `409`, `500` o sin conexión) muestra el mensaje y recarga el
estado vigente. Panel general (métricas, timeline, molienda), chatbot,
transportistas, reportes y configuración siguen en mock y se identifican como
datos de demostración.

## Convenciones de la UI

- Los estilos siguen los tokens de diseño definidos en
  `src/presentation/styles/tokens.css` (paleta agroindustrial: verde caña,
  ámbar azúcar, papel crema) y la tipografía Fraunces (títulos) + IBM Plex
  Sans/Mono (datos).
- Cada vista tiene su propio hook en `presentation/hooks/`, que centraliza el
  estado de carga, error y las acciones (crear, reasignar, cancelar, etc).
