# Conversaciones de WhatsApp en vivo en el dashboard

> **Estado: propuesta, sólo frontend.** Base: `main` en `997f4cd`.

## Problema

En la vista Chatbot WhatsApp, un mensaje nuevo recién aparece al presionar **Actualizar** o al volver a la pestaña: la vista se refresca cada 15 segundos y sólo baja al último mensaje al seleccionar una conversación, por lo que lo nuevo queda fuera de la vista. Tampoco hay forma de buscar o filtrar conversaciones ni de saber cuáles recibieron mensajes mientras el operador miraba otra.

## Alcance

- La conversación abierta se lee cada segundo y la lista de conversaciones cada 3 segundos, sin parpadeo y sin perder la selección. Una lectura periódica no empieza mientras la anterior del mismo tipo sigue pendiente, y las lecturas se pausan con la pestaña oculta; al volver a mostrarse, se lee de inmediato. **Actualizar** queda como acción secundaria.
- Se conservan los resguardos vigentes: las respuestas de una selección anterior o de una consulta superada se descartan, un listado incompleto se muestra con su aviso y un refresco fallido conserva los datos obtenidos.
- Si el operador está al final de la conversación, lo nuevo lo mantiene al final; si subió, la vista no se mueve y un botón indica cuántos mensajes nuevos hay y lleva al final. Seleccionar una conversación sigue llevando al último mensaje.
- Los mensajes que llegan con la conversación abierta entran con una animación, que se desactiva con `prefers-reduced-motion`.
- El indicador "El bot está respondiendo" se deriva sólo de datos que la API ya informa: el último mensaje de la conversación es entrante y su procesamiento está `PROCESSING` (`en-curso` en el frontend). Un procesamiento fallido o vencido conserva su etiqueta actual.
- La lista suma avatar con iniciales, búsqueda por nombre o vista previa, filtros (Todas, Con el bot, Requiere revisión, No registrado) y un contador de novedades sin leer por conversación, calculado en el navegador durante la sesión. Conserva el orden de la API.
- Los mensajes muestran el estado de envío con íconos cuyo nombre accesible es el texto completo vigente; el encabezado muestra si la vista está en vivo, y el pie aclara que responde el bot y que la respuesta manual del operador todavía no está disponible.

## Fuera de alcance

- Cambios en la API, en n8n o en el contrato OpenAPI. No se agregan WebSockets ni notificaciones (RN-042A).
- Respuesta manual del operador, marcas de leído persistidas y contadores exactos de mensajes no leídos: la API no informa esos datos.
- Métricas y plantillas del chatbot, que siguen sin datos.

## Decisiones

- **Contador de novedades:** el listado sólo informa el último mensaje de cada conversación. El contador suma uno por cada lectura del listado en la que una conversación que no está abierta muestra actividad nueva (cambia su último mensaje); por eso se nombra "novedades sin leer" y no "mensajes sin leer". Se reinicia al abrir la conversación o al recargar la página.
- **Relación con `persistir-conversaciones-whatsapp`:** ese cambio, todavía sin consolidar, exige que el refresco periódico no mueva la vista. Este cambio lo precisa: el refresco no mueve al operador que subió, y mantiene al final al que ya estaba ahí. Al consolidar ambos cambios se debe conservar esta precisión.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `monitoreo-operativo` | Existente | [specs/monitoreo-operativo/spec.md](specs/monitoreo-operativo/spec.md) |
