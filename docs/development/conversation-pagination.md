# Paginación de conversaciones — corrección #122

## Qué fallaba

El frontend pide mensajes en páginas de 50 y reenvía `nextCursor`. La API
codificaba cursores de 68 caracteres para fechas actuales, pero rechazaba más
de 64 al decodificarlos. Por eso un chat corto funcionaba y uno de más de
50 mensajes fallaba con `400 VALIDATION_ERROR`, `Cursor inválido`.
El listado paginado de conversaciones usaba el mismo codec y tenía el defecto.

## Corrección y compatibilidad

El máximo es ahora **70 caracteres**, suficiente para la representación actual
en todo el rango de DateTime. El formato y los datos persistidos no cambian.
OpenAPI **0.7.1** alinea el parámetro cursor y nextCursor de ambas respuestas.
Los clientes deben tratarlo como un valor opaco y reenviarlo sin interpretarlo.

Se conserva el rechazo de cursores inválidos/no canónicos y el aislamiento por
ingenio y conversación. No se requiere reinicializar SQLite ni borrar mensajes.
Tampoco se cambia el frontend ni el workflow de WhatsApp.

## Ensayo manual

1. Seguir el [runbook local](local-demo-runbook.md) con una base de ensayo y
   configuración privada de claves. Usar sólo mensajes ficticios o consentidos.
2. Preparar una conversación con al menos 51 mensajes persistidos, mediante
   el canal de prueba o `POST /api/v1/conversation-events`. Cada evento de
   prueba debe tener un providerMessageId distinto.
3. Abrir **Chatbot WhatsApp** y seleccionar esa conversación. En las
   solicitudes del navegador, comprobar una primera respuesta de 50 mensajes
   y una segunda petición con el cursor recibido: ambas deben responder 200.
4. Comprobar que el primer y último mensaje aparecen, sin duplicados. Agregar
   otro mensaje con el chat abierto y esperar el refresco: debe aparecer.
5. Recargar la página y volver a seleccionar el chat: el historial permanece.
6. Para el listado HTTP, crear al menos tres conversaciones ficticias distintas
   y recorrer `GET /api/v1/conversations?limit=1` hasta nextCursor null. La clave
   del dashboard se usa sólo en el cliente de QA del servidor o en el proxy;
   nunca se agrega al JavaScript del navegador.
7. Un cursor con padding, mal formado o excesivamente largo debe seguir causando
   400. Los anclajes de mensajes de otra conversación no permiten saltar de chat.

El frontend conserva sus límites actuales: veinte páginas por lectura de
mensajes (hasta 1000) y una sola página de listado de conversaciones. Este
cambio corrige el rechazo del cursor, no amplía esos alcances.

## Pruebas

- `PageCursorCodecTests`: fechas mínima/actual/máxima, UUID, sintaxis, rango,
  longitud y representaciones no canónicas.
- `ConversationsHttpTests`: 51 mensajes con páginas de 50, varias páginas con
  nuevas llegadas, listado completo por cursor y anclajes ajenos sobre SQLite real.
- Pruebas existentes de store, frontend e integración: se conservan.

La evidencia del ensayo de este cambio no acredita WhatsApp real; su cierre
sigue registrado por separado en #110 y #111.

## Evidencia local — 2026-11-16

- Base: main `ea1153b5f0dead93e5274cc9f4cdc5905471ee7a`; rama
  `fix/122-cursor-conversaciones`.
- Regresiones nuevas: **30 aprobadas**. Se verificó primero que los casos de
  fechas actuales/máxima y paginación fallaban con el límite anterior.
- Backend completo: **477/477**. Frontend: **138/138**, typecheck y build
  correctos; lint sin errores, con cinco advertencias preexistentes.
- Integración/workflow: **106/106** y aceptación existente correcta con
  API/SQLite/proxy reales y Evolution, WhatsApp y agente simulados.
- OpenSpec estricto: **27/27**. Ambos contratos OpenAPI válidos; Redocly
  conserva el aviso preexistente de 4xx en health. Enlaces Markdown válidos.
  Auditoría npm: cero vulnerabilidades; no se cambiaron dependencias.
- Chrome (Asia/Tokyo), API de la rama, proxy Vite y SQLite efímera:
  historial inicial de 51 mensajes (50 + 1), cursor de 68 caracteres aceptado,
  llegada del mensaje 52 visible por polling y recarga con los 52 en orden,
  sin duplicados ni errores JS. Las peticiones del navegador no contenían claves.
- El listado HTTP de dos conversaciones también se recorrió con limit=1 hasta
  nextCursor null. Los tests HTTP de aislamiento usan un ámbito fijo **sólo en
  la fixture** para comprobar dos ingenios; LocalDemo conserva su restricción
  operativa de un único ingenio.
