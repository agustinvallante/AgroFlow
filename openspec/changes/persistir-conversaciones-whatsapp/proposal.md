# Persistir conversaciones de WhatsApp e identificar transportistas

> **Estado: propuesta en revisión (Fase 0).** Este cambio formaliza la [propuesta de persistencia de conversaciones](../../../docs/planning/propuesta-persistencia-conversaciones-whatsapp.md) para su revisión por el equipo. No aprueba decisiones abiertas, no modifica el [contrato OpenAPI](../../../docs/contracts/openapi.yaml) ni autoriza implementar mientras las decisiones bloqueantes listadas en la sección 7 del [diseño](design.md) no estén aprobadas.

## Objetivo

1. Permitir que el ingenio registre y consulte transportistas con su número de WhatsApp, de modo que el canal conversacional los identifique automáticamente sin editar el seed ni versionar números reales.
2. Persistir en el backend de AgroFlow las conversaciones y los mensajes de WhatsApp, entrantes y salientes, para que la API los recupere y el dashboard los muestre.
3. Evitar que un mismo mensaje entrante produzca filas repetidas, procesamientos concurrentes o respuestas duplicadas.

## Motivación

- La tabla de transportistas existe y la API de turnos ya resuelve al transportista por su número, pero la única forma de cargar un número es el seed ficticio. El servicio de datos maestros de PR #47 está escrito y deshabilitado.
- El workflow de n8n responde al webhook antes de procesar, no deja registro autoritativo de mensajes y su memoria de ventana no es un historial.
- El dashboard muestra conversaciones simuladas aun en modo HTTP.
- La [sincronización del backlog](../../../docs/planning/mvp-backlog-sync-2026-10-15.md) exige un cambio aprobado propio para incorporar historial conversacional al MVP.

## Alcance

- Habilitar el registro, la edición, la inhabilitación y la consulta de transportistas reutilizando la fundación de datos maestros existente.
- Identificar al remitente de un mensaje de WhatsApp a partir de su número, dentro del ingenio al que pertenece el canal.
- Registrar conversaciones, mensajes entrantes de texto y respuestas salientes con su estado de envío.
- Garantizar idempotencia en la ingesta y una única autorización de procesamiento por mensaje entrante.
- Exponer operaciones para n8n y consultas paginadas para el dashboard.
- Autenticar a n8n como servicio y aislar los datos por ingenio.
- Definir migraciones EF Core sobre el SQLite académico y la estrategia de pruebas.

## Fuera de alcance

- Adjuntos, audios, ubicaciones, mensajes de grupo, reacciones y estados de lectura.
- Callbacks de entrega o lectura del proveedor.
- Notificaciones salientes no originadas en una respuesta, como avisos de interrupción.
- Resumen o selección de contexto para el modelo de lenguaje a partir del historial.
- Reprocesamiento automático de mensajes cuya autorización venció.
- Cambiar el contrato de `/api/v1/appointments` o exigir credenciales a las herramientas actuales del agente.
- Retención, borrado y exportación definitivas de datos personales (OD-009, OD-011).
- PostgreSQL, colas o infraestructura adicional.

## Capacidades afectadas

| Capacidad | Tipo | Delta |
|---|---|---|
| `conversaciones-whatsapp` | Nueva | [specs/conversaciones-whatsapp/spec.md](specs/conversaciones-whatsapp/spec.md) |
| `integracion-whatsapp-n8n` | Existente | [specs/integracion-whatsapp-n8n/spec.md](specs/integracion-whatsapp-n8n/spec.md) |
| `datos-maestros` | Existente | [specs/datos-maestros/spec.md](specs/datos-maestros/spec.md) |

`acceso-y-autorizacion`, `plataforma-y-segregacion` y `monitoreo-operativo` se aplican sin cambios: aislamiento por ingenio, errores sin información sensible y confirmación posterior a la persistencia.

## Casos de uso relacionados

- **CU-009** Gestionar transportistas: alta, edición, inhabilitación y consulta con WhatsApp.
- **CU-002, CU-003, CU-006**: solicitud, consulta y aviso `EN_CAMINO` por el canal. El registro de mensajes precede a esas operaciones, pero no las sustituye.
- **CU-008** Monitorear operación: nueva vista de conversaciones del ingenio.

## Impacto

| Área | Impacto |
|---|---|
| Backend | Nuevas entidades, servicio y controladores de conversaciones; habilitación del controlador de transportistas; autenticación de servicio; contexto de ingenio para el perfil de demo. |
| Persistencia | Migraciones en `AgroFlowDbContext` (SQLite): tablas de auditoría e idempotencia ya modeladas, índice global de WhatsApp, canales, conversaciones, mensajes y procesamiento. |
| Contrato API | Nuevas rutas de transportistas, ingesta, intenciones de respuesta y consultas. Se proponen en el diseño y se publican en OpenAPI en cada etapa aprobada. |
| n8n | Nodos de validación, registro, enrutamiento y actualización de estado. Requiere actualizar el validador de integraciones de CI. Etapa posterior. |
| Frontend | Adaptadores HTTP de conversaciones y transportistas. Etapa posterior. |
| Pruebas | Dominio, persistencia SQLite migrada, HTTP, aislamiento entre dos ingenios, concurrencia de duplicados, frontend y fixtures sintéticos del workflow. |
| CI | `validate-integrations.mjs` debe admitir las rutas nuevas; las pruebas que hoy afirman la fundación deshabilitada se actualizan deliberadamente. |

## Trazabilidad

Epic [#46](https://github.com/agustinvallante/AgroFlow/issues/46), con [#7](https://github.com/agustinvallante/AgroFlow/issues/7) (B00) y [#42](https://github.com/agustinvallante/AgroFlow/issues/42) como referencias. Depende de las decisiones OD-006, OD-008, OD-009, OD-010, OD-011 y OD-013 del [registro de decisiones](../../../docs/planning/open-decisions.md).
