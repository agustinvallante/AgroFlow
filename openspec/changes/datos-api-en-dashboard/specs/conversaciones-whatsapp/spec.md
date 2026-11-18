# Delta: Conversaciones de WhatsApp

## ADDED Requirements

### Requirement: Lectura completa de conversaciones y mensajes en el dashboard

El dashboard SHALL recorrer las páginas de conversaciones y de mensajes reutilizando cada `nextCursor` hasta recibir null, con un tope de 20 páginas por lectura. Si falla una página posterior a la primera o se alcanza el tope, el dashboard SHALL mostrar lo ya obtenido junto con un aviso de que la lectura está incompleta. MUST NOT descartar las páginas ya leídas ni cortar el listado sin aviso.

#### Scenario: Más conversaciones que una página

- **GIVEN** más conversaciones que el tamaño de página
- **WHEN** el dashboard carga el Chatbot
- **THEN** lista todas las conversaciones de las páginas recorridas

#### Scenario: Falla una página posterior de mensajes

- **GIVEN** una conversación cuya segunda página de mensajes falla
- **WHEN** el dashboard la abre
- **THEN** muestra los mensajes de la primera página y avisa que no pudo cargar el resto
