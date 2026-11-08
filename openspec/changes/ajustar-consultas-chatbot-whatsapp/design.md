# Diseño: ajustar las consultas del chatbot de WhatsApp

## 1. Turnos activos de cualquier fecha

**Causa raíz.** `EfAppointmentStore.ListAsync` aplica la fecha local actual del ingenio cuando `date` no se informa, como exige el listado de la demo. La herramienta `Lookup Appointments By Phone` consulta sólo con `phone`, así que un turno asignado para mañana queda fuera aunque esté persistido.

**Decisión.** Agregar `active=true` como filtro explícito en lugar de cambiar el valor por defecto:

- Con `active=true` se excluyen `FINALIZADO` y `CANCELADO` (el mismo criterio que `Turno.IsActive`) y, sin `date`, no hay fecha por defecto.
- Con `date`, se restringe a esa fecha; los demás filtros siguen combinándose con AND.
- Sin `active`, el comportamiento es idéntico al anterior: el listado de operadores conserva el día actual por defecto (RN-022A).
- Sólo se admite `true`: "turnos no activos" no tiene un recorrido acordado y, sin fecha por defecto, devolvería todo el historial terminal.
- Un turno activo cuya ventana ya pasó también se devuelve: sigue bloqueando un alta nueva para ese camión (`ACTIVE_APPOINTMENT_EXISTS`), y ocultarlo dejaría al transportista sin explicación.

**Alternativas descartadas.** Cambiar el valor por defecto cuando se informa `phone` (regla implícita y distinta según el filtro); calcular en n8n las fechas de hoy y mañana (duplica una regla del backend y falla con turnos posteriores).

## 2. Resolución de fincas por nombre

**Causa raíz.** El alta resuelve `farmCode` sin distinguir mayúsculas pero sin otra normalización, y el agente sólo tenía como referencia los códigos escritos en la descripción de la herramienta. No había forma de consultar el catálogo.

**Decisión.**

- `GET /api/v1/farms` en LocalDemo, sólo lectura, reutilizando `MasterDataService.Farms` (el `ListFincas` existente); `MasterDataController` sigue `[NonController]`.
- La coincidencia vive en el backend (`FarmSearch`, capa Application): palabras completas del código y del nombre, sin mayúsculas, tildes ni separadores, descartando "finca" y los artículos en ambos lados. Cada palabra buscada debe coincidir. No hay coincidencias parciales ni por similitud, para no proponer una finca que la persona no nombró.
- Varias coincidencias se devuelven todas y el agente pide aclaración; ninguna devuelve `[]` y el agente no inventa. Una búsqueda sin palabras distintivas ("finca") devuelve todas, lo que también obliga a preguntar.
- n8n reenvía las palabras del transportista sin normalizarlas y usa sólo el `code` devuelto. El alta no cambia: sigue recibiendo un código exacto.

**Alternativas descartadas.** Hacer que el alta acepte nombres naturales (no permite pedir aclaración y podría asignar la finca equivocada); mantener la lista de códigos en el prompt (duplica el catálogo y queda desactualizada).

## 3. Autenticación

El catálogo no es anónimo. La política `FarmCatalogRead` acepta la clave del canal (herramienta del agente) o la del dashboard mediante un esquema de reenvío que elige uno solo por solicitud, para emitir un único `401`. Ninguna mutación de datos maestros acepta la clave del canal.

El validador de CI prohibía la clave del canal fuera de las rutas de persistencia del canal. Se agrega una única excepción: `GET` al template exacto `{{ $env.AGROFLOW_API_URL }}/api/v1/farms`, sin cuerpo, sin encabezados propios y sólo con los parámetros `active` y `search`. El modelo elige el texto de búsqueda, nunca el destino, el método ni el contenido.

**Riesgo.** Esto amplía el alcance de la clave estática del canal, que es una medida provisoria de LocalDemo mientras `OD-008` sigue pendiente. Cuando se apruebe `OD-008`, esta lectura debe adoptar la identidad de servicio que se defina.

## 4. Errores y estado conversacional en n8n

- Turnos: una lista vacía (`200 []`) y un error (`problem+json` con `code`) llegan al agente como resultados distintos; el prompt prohíbe decir "no tenés turnos" ante un error.
- Fincas: `400 VALIDATION_ERROR` por parámetros no documentados, repetidos, vacíos o de más de 100 caracteres; `401 UNAUTHENTICATED` sin clave válida; `500 INTERNAL_ERROR` ante una falla de persistencia, nunca una lista vacía. Ante cualquier error el agente no llama al alta.
- Preservación de colecciones vacías en `ToolsAgent V3`: en n8n, `HttpRequestV3` con `responseFormat: "json"` y `fullResponse: false` expande un arreglo JSON en un ítem por elemento, de modo que `200 []` produce `0` ítems y `buildSteps.js` serializa la observación de la herramienta como `""` en lugar de `[]`, induciendo al modelo a reportar una falla del sistema cuando un operador cancela o finaliza el único turno activo. Por ello, las herramientas de consulta que devuelven arreglos (`Lookup Appointments By Phone` y `Lookup Farms`) habilitan `fullResponse: true` y entregan siempre un único ítem con `statusCode` y `body` (n8n agrega además `headers` y `statusMessage`, lo que solo suma tokens a la observación). Las herramientas de acción siguen devolviendo el cuerpo directo; una herramienta nueva que devuelva listas debe habilitar `fullResponse: true` para no repetir el defecto.
- Memoria conversacional subordinada al backend (`ADR-003`): `Window Buffer Memory` conserva mensajes previos donde un camión pudo haber tenido `ACTIVE_APPOINTMENT_EXISTS`, pero un operador puede cancelar o finalizar ese turno desde el panel en cualquier momento. Las reglas duras de `AI Agent1` prohíben asumir desde el historial de chat que una patente sigue teniendo (o no) turno activo y rechazar un alta nueva por ese motivo: el agente sigue el flujo normal de solicitud (incluidas sus condiciones para no crear el turno, como fincas ambiguas, inexistentes o una búsqueda fallida) y la API decide.

## 5. Estrategia de pruebas

| Nivel | Cobertura |
|---|---|
| Persistencia (`EfAppointmentStoreTests`) | Hoy, mañana y días posteriores; cancelados y finalizados excluidos; día por defecto conservado sin `active`; `date` + `active`; turno vencido activo; transportista sin turnos y teléfono inexistente. |
| HTTP turnos (`AppointmentsHttpTests`) | `active=true` sin fecha; valores inválidos o repetidos; lista vacía frente a `500`. |
| Búsqueda (`FarmSearchTests`) | Códigos exactos, nombres naturales, tildes, artículos, inexistentes, parciales y ambiguos. |
| HTTP fincas (`FarmsHttpTests`) | Claves, nombres naturales sobre el seed, finca inactiva, validaciones y `500` ante falla de persistencia. |
| Workflow (`workflow.test.mjs`) | Consulta y parámetros de cada herramienta, credenciales, resultados vacíos, ambiguos y errores, ausencia de códigos fijos e instrucciones del agente. |
| Validador (`integrations.test.mjs`) | La excepción de la clave del canal y sus rechazos. |
| Aceptación (`acceptance.mjs`) | Las herramientas contra la API real: turno activo encontrado aunque su ventana sea de otro día y fincas resueltas por nombre. |

El simulador ejecuta las herramientas con los argumentos que elegiría el modelo, pero no simula el modelo: la elección de herramientas y el texto de las respuestas sólo se verifican con OpenAI y WhatsApp reales.
