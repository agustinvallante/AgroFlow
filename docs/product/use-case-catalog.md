# Catálogo de casos de uso del MVP

Este catálogo conserva la trazabilidad de los identificadores del documento académico. Los actores indicados reflejan el alcance vigente del MVP, no una transcripción literal del PDF; los detalles normativos están en OpenSpec.

| ID | Caso de uso | Actores principales | Capacidad OpenSpec |
|---|---|---|---|
| CU-001 | Autenticación interna | Operador, gerente o supervisor | `acceso-y-autorizacion` |
| CU-002 | Solicitar y asignar turno | Transportista mediante n8n; operador o supervisor desde la interfaz interna | `turnos-solicitud-y-asignacion` |
| CU-003 | Consultar turno | Transportista, n8n | `turnos-consulta` |
| CU-004 | Consultar turnos del ingenio | Usuario interno | `turnos-consulta` |
| CU-005 | Consultar detalle de turno | Usuario interno | `turnos-consulta` |
| CU-006 | Actualizar estado de turno | Transportista u operador autorizado | `turnos-ciclo-de-vida` |
| CU-007 | Registrar interrupción operativa | Operador autorizado | `interrupciones-operativas` |
| CU-008 | Monitorear operación | Operador, gerente o supervisor | `monitoreo-operativo` |
| CU-009 | Gestionar transportistas | Operador o supervisor del ingenio | `datos-maestros` |
| CU-010 | Gestionar camiones | Operador o supervisor del ingenio | `datos-maestros` |
| CU-011 | Cancelar turno | Operador o supervisor del ingenio desde la interfaz interna | `turnos-ciclo-de-vida` |
| CU-012 | Gestionar fincas | Operador o supervisor del ingenio | `datos-maestros` |

## Reglas transversales

- `integracion-whatsapp-n8n` cubre la comunicación de CU-002, CU-003, el aviso `EN_CAMINO` de CU-006 y las notificaciones de CU-007. La cancelación por WhatsApp no es obligatoria para el MVP.
- `plataforma-y-segregacion` cubre aislamiento por ingenio, persistencia previa a confirmación, errores y trazabilidad.
- La prioridad, capacidad y selección de ventanas afectan CU-002, CU-007 y CU-011. Su política principal está decidida; los detalles de calendario, zona horaria, concurrencia e idempotencia que faltan siguen en [decisiones abiertas](../planning/open-decisions.md).

## Uso por los equipos

Cada issue debe incluir al menos un caso de uso y una capacidad. Una funcionalidad transversal puede tener una épica común y subtareas separadas para frontend y backend, sin duplicar la especificación.
