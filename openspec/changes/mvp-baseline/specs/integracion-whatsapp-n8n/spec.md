## ADDED Requirements

### Requirement: Límite autenticado y credencial de servicio acotada

AgroFlow MUST aceptar operaciones originadas en WhatsApp únicamente cuando el evento del canal haya superado la verificación de autenticidad configurada y n8n se autentique ante la API con una credencial de servicio propia y limitada a las operaciones del canal. La credencial MUST NOT compartirse con el navegador ni habilitar operaciones administrativas.

La identidad de servicio MUST quedar acotada al ingenio autorizado y el teléfono informado por una integración autenticada SHALL ser solo una entrada para identificar al transportista; el backend MUST volver a validar transportista, camión y asociación.

#### Scenario: Evento y servicio válidos

- **GIVEN** un evento auténtico del canal y una llamada de n8n con credencial vigente, alcance suficiente e ingenio autorizado
- **WHEN** n8n solicita una operación permitida para el canal
- **THEN** AgroFlow continúa con las validaciones funcionales en el backend

#### Scenario: Evento sin autenticidad verificable

- **GIVEN** un evento cuya autenticidad no puede verificarse
- **WHEN** intenta ingresar al flujo conversacional
- **THEN** la integración lo rechaza sin invocar una mutación funcional

#### Scenario: Credencial ausente o sin alcance

- **GIVEN** una llamada de n8n sin credencial válida o sin el alcance requerido
- **WHEN** intenta invocar una operación de la API
- **THEN** AgroFlow rechaza la solicitud sin exponer ni modificar datos

#### Scenario: Operación administrativa desde la identidad de canal

- **GIVEN** una identidad de servicio válida para operaciones conversacionales
- **WHEN** intenta gestionar usuarios, roles o datos maestros
- **THEN** AgroFlow rechaza la operación por estar fuera de su alcance

#### Scenario: Recurso de otro ingenio

- **GIVEN** una identidad de servicio acotada a un ingenio y un identificador de otro ingenio
- **WHEN** n8n intenta consultarlo o modificarlo
- **THEN** AgroFlow rechaza la solicitud sin revelar ni alterar el recurso

### Requirement: Deduplicación persistida de mensajes

Cada mensaje del canal SHALL conservar un identificador estable del proveedor o adaptador, una correlación y una clave idempotente durante todos sus reintentos. AgroFlow MUST persistir la deduplicación de mensajes de modo que sobreviva a reinicios y MUST NOT aplicar más de una vez la misma intención.

Repetir el mismo identificador y contenido SHALL devolver el resultado original. Reutilizar el identificador con contenido que represente otra intención MUST ser rechazado como conflicto.

#### Scenario: Reintento del mismo mensaje

- **GIVEN** un mensaje cuya operación ya fue procesada
- **WHEN** n8n reintenta el mismo identificador con el mismo contenido
- **THEN** AgroFlow devuelve el resultado original sin repetir la mutación

#### Scenario: Reutilización con otro contenido

- **GIVEN** un identificador de mensaje ya asociado a una entrada
- **WHEN** n8n lo reutiliza con contenido que representa otra intención
- **THEN** AgroFlow rechaza el mensaje como conflicto sin modificar el resultado original

#### Scenario: Replay después de un reinicio

- **GIVEN** un mensaje procesado y un reinicio de los componentes
- **WHEN** el mismo mensaje vuelve a recibirse
- **THEN** AgroFlow reconoce el resultado persistido y no duplica la operación

### Requirement: Entrega observable y deduplicada de notificaciones

Cuando una operación confirmada requiera notificación, el backend SHALL determinar destinatario, contenido funcional, correlación y clave de deduplicación y SHALL registrar durablemente la entrega pendiente. n8n MUST comunicar solo esa instrucción y MUST NOT recalcular destinatarios ni cambios operativos.

La entrega SHALL exponer como mínimo los estados `PENDIENTE`, `ENVIADA`, `ENTREGADA` cuando el canal lo confirme y `FALLIDA`. AgroFlow SHALL permitir hasta tres intentos automáticos con espera incremental y, tras agotarlos, SHALL mantener la falla visible. AgroFlow MUST permitir el reintento manual únicamente a un actor con el permiso definido por `acceso-y-autorizacion`. Una falla de entrega MUST NOT revertir ni presentar como fallida una mutación operativa ya confirmada.

#### Scenario: Notificación entregada

- **GIVEN** una notificación `PENDIENTE` derivada de una operación confirmada
- **WHEN** el canal acepta el envío y luego confirma su entrega
- **THEN** AgroFlow conserva la transición a `ENVIADA` y luego a `ENTREGADA`

#### Scenario: Confirmación repetida del canal

- **GIVEN** una notificación cuyo resultado de entrega ya fue registrado
- **WHEN** el canal repite la misma confirmación
- **THEN** AgroFlow conserva un único resultado sin duplicar efectos

#### Scenario: Tres intentos fallidos

- **GIVEN** una notificación que falla en tres intentos automáticos
- **WHEN** se agota la política de reintentos
- **THEN** AgroFlow la conserva como `FALLIDA` y disponible para reintento manual autorizado

#### Scenario: Falla de notificación después de una mutación

- **GIVEN** una reprogramación o cancelación confirmada que originó una notificación
- **WHEN** la entrega de la notificación falla
- **THEN** AgroFlow conserva la mutación operativa confirmada y muestra el estado de entrega fallido por separado

#### Scenario: Reintento manual autorizado

- **GIVEN** una notificación `FALLIDA` y un actor con el permiso de reintento manual definido por `acceso-y-autorizacion`
- **WHEN** solicita ordenar un reintento manual
- **THEN** AgroFlow inicia un nuevo intento sin alterar la mutación operativa que originó la notificación

#### Scenario: Reintento manual sin permiso

- **GIVEN** una notificación `FALLIDA` y un actor sin el permiso de reintento manual definido por `acceso-y-autorizacion`
- **WHEN** intenta ordenar un reintento manual
- **THEN** AgroFlow rechaza la acción y conserva el estado de la entrega
