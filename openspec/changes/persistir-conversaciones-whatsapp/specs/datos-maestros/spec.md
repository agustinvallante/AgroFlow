> **Estado: delta propuesto, pendiente de OD-013.** Complementa la [propuesta de normalización de WhatsApp](../../../reconciliar-pr47-con-mvp/specs/datos-maestros/spec.md) de `reconciliar-pr47-con-mvp`, que sigue pendiente; ambas deben revisarse juntas antes de consolidarse. La gestión de catálogos y la unicidad global ya son requisitos vigentes: este delta solo agrega el formato necesario para identificar al remitente.

## ADDED Requirements

### Requirement: WhatsApp del transportista en formato internacional

AgroFlow SHALL exigir que el número de WhatsApp de un transportista se registre en formato internacional, con el signo más, el código de país y el número completo, y SHALL rechazar cualquier otro formato. El valor normalizado SHALL ser el que se compara con el número del remitente de WhatsApp, sin agregar ni quitar dígitos por inferencia.

#### Scenario: Alta con formato internacional

- **GIVEN** un usuario autorizado y un número de WhatsApp en formato internacional válido
- **WHEN** registra un transportista con ese número
- **THEN** AgroFlow lo persiste y un mensaje de WhatsApp de ese número identifica a ese transportista

#### Scenario: Número sin código de país

- **GIVEN** un número de WhatsApp sin signo más ni código de país
- **WHEN** un usuario autorizado intenta registrarlo
- **THEN** AgroFlow rechaza el valor e identifica el campo inválido sin persistirlo

#### Scenario: Número registrado en otro ingenio

- **GIVEN** un número de WhatsApp registrado para un transportista de otro ingenio
- **WHEN** un usuario autorizado intenta registrarlo en su ingenio
- **THEN** AgroFlow rechaza el alta como identificador en uso sin revelar el otro ingenio
