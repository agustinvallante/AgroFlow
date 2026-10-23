> **Estado: delta propuesto, no aprobado.** Depende de OD-008, OD-010 y OD-011. Complementa la capacidad vigente sin modificar sus requisitos; la persistencia y sus reglas se definen en `conversaciones-whatsapp`.

## ADDED Requirements

### Requirement: Autenticación del canal ante la API (OD-008)

La integración de n8n SHALL autenticarse como servicio en cada operación de registro de mensajes, respuestas y resultados. AgroFlow MUST rechazar esas operaciones sin una credencial de servicio válida, MUST rechazar instancias del canal no habilitadas y SHALL determinar el ingenio únicamente a partir de la instancia autorizada.

#### Scenario: Credencial ausente o inválida

- **GIVEN** una solicitud de registro de mensaje sin credencial de servicio válida
- **WHEN** llega a la API
- **THEN** AgroFlow responde no autenticado sin persistir datos

#### Scenario: Instancia no habilitada

- **GIVEN** una credencial válida y una instancia del canal desconocida o inactiva
- **WHEN** n8n registra un mensaje de esa instancia
- **THEN** AgroFlow rechaza la operación sin persistir datos ni revelar otros ingenios

### Requirement: Registro previo al procesamiento conversacional (OD-011)

n8n SHALL registrar cada mensaje entrante en AgroFlow antes de invocar al asistente o a cualquier operación de turnos, y SHALL continuar solo cuando AgroFlow autorice el procesamiento. Ante una disposición de detención, un conflicto, un error o la imposibilidad de registrar, n8n MUST NOT invocar al asistente, a las operaciones de turnos ni al envío.

#### Scenario: Mensaje autorizado de un transportista reconocido

- **GIVEN** un mensaje registrado con procesamiento autorizado y remitente reconocido
- **WHEN** n8n recibe la respuesta del registro
- **THEN** continúa con el asistente usando la autorización recibida

#### Scenario: Mensaje repetido

- **GIVEN** un mensaje que AgroFlow informa como repetido
- **WHEN** n8n recibe la disposición de detención
- **THEN** finaliza sin invocar al asistente, las operaciones de turnos ni el envío

#### Scenario: Historial no disponible

- **GIVEN** que el registro del mensaje falla por error o tiempo de espera
- **WHEN** n8n evalúa el resultado
- **THEN** finaliza sin invocar al asistente ni enviar respuesta

#### Scenario: Remitente no reconocido

- **GIVEN** un mensaje autorizado cuyo remitente AgroFlow informa como no reconocido
- **WHEN** n8n lo procesa
- **THEN** no invoca operaciones de turnos y solo puede responder con el texto fijo aprobado para remitentes no registrados

### Requirement: Envío limitado a respuestas registradas (OD-010)

n8n SHALL enviar por WhatsApp únicamente el texto y el destinatario de una intención de respuesta confirmada por AgroFlow, y SHALL informar el resultado del envío. n8n MUST NOT reenviar automáticamente una respuesta cuyo resultado sea incierto.

#### Scenario: Envío confirmado por el proveedor

- **GIVEN** una intención de respuesta registrada
- **WHEN** el proveedor acepta el envío
- **THEN** n8n informa la aceptación con el identificador devuelto por el proveedor

#### Scenario: Tiempo de espera del proveedor

- **GIVEN** una intención de respuesta registrada
- **WHEN** el envío excede el tiempo de espera
- **THEN** n8n informa un resultado incierto y no reenvía el mensaje
