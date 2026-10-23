> **Estado: delta propuesto, no aprobado.** Nueva capacidad para CU-008 y el canal de CU-002, CU-003 y CU-006. Los requisitos marcados dependen de OD-006, OD-009 y OD-010; no deben consolidarse en `openspec/specs/` antes de su aprobación.

## ADDED Requirements

### Requirement: Registro de mensajes entrantes del canal

AgroFlow SHALL registrar cada mensaje de texto entrante de WhatsApp que reciba de una integración autenticada antes de autorizar su procesamiento conversacional. El registro SHALL asociar el mensaje a una conversación del ingenio al que pertenece la instancia del canal y SHALL conservar únicamente el texto, la dirección remota, el identificador del proveedor y los tiempos del proveedor y de persistencia. AgroFlow MUST NOT conservar el nombre visible del remitente ni el contenido completo del evento del proveedor.

#### Scenario: Primer mensaje de una dirección

- **GIVEN** una instancia del canal habilitada para un ingenio y una dirección remota sin conversación previa
- **WHEN** la integración registra un mensaje de texto válido de esa dirección
- **THEN** AgroFlow crea la conversación en ese ingenio, persiste el mensaje e informa sus identificadores y la autorización de procesamiento

#### Scenario: Mensaje de una conversación existente

- **GIVEN** una conversación existente para una instancia y una dirección remota
- **WHEN** la integración registra un nuevo mensaje de esa dirección
- **THEN** AgroFlow agrega el mensaje a la misma conversación y actualiza su última actividad

#### Scenario: Mensaje inválido

- **GIVEN** un mensaje sin identificador del proveedor, con texto vacío o que excede el largo admitido, o dirigido a un grupo
- **WHEN** la integración intenta registrarlo
- **THEN** AgroFlow lo rechaza como error de validación sin persistirlo ni autorizar su procesamiento

#### Scenario: Ingenio declarado en el cuerpo

- **GIVEN** un mensaje que incluye un identificador de ingenio en su contenido
- **WHEN** la integración intenta registrarlo
- **THEN** AgroFlow rechaza la solicitud y nunca usa ese valor para determinar el ámbito

### Requirement: Prevención de mensajes duplicados (OD-006)

AgroFlow SHALL identificar un mensaje entrante por el ámbito de la instancia del canal, la dirección remota y el identificador del proveedor. Un mensaje repetido con el mismo contenido MUST NOT crear un nuevo registro ni una nueva autorización de procesamiento. Un mensaje repetido con contenido distinto MUST rechazarse como conflicto sin sobrescribir el registro original.

#### Scenario: Repetición idéntica

- **GIVEN** un mensaje entrante ya registrado
- **WHEN** la integración lo registra nuevamente con el mismo contenido
- **THEN** AgroFlow devuelve los identificadores originales con una disposición de detención y no autoriza un nuevo procesamiento

#### Scenario: Repeticiones simultáneas

- **GIVEN** dos solicitudes concurrentes con el mismo mensaje entrante
- **WHEN** AgroFlow las procesa
- **THEN** existe un único registro y solo una de las respuestas autoriza el procesamiento

#### Scenario: Misma clave con contenido distinto

- **GIVEN** un mensaje entrante ya registrado
- **WHEN** la integración registra la misma clave con otro texto o tiempo del proveedor
- **THEN** AgroFlow responde un conflicto y conserva el registro original sin cambios

#### Scenario: Mismo identificador en otra instancia

- **GIVEN** un mensaje registrado en una instancia del canal
- **WHEN** otra instancia habilitada registra un mensaje con el mismo identificador del proveedor
- **THEN** AgroFlow lo trata como un mensaje distinto dentro del ámbito de esa instancia

### Requirement: Autorización única de procesamiento (OD-006)

AgroFlow SHALL otorgar como máximo una autorización de procesamiento por mensaje entrante, en la misma operación que lo registra. Solo el titular de una autorización vigente SHALL poder registrar la respuesta o el resultado de ese procesamiento. AgroFlow MUST NOT otorgar automáticamente una nueva autorización cuando la anterior vence sin un resultado informado, y SHALL exponer esa situación como pendiente de revisión.

#### Scenario: Resultado informado por el titular

- **GIVEN** un mensaje entrante con autorización vigente
- **WHEN** el titular informa el resultado del procesamiento
- **THEN** AgroFlow cierra la autorización y registra el resultado

#### Scenario: Autorización vencida

- **GIVEN** un mensaje entrante cuya autorización venció sin resultado
- **WHEN** la integración vuelve a registrar el mismo mensaje
- **THEN** AgroFlow devuelve una disposición de detención, no otorga una nueva autorización y marca la conversación como pendiente de revisión

#### Scenario: Autorización ajena o vencida

- **GIVEN** un mensaje entrante y una autorización inválida o vencida
- **WHEN** se intenta registrar una respuesta o un resultado con ella
- **THEN** AgroFlow rechaza la operación sin modificar el procesamiento

### Requirement: Reconocimiento del remitente en el registro

Al registrar un mensaje entrante, AgroFlow SHALL informar si el remitente corresponde a un transportista activo del ingenio de la instancia, comparando el número derivado de la dirección remota con el WhatsApp registrado. AgroFlow MUST NOT derivar un número telefónico de direcciones que no representan un teléfono, MUST NOT reconocer transportistas de otro ingenio y MUST NOT revelar datos del transportista a la integración más allá de su reconocimiento.

#### Scenario: Remitente registrado

- **GIVEN** un transportista activo del ingenio con el número del remitente registrado
- **WHEN** la integración registra su mensaje
- **THEN** AgroFlow informa al remitente como reconocido y vincula la conversación con ese transportista

#### Scenario: Remitente no registrado

- **GIVEN** un número que no corresponde a ningún transportista activo del ingenio
- **WHEN** la integración registra su mensaje
- **THEN** AgroFlow registra el mensaje e informa al remitente como no reconocido

#### Scenario: Dirección sin teléfono

- **GIVEN** una dirección remota que no representa un número telefónico
- **WHEN** la integración registra su mensaje
- **THEN** AgroFlow informa al remitente como no reconocido sin inferir un teléfono

#### Scenario: Número registrado después

- **GIVEN** una conversación no reconocida y un transportista registrado luego con ese número
- **WHEN** llega un nuevo mensaje de esa dirección
- **THEN** AgroFlow vincula la conversación con el transportista sin modificar los mensajes anteriores

### Requirement: Respuestas registradas antes del envío (OD-010)

AgroFlow SHALL registrar cada respuesta saliente como intención pendiente de envío antes de que la integración la entregue al proveedor. La intención SHALL responder a un mensaje entrante de la misma conversación y SHALL devolver el destinatario, la instancia y el texto que deben enviarse. Una repetición con el mismo texto SHALL devolver la intención existente y una con texto distinto MUST rechazarse como conflicto.

#### Scenario: Intención válida

- **GIVEN** un mensaje entrante con autorización vigente
- **WHEN** su titular registra el texto de la respuesta
- **THEN** AgroFlow persiste la intención pendiente de envío y devuelve los datos de envío autorizados

#### Scenario: Segunda respuesta distinta para el mismo mensaje

- **GIVEN** un mensaje entrante que ya tiene una intención de respuesta
- **WHEN** se intenta registrar otra respuesta con texto distinto
- **THEN** AgroFlow rechaza la operación como conflicto

### Requirement: Estado de envío de las respuestas (OD-010)

AgroFlow SHALL registrar el resultado de envío informado para cada respuesta: aceptada por el proveedor, fallida o incierta. AgroFlow MUST NOT presentar una respuesta como entregada o leída sin evidencia del proveedor, SHALL permitir resolver un resultado incierto como aceptado o fallido y MUST rechazar cualquier otra transición. Registrar el resultado de envío SHALL cerrar el procesamiento del mensaje entrante correspondiente.

#### Scenario: Envío aceptado

- **GIVEN** una respuesta pendiente de envío
- **WHEN** la integración informa la aceptación con el identificador del proveedor
- **THEN** AgroFlow registra la respuesta como aceptada y cierra el procesamiento

#### Scenario: Resultado incierto

- **GIVEN** una respuesta cuyo envío excedió el tiempo de espera
- **WHEN** la integración informa un resultado incierto
- **THEN** AgroFlow la registra como incierta y la conversación queda pendiente de revisión

#### Scenario: Transición inválida

- **GIVEN** una respuesta aceptada
- **WHEN** se informa que falló
- **THEN** AgroFlow rechaza la transición sin modificar el estado

### Requirement: Consulta de conversaciones del ingenio (OD-009)

AgroFlow SHALL permitir a los usuarios internos autorizados del ingenio consultar sus conversaciones ordenadas por última actividad, en páginas acotadas, indicando si el remitente es un transportista reconocido y si la conversación requiere revisión. AgroFlow MUST NOT incluir conversaciones de otro ingenio.

#### Scenario: Listado del ingenio

- **GIVEN** conversaciones de dos ingenios
- **WHEN** un usuario autorizado de uno de ellos consulta las conversaciones
- **THEN** AgroFlow devuelve solo las de su ingenio, de la más reciente a la más antigua

#### Scenario: Conversación de otro ingenio

- **GIVEN** el identificador de una conversación de otro ingenio
- **WHEN** un usuario autorizado intenta consultarla
- **THEN** AgroFlow responde como recurso inexistente sin revelar su contenido

#### Scenario: Usuario sin permiso

- **GIVEN** un usuario autenticado cuyo rol no autoriza la lectura de conversaciones
- **WHEN** intenta consultarlas
- **THEN** AgroFlow rechaza la solicitud

### Requirement: Mensajes de una conversación paginados

AgroFlow SHALL devolver los mensajes de una conversación en el orden en que fueron persistidos, con su dirección, texto, tiempos, estado de envío y estado de procesamiento, en páginas acotadas mediante un cursor opaco. La paginación MUST NOT omitir ni repetir mensajes aunque se registren mensajes nuevos entre páginas, y un cursor inválido MUST rechazarse como error de validación.

#### Scenario: Recorrido completo

- **GIVEN** una conversación con más mensajes que el tamaño de página
- **WHEN** el usuario recorre todas las páginas mientras llegan mensajes nuevos
- **THEN** obtiene cada mensaje persistido exactamente una vez y en orden estable

#### Scenario: Cursor manipulado

- **GIVEN** un cursor alterado o mal formado
- **WHEN** se solicita una página con él
- **THEN** AgroFlow responde un error de validación sin exponer datos

### Requirement: Consulta de conversaciones sin efectos

Consultar conversaciones o mensajes MUST NOT modificar conversaciones, mensajes, autorizaciones de procesamiento ni estados de envío.

#### Scenario: Refresco del dashboard

- **GIVEN** la vista de conversaciones abierta
- **WHEN** el frontend vuelve a consultar
- **THEN** AgroFlow devuelve el estado vigente sin generar cambios

### Requirement: Minimización de datos del canal (OD-009)

AgroFlow MUST NOT registrar en sus logs el texto de los mensajes, las direcciones remotas ni los números telefónicos, y MUST NOT devolver direcciones remotas completas en los listados del dashboard.

#### Scenario: Error durante la ingesta

- **GIVEN** un mensaje cuya persistencia falla
- **WHEN** AgroFlow registra el error
- **THEN** el log contiene códigos e identificadores opacos, pero no el texto ni la dirección del remitente
