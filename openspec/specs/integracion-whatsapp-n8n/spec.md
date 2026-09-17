# Integración WhatsApp y n8n

## Purpose

Define los límites funcionales del canal conversacional para CU-002, CU-003, CU-006, CU-007 y CU-011.

## Requirements

### Requirement: Identificación desde WhatsApp (RN-014, RN-018)

Para solicitudes y consultas originadas en WhatsApp, AgroFlow SHALL identificar al transportista mediante el número telefónico del remitente y SHALL identificar el camión mediante su patente.

#### Scenario: Mensaje con identidad reconocida

- **GIVEN** un mensaje proveniente de un número asociado a un transportista y una patente informada
- **WHEN** n8n solicita una operación a la API
- **THEN** el backend valida ambas identidades y su asociación autorizada antes de continuar

#### Scenario: Número no reconocido

- **GIVEN** un mensaje proveniente de un número no asociado a un transportista activo
- **WHEN** n8n solicita una operación
- **THEN** AgroFlow la rechaza sin exponer datos de turnos o camiones

### Requirement: Backend como fuente de verdad (RN-019, RN-037)

El backend de AgroFlow SHALL determinar el estado vigente, quién debe ser notificado y qué cambió; n8n MUST limitarse a invocar la API y efectuar la comunicación mediante WhatsApp.

#### Scenario: Consulta por WhatsApp

- **GIVEN** un transportista identificado que consulta un turno
- **WHEN** n8n procesa el mensaje
- **THEN** obtiene el estado actual desde la API y responde sin usar un estado paralelo

#### Scenario: Interrupción con destinatarios afectados

- **GIVEN** una interrupción procesada por el backend
- **WHEN** se requieren notificaciones
- **THEN** n8n comunica los destinatarios y cambios indicados por AgroFlow sin recalcularlos

### Requirement: Actualización EN_CAMINO por el canal (RN-030)

AgroFlow SHALL admitir que un transportista identificado informe el estado `EN_CAMINO` mediante WhatsApp y n8n, sujeto a las mismas validaciones del ciclo de vida.

#### Scenario: Aviso válido de salida

- **GIVEN** un turno `ASIGNADO` de un camión autorizado para el transportista
- **WHEN** el transportista informa que está en camino
- **THEN** n8n solicita la transición y comunica únicamente el resultado confirmado por el backend

### Requirement: Confirmaciones derivadas de la API

n8n MUST NOT confirmar solicitudes, cancelaciones o cambios de estado que el backend no haya aceptado y persistido.

#### Scenario: API rechaza la operación

- **GIVEN** una operación solicitada mediante WhatsApp
- **WHEN** la API la rechaza o no puede persistirla
- **THEN** n8n informa el rechazo o la imposibilidad sin presentar la operación como exitosa
