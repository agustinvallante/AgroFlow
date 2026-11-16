# Delta: Representación de la ventana en el dashboard

## ADDED Requirements

### Requirement: Ventana completa en la zona informada por la API

El frontend SHALL conservar inicio y fin RFC 3339 completos de la ventana y
SHALL mostrar fecha, hora y desplazamiento en fila y detalle. MUST NOT convertir
la ventana a la zona del navegador. Al combinar un resumen reciente con un
detalle cacheado, SHALL utilizar la ventana y el estado del resumen.

#### Scenario: Navegador de otra zona

- **GIVEN** una ventana que comienza cerca de medianoche con desplazamiento del ingenio
- **WHEN** el operador abre la cola o detalle desde un navegador en otra zona
- **THEN** ve la fecha, hora y desplazamiento recibidos y el filtro de fecha conserva el día del inicio informado

#### Scenario: Ventana que cruza la medianoche

- **GIVEN** una ventana cuyo inicio y fin pertenecen a días distintos
- **WHEN** se presenta la ventana
- **THEN** se identifican ambos días y sus horas, sin presentar el fin como perteneciente al día del inicio

#### Scenario: Cola diaria conservada

- **GIVEN** turnos de varias fechas
- **WHEN** se abre la cola sin fecha o se aplican sus filtros
- **THEN** se conserva el contrato diario existente sin agregar active=true, salvo la selección explícita de fecha después de un alta confirmada
