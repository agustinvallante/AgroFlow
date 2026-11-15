# Visibilidad de turnos para la presentación

## Why

Las issues [#108](https://github.com/agustinvallante/AgroFlow/issues/108) y
[#109](https://github.com/agustinvallante/AgroFlow/issues/109) corrigen dos
recorridos que ya persisten correctamente en LocalDemo. Un alta con ventana de
otro día queda oculta en la cola de hoy, y el panel omite camiones en curso cuya
ventana no pertenece al día actual.

## What Changes

- Conservar inicio y fin RFC 3339 completos de la ventana en el modelo web.
- Confirmar el alta con los datos del POST, abrir su detalle y seleccionar su
  fecha. Limpiar los demás filtros y explicarlo en pantalla.
- Consultar explícitamente turnos activos de cualquier fecha para el panel,
  distinguiendo asignados de camiones efectivamente en curso.
- Mostrar fechas, horas y desplazamiento del ingenio sin convertirlas a la zona
  del navegador. Mantener polling, errores y descarte de respuestas atrasadas.

## Impact

Frontend React/Vite, modelo, repositorio HTTP, casos de uso, hook y vistas de cola
y panel, con pruebas y guía de verificación. Se utilizan las operaciones y DTOs
existentes de [OpenAPI](../../../docs/contracts/openapi.yaml) y el filtro
`active=true` definido en
[ajustar-consultas-chatbot-whatsapp](../ajustar-consultas-chatbot-whatsapp/specs/turnos-consulta/spec.md).
No cambia el contrato HTTP, la asignación, la base de datos ni el alcance de las
PRs #105 y #106. La rama parte directamente de main.
