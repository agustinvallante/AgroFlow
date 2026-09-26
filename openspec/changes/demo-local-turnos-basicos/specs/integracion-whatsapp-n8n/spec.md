# Delta: Integración WhatsApp y n8n

## ADDED Requirements

### Requirement: Chatbot local limitado a la API canónica

Para la demo local, n8n SHALL permitir solicitar un turno, consultar turnos e informar `EN_CAMINO` usando exclusivamente las operaciones de OpenAPI. Para el alta SHALL enviar el teléfono del remitente como `carrierPhone` junto con `truckPlate` y `farmCode`; para CU-003 MAY consultar con el filtro `phone`. n8n MUST NOT consultar endpoints de datos maestros, fijar UUIDs en el workflow, asignar ventanas, decidir transiciones, conservar un estado paralelo ni comunicar éxito antes de recibir la respuesta confirmada del backend.

#### Scenario: Solicitud desde el chatbot local

- **GIVEN** un mensaje entrante cuyo teléfono identifica a un transportista del seed y la conversación reunió una patente, un código de finca, corte y carga estimada válidos
- **WHEN** n8n invoca el alta canónica con `carrierPhone`, `truckPlate` y `farmCode`
- **THEN** comunica la ventana y el estado devueltos por la API

#### Scenario: Consulta desde el chatbot local

- **GIVEN** el teléfono del mensaje entrante y, opcionalmente, una patente o un identificador de turno provisto durante la demo
- **WHEN** n8n invoca el listado con `phone` o la consulta de detalle correspondiente
- **THEN** comunica el estado vigente recibido sin modificarlo

#### Scenario: Aviso EN_CAMINO

- **GIVEN** un turno `ASIGNADO`
- **WHEN** el chatbot solicita la transición a `EN_CAMINO`
- **THEN** comunica el nuevo estado sólo si la API lo persiste y confirma

### Requirement: Canal local sin equivalencia productiva

La demo SHALL poder usar el chat de prueba de n8n u otro adaptador local reproducible y MUST NOT presentarlo como integración verificada con WhatsApp real. La configuración SHALL mantener la URL base de la API fuera de las reglas del workflow.

#### Scenario: n8n ejecutado en Docker

- **GIVEN** n8n dentro de un contenedor y la API en el host
- **WHEN** se configura la URL base local
- **THEN** el workflow usa `host.docker.internal` o el mapeo equivalente documentado, sin cambiar el contrato ni duplicar lógica
