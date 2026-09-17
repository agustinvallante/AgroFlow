# Matriz de reglas de negocio del MVP

## Autoridad documental

El PDF `AgroFlow_Casos_de_Uso_MVP_con_Diagramas.pdf` es una fuente histórica de relevamiento. Esta matriz conserva literalmente sus 54 reglas numeradas y sus 7 adendas para mantener la trazabilidad, pero no reemplaza la especificación ejecutable.

La normativa vigente del producto reside en `openspec/specs/`. Si existe una diferencia entre este documento histórico y una especificación OpenSpec aprobada, prevalece OpenSpec. Las definiciones aún no resueltas deben permanecer en `docs/planning/open-decisions.md` y tramitarse mediante `openspec/changes/` antes de convertirse en normativa.

## Estados

- **Especificada:** la regla expresa un comportamiento normativo utilizable como base de escenarios OpenSpec.
- **Decisión pendiente:** el PDF exige considerar el tema, pero deja abierta una decisión que impide cerrar su comportamiento exacto.

## CU-001 - Autenticación de Usuario

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-001 | Solo los usuarios registrados y activos pueden autenticarse en AgroFlow. | `acceso-y-autorizacion` | Especificada |
| RN-002 | Las contraseñas nunca deben almacenarse en texto plano; deben almacenarse mediante un algoritmo seguro de hashing. | `acceso-y-autorizacion` | Especificada |
| RN-003 | Todo usuario autenticado debe tener asignado un rol válido. | `acceso-y-autorizacion` | Especificada |
| RN-004 | El token JWT debe identificar al usuario y contener su rol para permitir la autorización. | `acceso-y-autorizacion` | Especificada |
| RN-005 | Los usuarios solo pueden acceder a operaciones autorizadas para su rol. | `acceso-y-autorizacion` | Especificada |
| RN-006 | Un token expirado no permite acceder a recursos protegidos y la API debe responder 401 Unauthorized. | `acceso-y-autorizacion` | Especificada |
| RN-007 | Ante un usuario inexistente o una contraseña incorrecta se debe devolver un mensaje genérico de credenciales inválidas. | `acceso-y-autorizacion` | Especificada |

## CU-002 - Solicitar Turno

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-008 | La asignación de turnos debe ser realizada exclusivamente por el backend de AgroFlow. | `turnos-solicitud-y-asignacion` | Especificada |
| RN-009 | Todo turno debe poseer una ventana horaria de llegada. | `turnos-solicitud-y-asignacion` | Especificada |
| RN-010 | La prioridad debe considerar el tiempo transcurrido desde el corte de la caña. | `turnos-solicitud-y-asignacion` | Decisión pendiente |
| RN-011 | La prioridad debe contemplar la diferenciación entre flota propia y flota de terceros. | `turnos-solicitud-y-asignacion` | Decisión pendiente |
| RN-012 | Un mismo camión no puede poseer simultáneamente más de un turno activo. | `turnos-solicitud-y-asignacion` | Especificada |
| RN-013 | Un turno solo puede informarse como confirmado cuando haya sido persistido correctamente por el backend. | `turnos-solicitud-y-asignacion` | Especificada |
| RN-014 | En solicitudes originadas por WhatsApp, el transportista se identifica por el número telefónico del remitente. | `integracion-whatsapp-n8n` | Especificada |
| RN-015 | El camión se identifica mediante una patente única y debe encontrarse activo. | `turnos-solicitud-y-asignacion` | Especificada |
| RN-016 | Si la ventana solicitada no tiene disponibilidad, AgroFlow asigna automáticamente la próxima ventana disponible considerando las reglas de prioridad. | `turnos-solicitud-y-asignacion` | Decisión pendiente |

## CU-003 - Consultar Turno

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-017 | Un transportista solo puede consultar turnos correspondientes a vehículos que esté autorizado a utilizar. | `turnos-consulta` | Especificada |
| RN-018 | Las consultas vía WhatsApp identifican al transportista por su número telefónico y al vehículo por su patente. | `integracion-whatsapp-n8n` | Especificada |
| RN-019 | La consulta debe devolver siempre el estado actual almacenado en AgroFlow; n8n no es la fuente de verdad del turno. | `turnos-consulta` | Especificada |
| RN-020 | Consultar un turno no debe alterar su estado, horario, prioridad ni ningún otro dato. | `turnos-consulta` | Especificada |

## CU-004 - Consultar Turnos del Ingenio

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-021 | Un operador solo puede consultar turnos correspondientes al ingenio al que se encuentra asociado. | `turnos-consulta` | Especificada |
| RN-022 | La consulta del listado de turnos no modifica ningún dato. | `turnos-consulta` | Especificada |
| RN-022A | Por defecto se consultan los turnos del día actual y se ordenan por horario ascendente. | `turnos-consulta` | Especificada |
| RN-022B | El MVP permite filtrar por fecha, estado y patente. | `turnos-consulta` | Especificada |

## CU-005 - Consultar Detalle de Turno

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-023 | Un usuario interno solo puede consultar detalles de turnos pertenecientes a su ingenio. | `turnos-consulta` | Especificada |
| RN-024 | Consultar el detalle de un turno no modifica su estado, prioridad, horario ni ningún otro dato. | `turnos-consulta` | Especificada |
| RN-025 | Toda la información mostrada debe provenir del backend de AgroFlow y reflejar su estado actual. | `turnos-consulta` | Especificada |

## CU-006 - Actualizar Estado de Turno

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-026 | El ciclo operativo normal del MVP es ASIGNADO -> EN_CAMINO -> EN_ESPERA -> INGRESADO -> EN_DESCARGA -> FINALIZADO. | `turnos-ciclo-de-vida` | Especificada |
| RN-027 | Un operador solo puede modificar turnos pertenecientes a su ingenio. | `turnos-ciclo-de-vida` | Especificada |
| RN-028 | Un turno FINALIZADO no puede volver a un estado operativo anterior. | `turnos-ciclo-de-vida` | Especificada |
| RN-029 | Un turno CANCELADO no puede continuar avanzando por el ciclo operativo. | `turnos-ciclo-de-vida` | Especificada |
| RN-030 | EN_CAMINO puede ser informado por el transportista vía WhatsApp/n8n; EN_ESPERA, INGRESADO, EN_DESCARGA y FINALIZADO son actualizados por el operador en el MVP. | `turnos-ciclo-de-vida` | Especificada |
| RN-030A | Un nuevo estado solo se considera efectivo cuando fue persistido correctamente en el backend. | `turnos-ciclo-de-vida` | Especificada |

## CU-007 - Registrar Interrupción Operativa

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-031 | Solo un usuario autorizado del ingenio puede registrar o actualizar una interrupción operativa. | `interrupciones-operativas` | Especificada |
| RN-032 | Debe existir como máximo una interrupción activa por ingenio en el MVP. | `interrupciones-operativas` | Especificada |
| RN-033 | Los turnos ASIGNADO afectados deben reprogramarse automáticamente respetando disponibilidad y prioridad. | `interrupciones-operativas` | Especificada |
| RN-034 | Los turnos EN_CAMINO y EN_ESPERA no se reprograman automáticamente; deben ser identificados para notificación. | `interrupciones-operativas` | Especificada |
| RN-035 | Los turnos INGRESADO, EN_DESCARGA y FINALIZADO no se modifican por una interrupción de recepción. | `interrupciones-operativas` | Especificada |
| RN-036 | Durante una interrupción se aceptan nuevas solicitudes, pero solo se asignan ventanas posteriores al período estimado de indisponibilidad. | `interrupciones-operativas` | Especificada |
| RN-037 | El backend determina quién debe ser notificado y qué cambió; n8n se limita a efectuar la comunicación mediante WhatsApp. | `integracion-whatsapp-n8n` | Especificada |

## CU-008 - Monitorear Operación del Ingenio

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-038 | El dashboard solo muestra información del ingenio asociado al usuario autenticado. | `monitoreo-operativo` | Especificada |
| RN-039 | Los indicadores se calculan a partir del estado actual almacenado por AgroFlow. | `monitoreo-operativo` | Especificada |
| RN-040 | Por defecto los indicadores corresponden al día actual. | `monitoreo-operativo` | Especificada |
| RN-041 | Si existe una interrupción activa, el dashboard debe indicarla. | `monitoreo-operativo` | Especificada |
| RN-042 | Consultar el dashboard no modifica datos. | `monitoreo-operativo` | Especificada |
| RN-042A | En el MVP no se exige actualización mediante WebSockets; el frontend puede refrescar periódicamente la información. | `monitoreo-operativo` | Especificada |

## CU-009 - Gestionar Transportistas

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-043 | El DNI del transportista debe ser único. | `datos-maestros` | Especificada |
| RN-044 | El número de WhatsApp utilizado para identificar al transportista debe ser único. | `datos-maestros` | Especificada |
| RN-045 | Un transportista inactivo no puede solicitar nuevos turnos. | `datos-maestros` | Especificada |
| RN-046 | Para conservar el historial, los transportistas deben inhabilitarse en lugar de eliminarse físicamente cuando ya poseen información operativa asociada. | `datos-maestros` | Especificada |

## CU-010 - Gestionar Camiones

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-047 | La patente de un camión debe ser única. | `datos-maestros` | Especificada |
| RN-048 | Un camión inactivo no puede utilizarse para solicitar nuevos turnos. | `datos-maestros` | Especificada |
| RN-049 | El tipo de flota debe pertenecer a los valores admitidos por AgroFlow. | `datos-maestros` | Especificada |
| RN-049A | Un camión con historial operativo debe inhabilitarse en lugar de eliminarse físicamente. | `datos-maestros` | Especificada |

## CU-011 - Cancelar Turno

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-050 | Solo pueden cancelarse turnos en ASIGNADO, EN_CAMINO o EN_ESPERA. | `turnos-ciclo-de-vida` | Especificada |
| RN-051 | INGRESADO, EN_DESCARGA, FINALIZADO y CANCELADO no admiten cancelación. | `turnos-ciclo-de-vida` | Especificada |
| RN-052 | La cancelación debe liberar la capacidad utilizada por el turno. | `turnos-ciclo-de-vida` | Especificada |
| RN-052A | CANCELADO es un estado terminal y el turno no se elimina físicamente. | `turnos-ciclo-de-vida` | Especificada |

## CU-012 - Gestionar Fincas

| ID | Regla original | Capability OpenSpec destino | Estado |
|---|---|---|---|
| RN-053 | Una finca inactiva no puede utilizarse en nuevas solicitudes de turno. | `datos-maestros` | Especificada |
| RN-054 | Los turnos históricos conservan su referencia a la finca aunque posteriormente sea inhabilitada. | `datos-maestros` | Especificada |
| RN-054A | Para el MVP se administra un catálogo de fincas; la incorporación de coordenadas, polígonos y PostGIS queda fuera del alcance inicial. | `datos-maestros` | Especificada |

## Decisiones abiertas relacionadas

- La fórmula o precedencia que combina RN-010 y RN-011 todavía debe aprobarse.
- RN-016 fija el comportamiento de respaldo cuando una ventana solicitada no tiene disponibilidad, pero sigue pendiente decidir si el transportista elige una franja preferida o si AgroFlow asigna siempre la próxima ventana.
- La cardinalidad transportista-camión permanece pendiente. RN-017 conserva su estado **Especificada** porque la restricción de autorización es inequívoca; la cardinalidad y el diseño de la asociación son una decisión de modelo separada.
- RN-033 está especificada como obligación de reprogramar, aunque su resultado concreto depende de la política de prioridad aún pendiente.
