# Mapa simulado para la presentación

El equipo autorizó el 2026-11-14 el uso de viajes ficticios para la exposición. Este cambio documenta el alcance de [PR #105](https://github.com/agustinvallante/AgroFlow/pull/105) y prepara su revisión. Se relaciona con [QA de presentación #111](https://github.com/agustinvallante/AgroFlow/issues/111) y el límite entre demo y MVP de [#75](https://github.com/agustinvallante/AgroFlow/issues/75).

## Resultado

La pestaña Mapa toma de AgroFlow API las fincas del catálogo y los turnos activos de la cola, agrega un viaje por turno sin intervención del operador, conserva campos de prueba para la demo y permite observar un recorrido estimado de finca a ingenio sobre una red vial simulada. Al tocar un camión muestra su detalle. Identifica como simulado lo que la API todavía no publica y lo conserva en el navegador para continuar el ensayo después de recargar.

La capacidad nueva [mapa-simulado](specs/mapa-simulado/spec.md) contiene los requisitos verificables. La [guía de demostración](../../../docs/development/mapa-simulado-presentacion.md) aporta datos de prueba, pasos y correspondencia con las pruebas existentes.

## Alcance autorizado

- Fincas del catálogo de la API (`GET /api/v1/farms`), ubicadas de forma aproximada alrededor del ingenio como medida temporal: la API no publica coordenadas.
- Ubicación de referencia del ingenio, modificable por el operador, hasta que la API la publique.
- Un viaje por turno activo de la cola (`GET /api/v1/appointments?active=true`), con salida estimada hacia atrás desde la ventana y llegada tomada de los estados de la API.
- Viajes de prueba, sólo para la demo, con patente, finca, velocidad constante, salida relativa y retraso simulado.
- Estados visuales Programado, En ruta, Demora y Llegó.
- Reloj real y aceleraciones x10 y x60 para el ensayo.
- Persistencia local y eliminación de viajes cargados.
- Representación SVG con un fondo de rutas, accesos y ríos simulados; cada recorrido sigue esos caminos.
- Camiones como círculos, fincas e ingenio como cuadrados, y un recuadro de detalle del camión: patente, estado, finca, horarios, distancia total y restante.

La autorización corresponde a una demostración independiente. Las lecturas operativas del dashboard siguen obteniéndose de AgroFlow API, según [ADR-003](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md) y [ADR-005](../../../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md).

## Límites y decisiones posteriores

El mapa dibuja finca → ingenio. El vínculo futuro con EN_CAMINO requiere su acuerdo de negocio porque el chatbot actual describe ese estado como salida hacia la finca. En esta entrega la simulación no crea turnos, no llama a sus transiciones y no informa llegadas al backend.

La integración con API o GPS, los caminos reales, las detenciones reportadas por WhatsApp, la reanudación y el impacto sobre ventanas requieren cambios posteriores de OpenSpec y contrato. La sugerencia de detenciones de la discusión de #105 queda como propuesta. El mapa no amplía los criterios obligatorios del MVP ni sustituye evidencia de los recorridos operativos.

## Superficies

| Área | Alcance |
|---|---|
| Frontend | Vista, formularios, reloj, persistencia, red vial y detalle del camión; lectura del catálogo de fincas y su ruta en el proxy |
| Especificación | Nueva capacidad acotada en este cambio |
| Documentación | Guía del mapa, README frontend, navegación y registro del acuerdo |
| HTTP y base de datos | Sólo lectura de `GET /api/v1/farms` y de los turnos activos, ya publicados; sin operaciones ni migraciones nuevas |

Las correcciones de cola y panel de #108/#109 siguen sus propias issues. Este cambio permanece disponible para revisión antes de consolidar la capacidad en las especificaciones vigentes.
