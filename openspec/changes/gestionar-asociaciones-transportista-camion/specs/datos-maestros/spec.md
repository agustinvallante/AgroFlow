# Delta: Datos maestros

## ADDED Requirements

### Requirement: Gestión de asociaciones en la demo local

En `LocalDemo`, ZafraFlow SHALL publicar, protegidas por la clave del dashboard, operaciones para listar asociaciones transportista-camión, asociar un camión a un transportista e inhabilitar una asociación. Asociar MUST exigir un transportista y un camión existentes en el ingenio y habilitados, y MUST rechazar con `409 DUPLICATE_ASSOCIATION` un camión que ya tiene un transportista activo, sin cambiar la autorización vigente. Un transportista SHALL poder tener varios camiones activos. Inhabilitar una asociación MUST conservarla como historial y MUST NOT modificar turnos existentes. Volver a asociar un par con una asociación inactiva SHALL reactivar esa asociación. Las mutaciones MUST exigir `Idempotency-Key`. Las operaciones MUST NOT existir fuera de `LocalDemo`.

#### Scenario: Asociar un camión libre

- **GIVEN** un transportista habilitado sin camiones y un camión habilitado sin transportista activo
- **WHEN** el dashboard los asocia
- **THEN** la API responde `201` y el transportista queda con ese camión y puede solicitar turnos

#### Scenario: Camión con transportista activo

- **GIVEN** un camión asociado a un transportista activo
- **WHEN** el dashboard intenta asociarlo a otro transportista
- **THEN** la API responde `409 DUPLICATE_ASSOCIATION` y la asociación vigente no cambia

#### Scenario: Varios camiones para un transportista

- **GIVEN** un transportista habilitado y dos camiones libres
- **WHEN** el dashboard asocia ambos camiones
- **THEN** el transportista queda con los dos camiones activos

#### Scenario: Desasociar conserva el historial

- **GIVEN** una asociación activa
- **WHEN** el dashboard la inhabilita
- **THEN** la asociación queda inactiva en el listado, el transportista deja de poder pedir turnos con ese camión y el camión puede asociarse a otro transportista

#### Scenario: Volver a asociar el mismo par

- **GIVEN** una asociación inactiva entre un transportista y un camión habilitados
- **WHEN** el dashboard vuelve a asociarlos
- **THEN** la API reactiva la misma asociación y devuelve su identificador

#### Scenario: Recurso inexistente o inhabilitado

- **GIVEN** un transportista o camión inexistente o inhabilitado, o una asociación inexistente
- **WHEN** el dashboard asocia o desasocia
- **THEN** la API responde `404 ASSOCIATION_REFERENCE_NOT_FOUND` sin modificar datos

#### Scenario: Solicitud inválida

- **GIVEN** una mutación sin `Idempotency-Key`, con identificadores vacíos, con campos no documentados o con un identificador de ruta inválido
- **WHEN** el dashboard la envía
- **THEN** la API responde `400 VALIDATION_ERROR` sin modificar datos

#### Scenario: Sin clave del dashboard

- **GIVEN** una solicitud sin la clave del dashboard
- **WHEN** se listan o modifican asociaciones
- **THEN** la API responde `401` sin datos
