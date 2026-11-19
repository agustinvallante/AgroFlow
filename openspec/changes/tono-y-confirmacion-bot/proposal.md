# Tono humano y confirmación de incidentes en el asistente de WhatsApp

> **Estado: propuesta para la demo local.** Base: `main` en `4c623e8`.

## Problema

En una prueba en vivo, el transportista escribió "accidente sin heridos", el asistente preguntó si podía seguir y cuánto se demoraba, y con la respuesta "no puede seguir, 30 minutos de demora" registró el incidente sin leer los datos ni esperar un sí. Respondió además "Estado del turno: EN_CAMINO", con el código interno del estado. En la revisión del guion también se señaló que las respuestas suenan a plantilla: repiten la misma estructura, usan listas para respuestas simples y no se sienten como una conversación.

## Alcance

- Antes de registrar un incidente en ruta, el asistente lee la patente, el tipo de problema, si el camión puede seguir y la demora estimada, y espera un sí explícito en un mensaje posterior. Los datos que llegan en el mismo mensaje del aviso no son una confirmación.
- El alta de turnos conserva la lectura de los datos y exige el sí explícito en un mensaje posterior.
- Para avisar que se resolvió un problema o que el camión llegó al ingenio, el asistente confirma la patente sólo cuando el transportista tiene más de un turno activo.
- Todas las respuestas son breves, cálidas y naturales, en español neutro y profesional, de usted, con una pregunta por vez. No muestran códigos internos ni valores en inglés: los estados, tipos de problema y horarios se dicen con palabras.
- El prompt incluye tres intercambios de ejemplo en español (incidente con lectura y sí posterior, consulta de estado y error) que muestran el tono sin reemplazar las reglas.
- Se corrige la descripción de la herramienta `Report En Camino`: el camión sale de la finca hacia el ingenio.

## Fuera de alcance

- Cambios de ZafraFlow API, del contrato HTTP o de la persistencia.
- Nuevas intenciones: el alcance sigue en seis.
- Cambios de las reglas existentes: hechos sólo desde las herramientas, manejo de códigos de error, 911 primero ante un accidente con heridos, sin promesas de reprogramación, siempre en español.
- Personalizar con el nombre de perfil de WhatsApp: el workflow no lo envía al agente; el nombre se usa sólo si el transportista lo dijo en la conversación.

## Decisiones

- Las reglas viven en el prompt del agente de `chatbot/workflows/Chatbot.json`; las pruebas de `chatbot/tests/workflow.test.mjs` las fijan, pero no simulan el modelo, de modo que el tono se valida además por WhatsApp en vivo.
- La confirmación de la patente sólo con más de un turno activo ajusta, para la llegada, lo descrito en `mapa-eventos-reales`; la resolución ya seguía esa regla.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `integracion-whatsapp-n8n` | Existente | [specs/integracion-whatsapp-n8n/spec.md](specs/integracion-whatsapp-n8n/spec.md) |
