# Auditoría funcional para la presentación de AgroFlow

Fecha del corte 2026-11-14, zona America/Buenos_Aires. La revisión comprende código, contratos, persistencia y ejecución local. Prioriza el recorrido de exposición acordado con el equipo. Es evidencia de auditoría y planificación, subordinada a OpenSpec.

## Alcance acordado

El recorrido principal será WhatsApp real → solicitud de turno → asignación automática → consulta → aviso EN_CAMINO → visualización en el dashboard → estados operativos → finalización. Un segundo recorrido mostrará cancelación y reutilización del cupo. Habrá una simulación del canal como respaldo. El mapa puede usar viajes ficticios claramente identificados.

Login y usuarios quedan fuera del guion. Interrupciones, reprogramación y avisos masivos continúan para el MVP. No se requiere completar reportes ni configuración editable para esta exposición.

## Versiones revisadas

| Fuente | SHA | Resultado |
|---|---|---|
| main | `8873a2644b2fd427e571c832b04f5dfa83647a9a` | Incluye la #104 de conversaciones |
| [#105](https://github.com/agustinvallante/AgroFlow/pull/105) | `2c88394e7fc902603cbf95fdf5943aca58790a3f` | Mapa independiente con ingreso manual y persistencia en el navegador. Está en borrador |
| [#106](https://github.com/agustinvallante/AgroFlow/pull/106) | `8ce785294a841de44dd009aa7d359a24182cf0b4` | Lecturas autenticadas en un perfil separado. El guion de escritura y bot utiliza LocalDemo |
| [#107](https://github.com/agustinvallante/AgroFlow/pull/107) | `40742714b67f91256bb0de50081b283a77889ebb` | Generador y borrador incompleto de ERS. Sin efecto en los flujos operativos |

Se construyó una copia aislada de main y otra con las tres ramas combinadas mediante merges exclusivamente locales. La combinación no presentó conflictos y su SHA local es `1b25fc108485005f166f368fc6d9569f00aab090`. Esto prueba compatibilidad de ese candidato, no integración ni aprobación de las PRs.

## Evidencia de ejecución

Se ejecutó en Windows con Node 24.13.0 y SDK .NET 9.0.203, compilando los proyectos net8.0. Se usaron bases SQLite nuevas y ficticias, claves locales aleatorias y Chrome sin interfaz mediante Playwright. No se tocaron bases del equipo ni archivos privados de transportistas.

| Verificación | main | Candidato con PRs |
|---|---|---|
| Backend compilado y probado | 352 pruebas aprobadas | 371 pruebas aprobadas |
| Frontend probado y compilado | 87 pruebas aprobadas | 124 pruebas aprobadas |
| Simulaciones de integración y workflow | 106 pruebas aprobadas | 106 pruebas aprobadas |
| Aceptación existente de API, SQLite, canal simulado, proxy y reinicio | Recorrido completo aprobado | Recorrido completo aprobado |
| Alta manual y ciclo por navegador | Hasta FINALIZADO al seleccionar fecha asignada | Hasta FINALIZADO al seleccionar fecha asignada |
| Canal simulado con solicitar, consultar e informar EN_CAMINO | Estado persistido comprobado | Estado persistido comprobado |
| Mapa por navegador | No incluido en main | Alta manual de ingenio, finca y viaje, avance x60 y persistencia tras recarga comprobados |

En el candidato pasaron OpenSpec estricto, ambos contratos OpenAPI, validación de exportación y configuración Compose. El lint pasó con las advertencias ya existentes de hooks y una prueba de conversaciones. Redocly conserva la advertencia de health sin respuesta 4xx. Son observaciones secundarias para esta presentación.

WhatsApp, Evolution y el agente fueron simulados en las pruebas automatizadas del canal. Docker Engine estaba detenido y no había un entorno real del equipo conectado a esta copia. La entrega y recepción real de mensajes y la selección de herramientas por el modelo quedan pendientes del ensayo A03. La configuración Compose se validó sin arrancar contenedores.

## Recorridos y endpoints

| Recorrido | Operaciones ejercitadas | Estado |
|---|---|---|
| Arranque y salud | `GET /health`, migración y seed de SQLite | Funciona |
| Solicitud manual o desde herramienta del bot | `POST /api/v1/appointments` con teléfono, patente, finca, corte y carga | Funciona. La fecha siguiente puede quedar oculta en la UI |
| Consulta | `GET /api/v1/appointments` con fecha, estado, patente, teléfono y `active=true` según contrato LocalDemo | Funciona. El panel no utiliza la consulta de activos de varias fechas |
| Detalle | `GET /api/v1/appointments/{id}` | Estado y ventana persistidos comprobados |
| Ciclo | `POST /api/v1/appointments/{id}/transitions` | Secuencia completa aprobada. Saltos y cambios desde terminales rechazados con 409 |
| Cancelación | La misma operación de transición con CANCELADO | Reduce el cupo ocupado una vez y permite reutilizarlo |
| Transportistas | Listado, detalle, alta, edición HTTP e inhabilitación en `/api/v1/drivers` | Funcionan. La UI no ofrece edición ni asociación de camión |
| Fincas para el bot | `GET /api/v1/farms?active=true&search=norte` | Encuentra la finca y devuelve su código |
| Ingesta del canal | `POST /api/v1/conversation-events` | Reconoce remitente sembrado y persiste antes de responder al webhook |
| Respuesta y resultado | `POST /api/v1/conversation-reply-intents` y `PATCH /api/v1/conversation-reply-intents/{id}/send-status` | Recorrido simulado comprobado, incluida deduplicación |
| Fallas de procesamiento | Actualizaciones de `/api/v1/conversation-events/{id}/processing` | Cubiertas por la suite del canal |
| Historial del dashboard | Listado, detalle y mensajes en `/api/v1/conversations` | Se mostraron seis mensajes persistidos. La recuperación de una falla inicial presenta A06 |
| Mapa simulado | Formularios de la #105 y localStorage | Funciona como simulación independiente |

La suite de aceptación conserva mensajes tras reiniciar y evita procesar de nuevo el mismo evento. Se verificaron dos ciclos completos por API en cada base y ciclos de navegador. Las herramientas reales de la API fueron invocadas desde el simulador del workflow para solicitar, consultar y pasar a EN_CAMINO.

## Hallazgos que se deben resolver o ensayar

### A01 — P1 — El turno creado queda fuera de la vista sin confirmación

Después del horario disponible del día, la API asigna una ventana del día siguiente. En el ensayo del 14/11 asignó ventanas del 15/11. El formulario se cierra y la cola sigue con fecha vacía, equivalente a hoy, mostrando cero turnos. El modelo de frontend conserva la hora de ventana y pierde su fecha completa. El detalle también muestra sólo horas para la ventana.

Se reprodujo en main y en el candidato mediante el navegador. Elegir manualmente el día asignado permite continuar. El bot sí encuentra el turno porque consulta activos de cualquier fecha. La corrección debe devolver al formulario el turno persistido, mostrar fecha y hora y facilitar abrirlo en la fecha correcta, respetando los demás filtros.

Archivos principales en `frontend/src/` — `domain/entities/Turno.ts`, `infrastructure/http/mappers/appointmentMapper.ts`, `presentation/hooks/useColaTurnos.ts` y `presentation/views/ColaTurnosView.tsx`. Relacionada con #72. [Captura del resultado oculto](assets/2026-11-14/turno-oculto.png)

### A02 — P1 — El panel pierde camiones en curso de otra fecha

Con un turno del día siguiente ya en EN_CAMINO, el panel sigue mostrando cero camiones en curso. Usa el hook del listado diario y luego filtra estados en el navegador. El estado sí existe en SQLite y en la consulta `active=true`.

Se reprodujo en main y candidato. El panel necesita consultar activos de cualquier fecha. Conviene definir explícitamente ese modo en el repositorio HTTP y sus consumidores, manteniendo la consulta diaria para la cola. La fecha de la ventana debe mostrarse al incluir varios días.

Archivos principales — `PanelGeneralView.tsx`, `useColaTurnos.ts`, `TurnoRepository.ts`, `TurnoUseCases.ts`, `HttpTurnoRepository.ts` y sus pruebas. Relacionada con #73.

### A03 — P1 — Falta evidenciar el circuito real de WhatsApp del candidato final

La simulación prueba la API y la persistencia, pero no verifica el emparejamiento de Evolution, la importación y publicación del workflow, las credenciales de cada nodo, el JID recibido realmente ni la interpretación del modelo.

El ensayo debe usar un teléfono consentido asociado a un camión, resolver la finca por nombre, crear el turno confirmado, consultar su fecha real e informar EN_CAMINO. Después debe comprobar el mismo estado en dashboard y SQLite, cancelar desde el panel, consultar la lista vacía y volver a solicitar. Repetir tras reiniciar sin perder el historial.

Usar `chatbot/workflows/Chatbot.json`, `infra/compose.yaml`, `infra/env.example`, la precarga privada de `config/local-demo/` y el procedimiento de [demo local](../development/local-demo.md). La credencial del canal también debe vincularse a Lookup Farms. Un evento `@lid` requiere diagnóstico del origen y reconocimiento, sin inventar un teléfono. Relacionada con #42.

### A04 — P1 — Falta un ensayo repetible del guion final y su alternativa local

Los endpoints funcionan con los datos sembrados, pero crear un transportista nuevo no lo asocia a un camión. En el ensayo se registró con éxito y `canRequestAppointments=false`. Intentar reservarle usando un camión ajeno devuelve 404. El menú permite esa alta, por lo que el guion debe preparar claramente los actores y vehículos que sí pueden solicitar.

La aceptación técnica ya disponible es un punto de partida. Hace falta fijar el SHA final, los dos actores con camiones y fincas, la fecha asignada, el recorrido manual y el del bot, y una alternativa local ensayada si falla Internet, Evolution o el modelo. La alternativa debe mostrar API y persistencia reales y rotular el canal simulado.

El mapa de #105 requiere cargar un ingenio, finca y viaje ficticios y conservar su identificación como simulación. Su documentación OpenSpec y el cierre de la revisión del borrador quedan a cargo de esa PR. Una demostración del mapa no demuestra geolocalización real ni cambia estados de turnos.

[Captura del viaje simulado ensayado](assets/2026-11-14/mapa-simulado.png)

Entregables de QA en `docs/development/` y, si se automatiza el respaldo, en `chatbot/tests/`, reutilizando `acceptance.mjs` y `n8n-simulator.mjs`. Relacionada con #13 y #75.

### A05 — P2 — Una patente inválida provoca un error interno

`GET /api/v1/appointments?truckPlate=------` respondió `500 INTERNAL_ERROR` en ambos perfiles LocalDemo auditados. El controlador acepta la longitud, pero el normalizador descarta los separadores y lanza una excepción. La respuesta debe ser una validación 400 por campo. Se debe cubrir también la solicitud de alta y evitar convertir excepciones inesperadas en validaciones genéricas.

Archivos principales — `AppointmentsController.cs`, `AppointmentService.cs`, `EfAppointmentStore.cs`, validadores y pruebas HTTP, junto con el contrato LocalDemo. La validación de las lecturas del perfil MVP de #106 no corrige esta ruta de la demo. Relacionada con #11.

### A06 — P2 — El chatbot queda atascado después de una falla inicial de carga

En el navegador se hizo responder 503 a la primera consulta de conversaciones y luego se restituyó la API. Tras más de un intervalo de refresco, la pantalla seguía mostrando el error inicial y no ofrecía Actualizar ni Reintentar. `recargar` actualiza conversaciones y el error de refresco, pero no limpia el error inicial que bloquea toda la vista.

La misma presentación inicial de error en Transportistas tampoco ofrece recuperación en la vista. Navegar a otra sección y regresar permite reintentar, pero debe existir recuperación explícita y comprobarse la recuperación automática del chatbot.

Archivos principales — `useChatbot.ts`, `ChatbotView.tsx`, `useTransportistas.ts`, `TransportistasView.tsx` y pruebas de carga/error. Relacionada con #68 y #75.

## Observaciones secundarias y trabajo del MVP

| Observación | Tratamiento para la presentación | Seguimiento |
|---|---|---|
| El selector ofrece saltos, retrocesos y cancelación en terminales | La API los rechaza correctamente. Usar la secuencia del guion | #72 y #32/#33 |
| Dar de baja un transportista no pide confirmación y no hay reactivación en la UI | Evitar dar de baja actores del ensayo. Usar un actor separado para mostrar la baja | #68 |
| Se admitió una solicitud con corte diez días en el futuro | Falta cerrar la regla temporal en OD-005. El guion usa un corte pasado válido | #7 y #31 |
| El frontend lee una página de 50 conversaciones y hasta 1000 mensajes por conversación | Suficiente para el ensayo pequeño. No afirmar historial completo a partir de esa cantidad | #75 y evolución del historial |
| El panel tiene indicadores, franjas, molienda y avisos sin datos | Exponer sólo lo implementado. Los contadores operativos de la cola sí usan datos de API | #39, #44 y #73 |
| Interrupciones y avisos no tienen recorrido funcional | Acordado continuar después de la presentación | #37, #38, #40, #41 y #74 |
| No hay CRUD HTTP/UI completo de camiones, fincas y asociaciones | Usar actores sembrados y documentados | #19–#28, #43 y #69–#71 |
| LocalDemo y el perfil autenticado de #106 tienen formas de listado y capacidades distintas | Ejecutar LocalDemo para mostrar creación, bot y ciclo de vida | #7, #9, #10, #11, #29/#30 y #67 |
| #107 agrega un generador y un esqueleto incompleto de ERS | Coordinar por separado la entrega académica | #102 |

## Base de datos y concurrencia

En las dos bases, `PRAGMA integrity_check` devolvió ok, `foreign_key_check` no devolvió filas, no se observaron ventanas sobreasignadas ni camiones con más de un turno activo. La base principal aplicó ocho migraciones y la candidata nueve por Identity.

Se enviaron cuatro altas paralelas de camiones distintos en main. Ninguna ventana superó su cupo de dos. Cinco solicitudes simultáneas de un mismo camión produjeron una creación y cuatro conflictos. Dos cancelaciones concurrentes produjeron un éxito y un conflicto y redujeron el cupo una sola vez. Estas pruebas sostienen el recorrido pequeño observado.

Se observó `journal_mode=wal` en los archivos de prueba. Esto describe esas bases y no resuelve por sí solo respaldo, recuperación o capacidad operativa del MVP. El esquema tiene referencias de dominio sin todas las restricciones previstas en #9. Las comprobaciones de integridad observadas no demuestran las restricciones todavía ausentes.

## Levantar y probar

Requisitos del recorrido local — SDK .NET 8 o compatible, Node con soporte para las herramientas del repositorio y dos terminales. CI usa Node 22. El frontend integrado se ejecuta desde `frontend/`.

Desde la raíz del repositorio, preparar dependencias:

```powershell
dotnet restore backend/AgroFlow.sln
dotnet build backend/AgroFlow.sln
npm --prefix frontend ci
```

En la terminal de API, configurar `LocalDemo__Enabled=true`, `ChannelIntegration__ServiceKey` y `Dashboard__ApiKey` con dos claves distintas de al menos 32 bytes. Generar cada clave localmente con `[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))` y conservarlas en configuración privada.

```powershell
$env:LocalDemo__Enabled = 'true'
dotnet run --project backend/AgroFlow.Api --launch-profile http --urls http://127.0.0.1:5000
```

El comando usa el archivo SQLite de la demo. Si ya había datos, conservar la selección explícita y el procedimiento de respaldo del [runbook](../development/local-demo-runbook.md). Para un ensayo descartable, seleccionar antes una ruta nueva y explícita en `ConnectionStrings__AgroFlowDb`. Reutilizar esa misma ruta al reiniciar para probar persistencia.

En la terminal de frontend, configurar `AGROFLOW_DASHBOARD_KEY` con el mismo valor privado de `Dashboard__ApiKey` y `VITE_API_URL=http://127.0.0.1:5000`. También pueden guardarse en `frontend/.env.local`, ignorado por Git.

```powershell
npm --prefix frontend run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Abrir `http://127.0.0.1:5173` y comprobar `http://127.0.0.1:5000/health`. Sin las claves configuradas funcionan los turnos, pero los endpoints protegidos de transportistas e historial responden 401.

Datos ficticios iniciales para dos recorridos:

| Transportista | Patente | Finca |
|---|---|---|
| `+5493815550101` | `AF123BC` | `FINCA-NORTE` |
| `+5493815550102` | `AF456DE` | `FINCA-SUR` |

La precarga privada del equipo cambia los teléfonos de esos actores. Usar entonces sus números autorizados y las mismas asociaciones preparadas.

1. Crear un turno con corte pasado y carga positiva. Anotar la fecha y hora reales de la respuesta.
2. Hasta resolver A01, seleccionar esa fecha en la cola. Abrir el detalle y filtrar por patente y estado.
3. Avanzar por ASIGNADO → EN_CAMINO → EN_ESPERA → INGRESADO → EN_DESCARGA → FINALIZADO.
4. Crear otro turno, cancelarlo desde un estado permitido y volver a solicitar con ese camión. Comprobar el cupo liberado.
5. Reiniciar sobre el mismo archivo y verificar que persisten los datos. Repetir el ensayo.
6. Para #105, configurar ingenio en `-26.82, -65.22`, finca en `-26.84, -65.24` y un viaje ficticio a 40 km/h. Probar x60 y recargar el mismo navegador para comprobar persistencia.

Para WhatsApp real, iniciar Docker, preparar `infra/.env`, importar el workflow del SHA elegido, vincular sus credenciales y publicar el workflow. Vincular Evolution al número del bot y la instancia `agroflow-demo`. El teléfono del remitente debe corresponder a uno de los actores con camión asociado. Seguir el [procedimiento completo](../development/local-demo.md) y cerrar A03 con evidencia.

El respaldo automatizado existente se ejecuta desde la raíz con `node chatbot/tests/acceptance.mjs` después de compilar en Debug e instalar frontend. Usa API y SQLite reales, puertos 5099/5179 y canal/agente simulados. Permite comprobar la integración cuando el servicio externo falla, pero se debe preparar además su guion visible en A04.

## Condición de cierre de la auditoría

Priorizar A01 y A02 y ejecutar A03 y A04 sobre el SHA que se presentará. Resolver A05 y A06 antes si hay capacidad, porque tienen reproducciones pequeñas. Los demás puntos continúan en las issues del MVP ya existentes.

Las issues acotadas de esta auditoría se vinculan abajo para repartirlas. Cada implementación debe actualizar su capacidad OpenSpec y contrato cuando cambie el comportamiento, conservar LocalDemo e incluir evidencia del flujo que corrige.

<!-- audit-issues -->
