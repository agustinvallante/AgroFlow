# Recuperar lecturas del dashboard tras una falla inicial

## Why

La [issue #113](https://github.com/agustinvallante/AgroFlow/issues/113), hallazgo
A06 de la auditoría, identifica que una falla inicial deja Chatbot bloqueado
aunque el polling vuelva a recibir conversaciones. Transportistas tampoco
ofrece un reintento desde su error inicial.

## What Changes

- Unificar carga inicial, reintento y polling del listado de conversaciones.
  Una respuesta exitosa limpia el error inicial que bloqueaba la pantalla.
- Ofrecer Reintentar en el error inicial de ambas vistas y Actualizar en
  Transportistas. Chatbot conserva su refresco cada quince segundos.
- Separar errores iniciales de errores de refresco y conservar el último listado
  válido, con un aviso de datos desactualizados cuando falle una lectura posterior.
- Conservar selección e historial sólo cuando corresponden a la conversación
  vigente. Descartar respuestas atrasadas de listados y mensajes, y lecturas que
  terminan después de desmontar la vista.

## Impact

Rama desde main `eeb5f3b`. Cambia hooks, vistas, pruebas y documentación de
React/Vite. Reutiliza los GET publicados en
[OpenAPI](../../../docs/contracts/openapi.yaml), sin cambios HTTP, dependencias,
backend, migraciones, canal WhatsApp ni autenticación.

La capacidad [conversaciones-whatsapp](../persistir-conversaciones-whatsapp/specs/conversaciones-whatsapp/spec.md)
conserva agrupación por día, posición del historial durante refrescos y
desplazamiento al seleccionar una conversación. La recuperación de transportistas
se acota a la lectura de datos maestros; no amplía su gestión.
