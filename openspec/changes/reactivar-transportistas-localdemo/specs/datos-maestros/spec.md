## ADDED Requirements

### Requirement: Reactivación de transportistas en LocalDemo

LocalDemo SHALL permitir reactivar un transportista existente desde el
dashboard mediante una mutación con clave del dashboard e idempotencia.
La reactivación SHALL conservar el identificador y las referencias
históricas, aplicar la unicidad de DNI y WhatsApp, y registrar la auditoría.
El dashboard SHALL mostrar el estado persistido que devuelve la API al releer.

#### Scenario: Reactivar con asociación vigente

- **GIVEN** un transportista inactivo con una asociación vigente a un camión activo
- **WHEN** el operador pulsa Reactivar y la API confirma la operación
- **THEN** el mismo registro vuelve a estar activo y puede volver a solicitar turnos con ese camión según las reglas vigentes

#### Scenario: Reactivar sin asociación vigente

- **GIVEN** un transportista inactivo sin asociación vigente a un camión activo
- **WHEN** el operador lo reactiva
- **THEN** el registro queda activo, sin crear una asociación ni habilitar nuevas solicitudes de turno

#### Scenario: Reintentar una reactivación confirmada

- **GIVEN** una reactivación confirmada por la API
- **WHEN** el cliente repite la misma solicitud con la misma clave de idempotencia
- **THEN** la API devuelve el mismo recibo y conserva una sola entrada de auditoría para esa solicitud
