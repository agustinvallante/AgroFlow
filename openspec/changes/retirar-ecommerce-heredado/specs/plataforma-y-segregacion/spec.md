## ADDED Requirements

### Requirement: Superficie HTTP exclusiva del dominio AgroFlow

La API de AgroFlow MUST NOT publicar operaciones ni contratos de productos, pedidos o clientes del comercio electrónico de origen. El retiro de esas rutas SHALL preservar los recorridos de turnos y salud vigentes en el perfil de demo local.

#### Scenario: Rutas heredadas ausentes

- **GIVEN** la API iniciada en el perfil de demo local
- **WHEN** se inspecciona Swagger o se solicitan las rutas de productos, pedidos y registro de clientes heredadas
- **THEN** las operaciones no aparecen en Swagger y las solicitudes no encuentran una ruta publicada

#### Scenario: Recorrido de turnos preservado

- **GIVEN** la API iniciada con la base y el seed de la demo local
- **WHEN** se consulta salud y se crea, lista o modifica un turno conforme al contrato vigente
- **THEN** el backend conserva el comportamiento y el estado persistido de esos recorridos
