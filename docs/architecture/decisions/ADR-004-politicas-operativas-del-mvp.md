# ADR-004: Políticas operativas y alcance de gestión del MVP

- Estado: aceptada para los acuerdos enumerados; los detalles no tratados siguen abiertos
- Fecha: 2026-09-25
- Alcance: prioridad, asignación de ventanas, asociaciones, datos de solicitud, catálogos y cola visual

## Contexto

El relevamiento académico estableció factores de prioridad, capacidad y entidades del dominio, pero dejó sin fijar su precedencia y varias operaciones concretas. El prototipo muestra una cola y una pantalla de Configuración que no son por sí mismas requisitos aprobados. Para planificar endpoints y una demostración integrada, el equipo eligió un conjunto mínimo de políticas expresas. Esta decisión complementa la jerarquía documental de [ADR-002](ADR-002-jerarquia-documental-y-openspec.md): el comportamiento verificable se expresa en OpenSpec, no en este ADR ni en el prototipo.

## Decisión

1. **Prioridad (`OD-001`).** Si varias solicitudes elegibles compiten por capacidad, se atiende primero la que acumula más tiempo desde el corte de la caña. A igualdad de ese tiempo, tiene precedencia la flota propia sobre la de terceros. Si persiste el empate, se atiende primero la solicitud registrada antes. Esta es una precedencia lexicográfica, sin pesos configurables en el MVP.
2. **Asociación (`OD-002`).** Cada camión tiene como máximo un transportista autorizado activo en un momento dado. La relación debe ser explícita y verificable por el backend; sus cambios no alteran la identidad ni las referencias históricas de turnos ya registrados.
3. **Ventana (`OD-003`).** El solicitante no elige una franja. El backend asigna automáticamente la próxima ventana compatible con disponibilidad, calendario y prioridad. Por tanto, la variante de «ventana solicitada» del antecedente `RN-016` no integra el MVP vigente.
4. **Configuración de capacidad (`OD-004`).** La duración y el cupo se definen por ingenio mediante datos de arranque reproducibles. La interfaz de Configuración no permite modificarlos en el MVP. Los datos demo usarán 30 minutos y 2 camiones por ventana; otras instalaciones pueden usar valores distintos sin cambiar el algoritmo.
5. **Solicitud (`OD-005`).** Se requiere identificar transportista, camión y finca, además de informar fecha y hora de corte y carga estimada. No se pide una ventana preferida.
6. **Gestión interna (`OD-007`, `OD-013`).** Operador y supervisor del ingenio pueden listar, crear, editar e inhabilitar transportistas, camiones y fincas; también pueden crear y cancelar turnos internos de su ingenio. El backend verifica el rol, la pertenencia y las reglas de negocio; ocultar controles en la interfaz no constituye autorización.
7. **Cola del dashboard (`OD-012`).** El MVP muestra las franjas, incluidas las vacías, y los cupos restantes a partir del estado que entrega la API para el ingenio autorizado. Los indicadores y la cola no se calculan desde arreglos simulados como fuente operativa.

## Aspectos no resueltos por este ADR

- Calendario y zona horaria de cada ingenio, límites y horizonte de búsqueda de ventanas (`OD-004`).
- Formatos y rangos válidos de fecha/hora de corte y carga estimada, incluida su unidad (`OD-005`).
- Concurrencia, idempotencia y comportamiento de reintentos (`OD-006`).
- Matriz de roles completa, gestión de asociaciones y campos editables de catálogos (`OD-007`, `OD-013`).
- Fórmulas de indicadores, fecha operativa, horizonte de la cola y cadencia de refresco (`OD-012`).

Estos detalles permanecen en el [registro de decisiones](../../planning/open-decisions.md). La ausencia de una respuesta no autoriza a implementarla implícitamente.

## Consecuencias

- La lógica de selección, prioridad, cupos y autorización se implementa una sola vez en el backend, conforme a [ADR-003](ADR-003-backend-fuente-de-verdad.md).
- El contrato de alta de turno no debe incorporar una ventana preferida para el MVP; frontend y n8n presentan la asignación persistida que devuelve la API.
- El modelo debe impedir dos asociaciones activas para el mismo camión y conservar el historial operativo sin borrados físicos de catálogos.
- El seed de demostración debe crear configuración por ingenio y permitir reproducir las franjas de 30 minutos con cupo 2, sin fijar esos valores en código de dominio.
- Las lecturas de cola requieren representar explícitamente capacidad disponible incluso cuando una franja no tenga turnos.
- Los cambios de comportamiento se tramitan por `openspec/changes/` y se reflejan en las specs, OpenAPI y pruebas afectadas antes de cerrar sus issues.
