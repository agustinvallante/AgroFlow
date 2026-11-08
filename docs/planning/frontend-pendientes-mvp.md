# Estado y pendientes del frontend

> **Estado: inventario de trabajo, no normativo.** No aprueba rutas, DTO ni decisiones; lo que falta requiere su cambio OpenSpec y contrato. **Corte:** 2026-11-07.

## Base analizada

| Fuente | Referencia | Estado |
|---|---|---|
| `main` | `48f7533` | Integra el #97 y el #100: contrato `0.5.0`, sin mocks y con auditoría `npm audit` en verde |
| [PR #97](https://github.com/agustinvallante/AgroFlow/pull/97) | `d13528f` | Integrado en `main`. Contrato `0.5.0`: conversaciones, transportistas y lectura de fincas, con resoluciones parciales `LocalDemo` en [open-decisions.md](open-decisions.md) |
| [PR #100](https://github.com/agustinvallante/AgroFlow/pull/100) | `48f7533` | Integrado en `main`. Retira el modo mock: el dashboard muestra únicamente datos de la API |

Los requisitos salen de [OpenSpec](../../openspec/specs/) y de la [aceptación del MVP](../product/mvp-acceptance.md). Las issues F01–F09 (`#67`–`#75`) se contrastaron con sus títulos vigentes en GitHub y con el [corte de backlog](mvp-backlog-sync-2026-10-15.md).

## Cómo leer los estados

| Estado | Significado |
|---|---|
| ✅ Funciona | Implementado contra la API y cubierto por pruebas automatizadas |
| 🟡 Parcial | Existe, pero le falta una parte o tiene un límite conocido |
| ❌ Falta | No existe en el frontend, o no hay API que lo soporte |
| ⛔ Fuera del MVP | Hace falta un cambio aprobado si se quiere incorporar |

**Límites de "Funciona":** todo corre únicamente en el perfil `LocalDemo`, sin usuarios ni roles. La evidencia reúne las 77 pruebas del frontend, la aceptación local automatizada (`chatbot/tests/acceptance.mjs`) y la prueba en `LocalDemo` con WhatsApp real registrada en el #97; el #100 aún tiene pendiente registrar un recorrido manual en el navegador. Nada de esto acredita por sí solo la [aceptación del MVP](../product/mvp-acceptance.md).

## Resumen

El frontend todavía no cubre el MVP. El mayor bloqueo es **acceso y sesión**.

| Sección | Estado | En una línea |
|---|---|---|
| Acceso y sesión | ❌ | No existe; sin esto no se puede usar fuera de la demo |
| Cola de turnos | 🟡 | El recorrido funciona; faltan seguridad, listado completo y paginación |
| Transportistas | 🟡 | Lista, alta y baja funcionan; faltan edición y asociación con camión |
| Camiones | ❌ | Sin rutas ni vista |
| Fincas | 🟡 | La API permite leerlas; sin vista ni gestión |
| Panel general | 🟡 | Solo camiones en curso; indicadores y franjas sin datos |
| Interrupciones | ❌ | Sin rutas ni pantalla |
| Chatbot | 🟡 | Conversaciones reales; métricas y plantillas sin datos |
| Reportes, configuración, mapa | ⛔ | Diseño vacío o sin desarrollar |

## 1. Inventario completo

### 1.1 Acceso y sesión (F01 #67)

| Función | Estado | Para completarla |
|---|---|---|
| Iniciar y cerrar sesión | ❌ | Contrato y pantalla. `POST /api/auth/login` existe, pero no está en OpenAPI ni acredita ingenio y roles del MVP |
| Usuario, rol e ingenio en el menú | ❌ | Hoy dicen "sin datos". Requiere B03 |
| Vencimiento de la sesión | ❌ | Volver al acceso y limpiar datos y pedidos en curso |
| Permisos por rol en la UI | ❌ | Ocultar acciones; la API sigue autorizando (OD-007) |
| Rechazo de acceso a otro ingenio | ❌ | Mostrar el error sin exponer el recurso |
| Usar turnos fuera de la demo | ❌ | Fuera de `LocalDemo`, `AppointmentsAuthorizationPolicy` deniega aunque haya un JWT válido |
| Clave del dashboard | 🟡 | Funciona en loopback como medida de demo. B03 la reemplaza |

**Decisión pendiente:** custodia de la sesión web. No asumir BFF ni token en el navegador.

### 1.2 Cola de turnos (F06 #72)

| Función | Estado | Para completarla |
|---|---|---|
| Alta manual | ✅ | — |
| Listado del día | ✅ | — |
| Filtros por estado, fecha, patente y teléfono | ✅ | — |
| Aviso de filtro incompleto | ✅ | — |
| Búsqueda por texto | ✅ | Solo sobre lo ya cargado |
| Detalle del turno | ✅ | — |
| Cambiar estado (la API valida) | ✅ | — |
| Cancelar | ✅ | — |
| Aviso de datos desactualizados y reintento | ✅ | — |
| Hora en la zona que informa la API | ✅ | — |
| Actualización periódica | 🟡 | Cada 4 s; el intervalo no está aprobado (OD-012) |
| Contadores de la cola | 🟡 | Se cuentan sobre la lista recibida; no son indicadores de la API |
| Elegir la finca en el alta | 🟡 | Hoy es texto libre; se puede usar `GET /api/v1/farms` |
| Listado completo | 🟡 | `AppointmentSummary` no trae transportista, finca ni carga, y se pide el detalle de cada turno |
| Filtro `active` | 🟡 | La API lo ofrece; lo usa el chatbot, no la vista |
| Paginación | ❌ | El listado no la tiene (B00) |
| Mostrar prioridad y flota | ❌ | La API no las informa. Decidir si se muestran; el front no las calcula |
| Reasignar horario | ❌ | Sin operación ni requisito encontrado. Definir si se descarta |

### 1.3 Transportistas (F02 #68, F05 #71, G03)

| Función | Estado | Para completarla |
|---|---|---|
| Listado | ✅ | — |
| Búsqueda y filtro por estado | ✅ | Locales, sobre lo ya cargado |
| Alta con nombre, DNI y WhatsApp | 🟡 | Funciona contra la API; falta validar `dni` vacío en cliente antes de enviar |
| Dar de baja | 🟡 | Funciona contra la API, pero inhabilita sin pedir confirmación previa |
| Ver camión asociado y si puede pedir turnos | ✅ | Solo lectura |
| Editar | 🟡 | `PUT /api/v1/drivers/{id}` existe; falta formulario y método en el repositorio |
| Reintentar tras un error | 🟡 | Hoy el error reemplaza toda la vista |
| Asociar con un camión | ❌ | Tarea 2.4 del #97, bloqueada por OD-002, OD-007 y OD-013. **Sin esto, un transportista nuevo no puede pedir turnos** |
| Reactivar | ❌ | Sin ruta; decisión en OD-013 |
| Historial | ❌ | Sin ruta |
| Finca habitual, flota, turnos del mes | ❌ | El contrato no los incluye. Decidir si hacen falta |

### 1.4 Camiones (F03 #69, F05 #71, G03)

| Función | Estado | Para completarla |
|---|---|---|
| Listado, alta, edición e inhabilitación | ❌ | Sin rutas ni vista. La entidad `Camion` existe en el backend |
| Patente y tipo de flota | ❌ | Depende de lo anterior |
| Sección en el menú | ❌ | Definir si es propia o parte de otra |

### 1.5 Fincas (F04 #70, G03)

| Función | Estado | Para completarla |
|---|---|---|
| Lectura por API, con búsqueda | 🟡 | `GET /api/v1/farms` existe; hoy solo la usa el chatbot |
| Vista y listado | ❌ | Sin pantalla |
| Alta, edición e inhabilitación | ❌ | Sin rutas; campos pendientes en OD-013 |

### 1.6 Panel general (F07 #73, G06)

| Función | Estado | Para completarla |
|---|---|---|
| Camiones en curso | ✅ | — |
| Cambiar estado desde el panel | ✅ | — |
| Aviso si la API falla, sin mostrar "0 activos" | ✅ | — |
| Reloj y fecha | ✅ | Hora del navegador |
| Indicadores del día | ❌ | Operación de dashboard; fórmulas en OD-012. El front no los calcula |
| Franjas con cupo, ocupación y cupos restantes | ❌ | Exponerlas por API. `Ventana` ya tiene `Capacity` y `Occupied` |
| Interrupción activa destacada | ❌ | Depende de 1.7 |
| Feed del chatbot | ❌ | Se muestra vacío. Definir si se conecta a conversaciones o se quita |
| Estado de molienda | ❌ | Probablemente pasa a ser la interrupción; confirmar |
| Aviso manual a la flota | ❌ | Sin requisito encontrado; depende de OD-010 |

### 1.7 Interrupciones (F08 #74, G07)

| Función | Estado | Para completarla |
|---|---|---|
| Registrar y actualizar (una activa por ingenio) | ❌ | Rutas y pantalla |
| Consultar la activa | ❌ | Rutas |
| Ver turnos `ASIGNADO` reprogramados | ❌ | La API los determina; el front no los predice |
| Ver turnos `EN_CAMINO`/`EN_ESPERA` identificados | ❌ | Rutas; destinatarios según OD-010 |

### 1.8 Chatbot

| Función | Estado | Para completarla |
|---|---|---|
| Ver conversaciones | 🟡 | Muestra solo las primeras 50: no usa `nextCursor` de la API |
| Ver mensajes de una conversación | 🟡 | Recorre hasta 20 páginas de 50 en orden ascendente: con más de 1000 mensajes se pierden los más nuevos sin aviso |
| Cambiar de conversación | 🟡 | `useChatbot` no limpia `mensajes` ni `errorMensajes` mientras carga la conversación seleccionada |
| Actualización automática y manual | ✅ | Cada 15 s (relee todas las páginas de la conversación abierta) |
| Remitente reconocido o no registrado | ✅ | Dirección enmascarada por la API |
| Estado de envío y de procesamiento | ✅ | Nunca afirma una entrega |
| Marca de revisión requerida | ✅ | `needsAttention` |
| Fecha en la conversación abierta | 🟡 | `ChatTranscript` antepone "Hoy" aunque `fechaHoraEnZonaIngenio` ya incluya la fecha de otro día |
| Reintentar tras un error de carga | 🟡 | El error inicial reemplaza toda la vista |
| Métricas | ❌ | Sin endpoint ni propuesta. Decidir si se quitan o se proponen |
| Plantillas | ❌ | Sin endpoint ni propuesta. Ídem |
| Conversación resuelta o escalada | ❌ | La API solo informa `needsAttention` |
| WhatsApp real de punta a punta | ✅ | Tarea 9.1 verificada en `LocalDemo` (PR #97); queda pendiente consolidar deltas en la tarea 9.2 |

### 1.9 Fuera del MVP, salvo cambio aprobado

El [corte de backlog](mvp-backlog-sync-2026-10-15.md) indica que reportes, mapa y configuración editable requieren su propio cambio.

| Sección | Estado | Qué necesitaría |
|---|---|---|
| **Reportes** | ⛔ Diseño vacío | Rango, espera promedio, reducción frente a asignación manual, turnos totales, % por bot, espera por día, distribución de flota, fincas con mayor espera y exportación |
| **Configuración** | ⛔ Diseño vacío | Ingenio, reglas, notificaciones, integración n8n y usuarios. OD-004 define duración y cupo por datos iniciales, no desde esta pantalla |
| **Mapa simulado** | ⛔ No existe | Coordenadas de fincas e ingenio, tiempo de viaje, hora de salida y llegada estimada. `Finca.LocationReference` es texto y `Turno` no guarda la hora de sus cambios de estado. Antes hay que aclarar qué tramo representa `EN_CAMINO`: el chatbot lo describe como salida *hacia la finca* |

### 1.10 Calidad y publicación (F09 #75)

| Función | Estado | Para completarla |
|---|---|---|
| Pruebas unitarias y de vistas | ✅ | 77 pruebas; ampliar con cada función nueva |
| Proxy de desarrollo | ✅ | Vite, en `dev` y `preview` |
| Auditoría de dependencias | ✅ | `npm audit --audit-level=high` sin vulnerabilidades en `main` |
| Accesibilidad | 🟡 | Filas y opciones del menú clicables, sin navegación por teclado |
| Pruebas de navegador de punta a punta | ❌ | No existen |
| Publicar `dist/` | ❌ | Requiere otro proxy; la clave del dashboard no sirve fuera de la demo |
| Evidencia `MVP-G01` a `MVP-G12` | ❌ | Dos recorridos fechados, reinicio y respaldo |

## 2. Dónde queda cada función del #97 y el #100

### 2.1 PR #97: datos reales

| Función | Pantalla | Frontend | Ruta de la API | Backend |
|---|---|---|---|---|
| Ver conversaciones | Chatbot | `HttpConversacionRepository`, `conversationMapper`, `useChatbot`, `ConversationList` | `GET /api/v1/conversations` | `ConversationsController`, `ConversationService`, `EfConversationStore` |
| Ver mensajes y su estado | Chatbot | Lo anterior y `ChatTranscript` | `GET /api/v1/conversations/{id}/messages` | Ídem |
| Listar y ver transportistas | Transportistas | `HttpTransportistaRepository`, `driverMapper`, `useTransportistas` | `GET /api/v1/drivers`, `GET /api/v1/drivers/{id}` | `DriversController`, `MasterDataService` |
| Alta de transportista | Transportistas | Ídem | `POST /api/v1/drivers` | Ídem |
| Dar de baja | Transportistas | Ídem | `POST /api/v1/drivers/{id}/inactivation` | Ídem |
| Enviar la clave sin exponerla | Chatbot, Transportistas | `server/dashboardProxy.ts`, `vite.config.ts` | Encabezado `X-AgroFlow-Dashboard-Key` | `ApiKeyAuthentication` |
| Fechas en la zona del ingenio | Chatbot | `shared/utils/date.ts` | — | — |

**Disponible en la API, sin uso en la UI:**

| Ruta | Quién la usa hoy | Destino sugerido |
|---|---|---|
| `PUT /api/v1/drivers/{id}` | Nadie en la UI | Editar en Transportistas |
| `GET /api/v1/farms` | Chatbot | Alta de turno y futura vista de fincas |
| `GET /api/v1/conversations/{id}` | Nadie en la UI | Sin necesidad por ahora |
| `active=true` en turnos | Chatbot | Sin necesidad por ahora |

**Sin pantalla, porque son del canal:**

| Función | Ruta | Quién la usa |
|---|---|---|
| Registrar mensajes entrantes | `POST /api/v1/conversation-events` | n8n |
| Cerrar el procesamiento | `PATCH /api/v1/conversation-events/{messageId}/processing` | n8n |
| Registrar la respuesta antes de enviarla | `POST /api/v1/conversation-reply-intents` | n8n |
| Informar el resultado del envío | `PATCH /api/v1/conversation-reply-intents/{replyId}/send-status` | n8n |

El proxy bloquea estas rutas para el navegador. El #97 también cambia `chatbot/workflows/Chatbot.json`, agrega la herramienta `Lookup Farms`, el filtro `active=true` y la precarga privada de transportistas en `config/local-demo/`.

### 2.2 PR #100: solo datos de la API

| Función | Pantalla | Archivos | Efecto |
|---|---|---|---|
| Quitar mocks y `VITE_DATA_SOURCE` | Todas | `infrastructure/mock` (eliminado), `composition/container.ts` | El dashboard siempre usa la API |
| Mantener el diseño sin datos | Panel, Chatbot, Reportes, Configuración | Vistas, `components/common/sinDatos.ts` | `—`, listas vacías y acciones deshabilitadas |
| Camiones en curso con estados de carga | Panel | `PanelGeneralView.tsx` | Si la API falla, no muestra "0 activos" |
| Dejar en Transportistas solo lo del contrato | Transportistas | `TransportistasView.tsx`, `Transportista.ts`, `useTransportistas.ts`, `driverMapper.ts` | Sin finca, flota ni turnos del mes |
| Dejar en Chatbot solo lo de la API | Chatbot | `ChatbotView.tsx`, `useChatbot.ts`, `ConversacionRepository.ts`, `HttpConversacionRepository.ts` | Métricas y plantillas sin datos |
| Quitar campos sin contrato de los turnos | Cola, Panel | `Turno.ts`, `appointmentMapper.ts`, `ColaTurnosView.tsx`, `Badges.tsx` | Sin flota, canal, espera, prioridad ni reasignar |
| Quitar sesión e ingenio fijos | Menú | `Sidebar.tsx` | Dicen "sin datos" |
| Pruebas | — | `PanelGeneralView.test.tsx`, `TransportistasView.test.tsx`, `SeccionesSinApi.test.tsx` | Cubren lo anterior |

**Estado de integración:** el #97 y el #100 ya fueron integrados en `main` en ese orden.

## 3. Qué hace falta para avanzar

### 3.1 Cierre inmediato sobre lo integrado

1. **Integración del #97 y el #100:** completada en `main`, con el alcance `LocalDemo` aprobado como resolución parcial en [open-decisions.md](open-decisions.md), [propuesta OpenSpec](../../openspec/changes/persistir-conversaciones-whatsapp/proposal.md) sincronizada, `npm audit` en verde y el retiro del modo mock acordado conforme a [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md).
2. **Pendiente inmediato:** registrar el recorrido manual del #100 en el navegador.

### 3.2 Lo que solo depende del frontend

| Tarea | Sección |
|---|---|
| Limpiar `mensajes` y `errorMensajes` al cambiar de conversación en `useChatbot` | 1.8 |
| Evitar perder mensajes nuevos al superar las 20 páginas en `HttpConversacionRepository` | 1.8 |
| Recorrer todas las conversaciones con `nextCursor` | 1.8 |
| Corregir el prefijo "Hoy" en `ChatTranscript` para fechas de otros días | 1.8 |
| Pedir confirmación antes de inhabilitar un transportista y validar `dni` vacío en cliente | 1.3 |
| Formulario para editar transportistas | 1.3 |
| Elegir la finca en el alta de turno | 1.2 |
| Reintentar tras un error en Chatbot y Transportistas | 1.3, 1.8 |
| Limpiar tipos de dominio exportados que quedaron sin uso tras retirar los mocks | 2.2 |
| Navegación por teclado | 1.10 |

### 3.3 Decisiones que bloquean

| Decisión | Bloquea |
|---|---|
| Custodia de la sesión web | Acceso y sesión |
| B00 (rutas, DTO, paginación) | Todo lo que necesita contrato nuevo |
| OD-007 (roles y permisos) | Sesión, catálogos, interrupciones |
| OD-002, OD-013 (asociaciones, campos de camiones/fincas, reactivación) | Transportistas, camiones, fincas |
| OD-012 (indicadores y cola) | Panel, intervalo de actualización |
| OD-010 (notificaciones no conversacionales, fuera de lo resuelto parcialmente en `LocalDemo`) | Interrupciones, avisos proactivos |
| OD-009, OD-011 (retención definitiva y operación fuera de `LocalDemo`) | Cierre productivo del historial del chatbot |

### 3.4 Orden sugerido

1. Resolver la sección 3.1.
2. Hacer la sección 3.2, que no bloquea a nadie.
3. Cerrar B00 con backend: contrato y decisiones del primer recorrido autenticado.
4. Implementar acceso y sesión (F01) y luego el listado de turnos (F06).
5. Completar catálogos y asociación, para que un transportista nuevo pueda pedir turnos.
6. Conectar panel e interrupciones (F07, F08).
7. Decidir qué pasa con reportes, configuración y mapa, y reunir la evidencia final (F09).
