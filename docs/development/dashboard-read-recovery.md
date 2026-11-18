# Recuperación de Chatbot y Transportistas — #113

La corrección de [#113](https://github.com/agustinvallante/AgroFlow/issues/113)
parte de main `eeb5f3b` y se especifica en
[recuperar-cargas-dashboard](../../openspec/changes/recuperar-cargas-dashboard/proposal.md).
Reutiliza los GET de conversaciones, mensajes y transportistas publicados en
[OpenAPI](../contracts/openapi.yaml).

## Comportamiento

Si la primera carga falla, ambas pantallas muestran el error y **Reintentar**.
Chatbot conserva su polling cada quince segundos; una consulta exitosa limpia
el error inicial y permite volver a ver el historial. Transportistas permite
reintentar manualmente y ofrece **Actualizar** tras una carga válida.

Si falla una actualización posterior, se conserva la información válida y se
muestra **Datos desactualizados**. Un error de mensajes conserva el historial
de la misma conversación junto al error. Una respuesta correcta limpia el aviso.
La selección sólo se conserva mientras la conversación siga en el listado.
Resultados o errores atrasados no pueden reemplazar una selección o consulta
más reciente.

La corrección no modifica mensajes, turnos, cuentas, claves, backend ni SQLite.
La agrupación por día y el comportamiento de desplazamiento del historial
implementados en #104 se mantienen.

## Ensayo manual

Usar una instancia LocalDemo y una base ficticia, con las claves del dashboard
configuradas siguiendo el [runbook](local-demo-runbook.md).

1. Detener temporalmente la API de ensayo y abrir Chatbot. Deben aparecer un
   error y Reintentar, sin afirmar que no existen conversaciones.
2. Volver a levantar la API sobre el mismo archivo SQLite. Esperar el siguiente
   intervalo de quince segundos o pulsar Reintentar. El error debe desaparecer
   y mostrarse la respuesta válida. Reintentar con la API todavía detenida debe
   conservar una recuperación disponible.
3. Abrir una conversación y detener la API. El siguiente refresco conserva
   lista, selección e historial con el aviso de error. Restituir la API y
   actualizar: el aviso se retira sin cambiar la conversación seleccionada
   mientras siga presente.
4. Abrir Transportistas mientras la API no responde. Restituirla y pulsar
   Reintentar; debe aparecer el listado. Aplicar un filtro, volver a detener
   la API y pulsar Actualizar: se conserva el listado filtrado junto al aviso.
   Volver a levantarla y actualizar sin perder el filtro.
5. Cambiar rápidamente entre conversaciones mientras hay lecturas pendientes.
   Ninguna respuesta tardía debe mostrar mensajes o errores de otra conversación.

Para ensayar errores 503 sin detener el proceso también se puede interceptar
temporalmente la primera respuesta en un navegador de pruebas, liberando luego
las consultas hacia la API real. Esto simula la falla de transporte, sin
modificar el backend ni el contenido persistido.

## Pruebas

Desde la raíz:

```bash
npm --prefix frontend test
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Las regresiones en `ChatbotView.test.tsx` cubren recuperación automática y
manual, errores repetidos, historial durante fallas de listado y mensajes,
conversaciones retiradas y respuestas fuera de orden. Las de
`TransportistasView.test.tsx` cubren reintento, errores de refresco con filtros
y resultados atrasados. La suite conserva además las pruebas del historial
por día y del desplazamiento de #104.

## Evidencia — 2026-11-15

- Suite frontend: 126 pruebas aprobadas, incluidas trece regresiones nuevas.
  Typecheck y build correctos; lint sin errores y con las cinco advertencias
  preexistentes.
- OpenSpec estricto: 24 elementos válidos. Enlaces Markdown locales válidos.
- Integraciones y workflow: 106 pruebas aprobadas y aceptación local completa
  contra API, SQLite y proxy reales, con canal y agente simulados.
- Chrome y LocalDemo con una SQLite temporal y dos conversaciones sintéticas:
  falla inicial 503 interceptada y recuperación automática de Chatbot; reintentos
  manuales con falla repetida y posterior recuperación; falla de refresco
  conservando conversación e historial; recuperación manual de Transportistas;
  falla de actualización conservando listado y filtros. Sin errores JavaScript.

Los 503 de esta prueba se indujeron en el navegador; las consultas liberadas
obtuvieron sus datos de la API y SQLite reales. No se acreditan recepción ni
entrega de WhatsApp real y no se utilizaron datos privados del equipo.
