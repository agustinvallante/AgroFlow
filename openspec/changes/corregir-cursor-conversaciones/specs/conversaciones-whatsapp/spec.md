# Delta: Conversaciones de WhatsApp

## ADDED Requirements

### Requirement: Cursores reutilizables de conversaciones

Todo `nextCursor` no nulo emitido por las lecturas de conversaciones y mensajes
SHALL poder reutilizarse sin modificarlo en la siguiente consulta del mismo
recorrido y ámbito autorizado. El codec SHALL aceptar sus propias salidas
canónicas para todo su rango de timestamps admitido. Los límites de entrada y
salida SHALL coincidir con OpenAPI. Los cursores malformados, no canónicos o
demasiado largos MUST seguir rechazándose como error de validación. Aceptar
un cursor sintácticamente válido MUST NOT habilitar acceso a conversaciones o
mensajes de otro ingenio ni omitir el anclaje de mensajes a su conversación.

#### Scenario: Historial de más de cincuenta mensajes

- **GIVEN** una conversación con 51 mensajes persistidos
- **WHEN** el dashboard consulta con limit=50 y reenvía el nextCursor recibido
- **THEN** ambas páginas responden éxito, el historial queda completo y el último nextCursor es null

#### Scenario: Mensajes nuevos entre páginas

- **GIVEN** un historial que requiere varias páginas
- **WHEN** llegan mensajes después de la primera lectura y se continúa con los cursores emitidos
- **THEN** el recorrido mantiene el orden de persistencia y devuelve cada mensaje exactamente una vez hasta finalizar

#### Scenario: Continuación del listado de conversaciones

- **GIVEN** más conversaciones que el límite de página
- **WHEN** el consumidor reutiliza cada nextCursor del listado
- **THEN** puede recorrerlas hasta null en el orden de actividad de la API sin rechazos por el tamaño de un cursor válido

#### Scenario: Extremos del timestamp

- **GIVEN** un cursor canónico generado con un timestamp actual o un extremo del rango admitido
- **WHEN** se codifica y decodifica
- **THEN** se conserva el timestamp y el identificador sin rechazar la propia salida del codificador

#### Scenario: Cursor inválido

- **GIVEN** un cursor malformado, no canónico o más largo que el máximo documentado
- **WHEN** se pide una página con él
- **THEN** la API responde 400 VALIDATION_ERROR

#### Scenario: Cursor de otra conversación o ingenio

- **GIVEN** un cursor canónico de mensajes perteneciente a otra conversación o ingenio
- **WHEN** se intenta usar como ancla de mensajes de una conversación distinta o acceder a una conversación ajena
- **THEN** el ancla ajena se rechaza y el acceso a la conversación de otro ingenio permanece oculto como recurso inexistente
