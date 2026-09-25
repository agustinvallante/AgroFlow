## ADDED Requirements

### Requirement: Solicitud conversacional sin elección de franja

n8n SHALL reunir el momento de corte y la carga estimada además de los datos requeridos para identificar al transportista, camión y finca cuando recibe una solicitud de turno. MUST NOT pedir una ventana preferida para el MVP y SHALL comunicar la ventana asignada por el backend únicamente después de su confirmación.

#### Scenario: Solicitud válida desde WhatsApp

- **GIVEN** un transportista identificado y un camión asociado
- **WHEN** comunica una finca, un momento de corte y una carga estimada válidos
- **THEN** n8n solicita el turno a la API sin elegir la franja y comunica la asignación persistida que recibe
