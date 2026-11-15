# Mapa simulado para la presentación

El equipo autorizó el 2026-11-14 el uso de viajes ficticios para la exposición. Este cambio documenta el alcance de [PR #105](https://github.com/agustinvallante/AgroFlow/pull/105) y prepara su revisión. Se relaciona con [QA de presentación #111](https://github.com/agustinvallante/AgroFlow/issues/111) y el límite entre demo y MVP de [#75](https://github.com/agustinvallante/AgroFlow/issues/75).

## Resultado

La pestaña Mapa permite cargar un ingenio, fincas y viajes ficticios y observar un recorrido estimado de finca a ingenio. Identifica los datos como simulados y los conserva en el navegador para continuar el ensayo después de recargar.

La capacidad nueva [mapa-simulado](specs/mapa-simulado/spec.md) contiene los requisitos verificables. La [guía de demostración](../../../docs/development/mapa-simulado-presentacion.md) aporta datos de prueba, pasos y correspondencia con las pruebas existentes.

## Alcance autorizado

- Datos manuales de nombre y coordenadas del ingenio y las fincas.
- Viajes con patente, finca, velocidad constante, salida relativa y retraso simulado.
- Estados visuales Programado, En ruta, Demora y Llegó.
- Reloj real y aceleraciones x10 y x60 para el ensayo.
- Persistencia local y eliminación de fincas y viajes cargados.
- Representación SVG con trayectorias ilustrativas y fondo decorativo.

La autorización corresponde a una demostración independiente. Las lecturas operativas del dashboard siguen obteniéndose de AgroFlow API, según [ADR-003](../../../docs/architecture/decisions/ADR-003-backend-fuente-de-verdad.md) y [ADR-005](../../../docs/architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md).

## Límites y decisiones posteriores

El mapa dibuja finca → ingenio. El vínculo futuro con EN_CAMINO requiere su acuerdo de negocio porque el chatbot actual describe ese estado como salida hacia la finca. En esta entrega la simulación no crea turnos, no llama a sus transiciones y no informa llegadas al backend.

La integración con API o GPS, los caminos reales, las detenciones reportadas por WhatsApp, la reanudación y el impacto sobre ventanas requieren cambios posteriores de OpenSpec y contrato. La sugerencia de detenciones de la discusión de #105 queda como propuesta. El mapa no amplía los criterios obligatorios del MVP ni sustituye evidencia de los recorridos operativos.

## Superficies

| Área | Alcance |
|---|---|
| Frontend | Vista, formularios, reloj y persistencia del mapa ya presentes en #105 |
| Especificación | Nueva capacidad acotada en este cambio |
| Documentación | Guía del mapa, README frontend, navegación y registro del acuerdo |
| HTTP y base de datos | Sin operaciones ni migraciones nuevas |

Las correcciones de cola y panel de #108/#109 siguen sus propias issues. Este cambio permanece disponible para revisión antes de consolidar la capacidad en las especificaciones vigentes.
