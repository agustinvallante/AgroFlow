# Delta: Integración WhatsApp y n8n

## ADDED Requirements

### Requirement: Consulta de turnos activos desde el canal (CU-003)

Cuando el transportista consulta sus turnos, n8n SHALL listar con el `phone` del remitente y `active=true`, sin calcular fechas, para que la API devuelva sus turnos activos de cualquier día. SHALL distinguir una colección vacía de una respuesta de error y MUST NOT comunicar que no hay turnos cuando la API respondió un error.

#### Scenario: Turno para mañana

- **GIVEN** un transportista reconocido cuyo turno activo fue asignado para mañana
- **WHEN** pregunta por sus turnos
- **THEN** el chatbot comunica ese turno con el estado y la ventana devueltos por la API

#### Scenario: Sin turnos activos tras cancelación o finalización

- **GIVEN** un transportista reconocido cuyo único turno fue cancelado o finalizado desde el panel operativo
- **WHEN** pregunta por sus turnos y la API devuelve una colección vacía
- **THEN** la herramienta conserva la colección vacía sin degradarla a error y el chatbot informa que no posee turnos activos

#### Scenario: Error de la consulta

- **GIVEN** que la API responde un error al listar turnos
- **WHEN** el transportista pregunta por sus turnos
- **THEN** el chatbot informa una falla del sistema y no afirma que carece de turnos

### Requirement: Resolución de la finca mediante la API (CU-002)

Para solicitar un turno, n8n SHALL resolver la finca nombrada por el transportista consultando el catálogo de fincas activas del backend con sus palabras, sin normalizarlas ni mantener códigos de finca en el workflow, y SHALL enviar al alta sólo un `farmCode` devuelto por esa consulta. Ante varias fincas SHALL pedir aclaración; ante ninguna SHALL informarlo y pedir que se verifique el nombre; ante un error MUST NOT solicitar el alta. Esta lectura es la única consulta de datos maestros permitida al canal; n8n MUST NOT invocar otras rutas de datos maestros.

#### Scenario: Nombre natural único

- **GIVEN** un transportista que menciona "finca sur" y una única finca activa coincidente
- **WHEN** el chatbot consulta el catálogo con esas palabras
- **THEN** usa el código devuelto y lo confirma con el transportista antes del alta

#### Scenario: Nombre ambiguo

- **GIVEN** un nombre que coincide con varias fincas activas
- **WHEN** el chatbot recibe la respuesta de la API
- **THEN** pregunta cuál de ellas corresponde y no solicita el alta hasta obtener la aclaración

#### Scenario: Finca inexistente

- **GIVEN** un nombre sin fincas activas coincidentes
- **WHEN** el chatbot recibe una colección vacía
- **THEN** informa que no encontró la finca, pide verificar el nombre y no inventa un código

#### Scenario: Error del catálogo

- **GIVEN** que la consulta de fincas responde un error
- **WHEN** el chatbot recibe la respuesta
- **THEN** informa una falla del sistema y no solicita el alta

### Requirement: Solicitud de turno sin bloqueo por memoria conversacional (CU-002, RN-019)

Cuando el transportista solicita un nuevo turno, n8n MUST NOT rechazar la operación por estado inferido de mensajes anteriores en la memoria conversacional, dado que el estado del turno puede cambiar fuera del canal (por ejemplo, cancelación o finalización desde el panel operativo). n8n SHALL resolver la finca, confirmar los datos requeridos con el transportista e invocar el alta en el backend para que la API determine la validez según el estado vigente.

#### Scenario: Nueva solicitud luego de cancelación en el panel

- **GIVEN** una conversación previa donde un camión tenía un turno activo que luego fue cancelado desde el panel operativo
- **WHEN** el transportista vuelve a solicitar un turno para esa patente y confirma los cuatro datos requeridos
- **THEN** n8n invoca el alta en la API sin rechazar por memoria conversacional y comunica el resultado persistido por el backend
