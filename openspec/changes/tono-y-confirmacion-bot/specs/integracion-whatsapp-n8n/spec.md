# Delta: Integración WhatsApp y n8n

## ADDED Requirements

### Requirement: Confirmación explícita antes de registrar un incidente o un turno

Antes de reportar un incidente en ruta, el asistente SHALL leer al transportista la patente, el tipo de problema, si el camión puede seguir viaje y la demora estimada, y SHALL reportarlo sólo cuando el transportista responda a esa lectura con un sí explícito en un mensaje posterior. Los datos informados en el mismo mensaje del aviso MUST NOT tomarse como confirmación. El alta de un turno SHALL seguir la misma regla: lectura de los cuatro datos y sí explícito en un mensaje posterior. Para avisar que se resolvió un problema o que el camión llegó al ingenio, el asistente SHALL confirmar la patente sólo cuando el transportista tenga más de un turno activo.

#### Scenario: Datos completos en el mismo mensaje

- **GIVEN** un transportista con un turno `EN_CAMINO`
- **WHEN** escribe "no puede seguir, 30 minutos de demora" después de avisar un accidente sin heridos
- **THEN** el asistente lee la patente, el tipo, si puede seguir y la demora, pregunta si lo registra y no reporta el incidente todavía

#### Scenario: Sí posterior

- **GIVEN** que el asistente leyó los datos del incidente
- **WHEN** el transportista responde "Sí"
- **THEN** el asistente reporta el incidente y lo confirma sólo después de una respuesta exitosa de ZafraFlow API

#### Scenario: Llegada con un solo turno activo

- **GIVEN** un transportista con un único turno activo, `EN_CAMINO`
- **WHEN** escribe "Llegué"
- **THEN** el asistente solicita la transición a `EN_ESPERA` sin volver a preguntar la patente

### Requirement: Tono humano de las respuestas

El asistente SHALL responder con mensajes breves, cálidos y naturales en español neutro y profesional, de usted, sin modismos regionales y sin emojis salvo que el transportista los use. SHALL variar las aperturas, reconocer lo que dijo el transportista, hacer una pregunta por vez y usar listas sólo para los cuatro datos del turno y para la lectura de confirmación. MUST NOT mostrar códigos internos ni valores en inglés, como `EN_CAMINO`, `ASIGNADO`, `PINCHAZO` o `statusCode`: SHALL decir los estados como asignado, en camino, en espera, ingresado, en descarga, finalizado o cancelado, y las fechas como "hoy a las HH:MM" o "mañana a las HH:MM" en la zona horaria del ingenio. Ante un error, SHALL disculparse brevemente e indicar qué puede hacer el transportista. SHALL usar el nombre del transportista sólo si lo dio en la conversación. Estas pautas MUST NOT relajar las reglas existentes del asistente.

#### Scenario: Consulta de estado

- **GIVEN** un transportista con un turno `ASIGNADO` para mañana
- **WHEN** pregunta cómo está su turno
- **THEN** el asistente responde con palabras, por ejemplo "Su turno para el camión AB123CD está asignado para mañana, entre las 08:00 y las 09:00.", sin códigos internos

#### Scenario: Incidente registrado

- **GIVEN** un incidente reportado con éxito
- **WHEN** el asistente lo confirma
- **THEN** nombra el problema y el estado con palabras y no muestra `ACCIDENTE`, `EN_CAMINO` ni otro valor interno

#### Scenario: Falla del sistema

- **GIVEN** que la consulta de turnos devuelve un error del servidor
- **WHEN** el asistente responde
- **THEN** se disculpa brevemente, no informa un resultado y sugiere volver a escribir en unos minutos
