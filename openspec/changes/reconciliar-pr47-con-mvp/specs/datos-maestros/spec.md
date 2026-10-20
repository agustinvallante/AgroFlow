> **Estado: propuesta pendiente de OD-013; no es especificación canónica aprobada.** Este delta describe la normalización propuesta. La fundación no habilitada conserva por compatibilidad la representación de la demo actual de main; eso no resuelve los detalles de validación de WhatsApp ni autoriza archivar este delta. Debe revisarse explícitamente con B00 antes de incorporarlo a la especificación canónica.

## ADDED Requirements

### Requirement: Normalización de WhatsApp del transportista

AgroFlow SHALL normalizar el número de WhatsApp de un transportista a solo dígitos al crear o actualizar sus datos, y SHALL usar ese valor normalizado al comparar identidad.

#### Scenario: Alta o edición con separadores

- **GIVEN** un número de WhatsApp con espacios, prefijo u otros separadores
- **WHEN** AgroFlow crea o actualiza el transportista
- **THEN** conserva el valor recibido y compara su identidad usando únicamente los dígitos

#### Scenario: Valor sin dígitos

- **GIVEN** un número de WhatsApp sin dígitos
- **WHEN** AgroFlow crea o actualiza el transportista
- **THEN** rechaza el valor sin persistirlo
