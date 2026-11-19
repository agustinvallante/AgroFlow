# Delta: Monitoreo operativo

## ADDED Requirements

### Requirement: Conversaciones de WhatsApp en vivo

El frontend SHALL leer los mensajes de la conversación abierta cada segundo y la lista de conversaciones cada 3 segundos, sin perder la selección. Una lectura periódica MUST NOT empezar mientras la anterior del mismo tipo sigue pendiente. Mientras la pestaña está oculta, el frontend MUST NOT hacer lecturas periódicas y, al volver a mostrarse, SHALL leer de inmediato. Las respuestas de una selección anterior o de una consulta superada MUST descartarse, un listado incompleto SHALL mostrarse con su aviso y un refresco fallido SHALL conservar los datos obtenidos.

#### Scenario: Mensaje nuevo con la conversación abierta

- **GIVEN** una conversación abierta
- **WHEN** ZafraFlow API registra un mensaje nuevo en ella
- **THEN** el frontend lo muestra en la siguiente lectura, sin que el operador presione Actualizar

#### Scenario: Lectura pendiente

- **GIVEN** una lectura de mensajes o de la lista que todavía no respondió
- **WHEN** vence el intervalo de refresco
- **THEN** el frontend no envía otra lectura del mismo tipo hasta que la anterior termine

#### Scenario: Pestaña oculta

- **GIVEN** el dashboard abierto en una pestaña oculta
- **WHEN** vencen los intervalos de refresco
- **THEN** el frontend no consulta la API, y al volver a mostrarse la pestaña lee de inmediato la lista y la conversación abierta

### Requirement: Posición de lectura en la conversación abierta

Cuando el operador está al final de la conversación, los mensajes nuevos SHALL mantener la vista al final. Cuando el operador subió, el refresco MUST NOT mover la vista y el frontend SHALL ofrecer un botón que indique cuántos mensajes nuevos llegaron y lleve al final. Seleccionar una conversación SHALL seguir llevando la vista a su último mensaje. Sólo los mensajes que llegan con la conversación ya abierta SHALL entrar con una animación, y ninguna animación SHALL reproducirse cuando el usuario pide reducir el movimiento.

#### Scenario: Operador al final

- **GIVEN** el operador está al final de la conversación abierta
- **WHEN** llega un mensaje nuevo
- **THEN** la vista muestra ese mensaje al final, con su animación de entrada

#### Scenario: Operador leyendo mensajes anteriores

- **GIVEN** el operador subió en la conversación abierta
- **WHEN** llega un mensaje nuevo
- **THEN** la vista conserva su posición y aparece el botón "1 mensaje nuevo", que lleva al final

### Requirement: Respuesta del bot en curso

El frontend SHALL indicar "El bot está respondiendo" sólo cuando el último mensaje de la conversación abierta es entrante y ZafraFlow API informa su procesamiento `PROCESSING`. Un procesamiento fallido o vencido SHALL conservar su etiqueta de falta de respuesta, sin el indicador.

#### Scenario: Mensaje entrante en proceso

- **GIVEN** una conversación cuyo último mensaje es entrante y está en procesamiento
- **WHEN** el operador la tiene abierta
- **THEN** ve el indicador, que desaparece cuando la API informa la respuesta del bot

### Requirement: Lista de conversaciones en vivo

La lista SHALL conservar el orden de ZafraFlow API y permitir buscar por nombre o vista previa y filtrar por Todas, Con el bot, Requiere revisión y No registrado; si nada coincide, SHALL indicarlo. Una conversación que no está abierta y muestra actividad nueva en una lectura de la lista SHALL destacarse y sumar una novedad sin leer, que se borra al abrirla. Las novedades se calculan en el navegador durante la sesión y MUST NOT presentarse como mensajes leídos o entregados. El estado de envío de cada mensaje SHALL tener un nombre accesible con el texto vigente.

#### Scenario: Actividad en otra conversación

- **GIVEN** el operador tiene abierta una conversación
- **WHEN** otra conversación recibe un mensaje
- **THEN** la lista la destaca con "1 novedad sin leer" en su lugar de la API, y la novedad se borra al abrirla

#### Scenario: Búsqueda sin resultados

- **GIVEN** conversaciones listadas
- **WHEN** el operador busca un texto que ninguna contiene en su nombre o vista previa
- **THEN** la lista indica que ninguna conversación coincide con la búsqueda
