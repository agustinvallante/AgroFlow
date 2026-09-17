# Catálogo de casos de uso del MVP

Este catálogo conserva la trazabilidad del documento académico. Los detalles normativos vigentes están en OpenSpec.

| ID | Caso de uso | Actores principales | Capacidad OpenSpec |
|---|---|---|---|
| CU-001 | Autenticación interna | Operador, gerente o supervisor | `acceso-y-autorizacion` |
| CU-002 | Solicitar y asignar turno | Transportista, n8n | `turnos-solicitud-y-asignacion` |
| CU-003 | Consultar turno | Transportista, n8n | `turnos-consulta` |
| CU-004 | Consultar turnos del ingenio | Usuario interno | `turnos-consulta` |
| CU-005 | Consultar detalle de turno | Usuario interno | `turnos-consulta` |
| CU-006 | Actualizar estado de turno | Transportista u operador autorizado | `turnos-ciclo-de-vida` |
| CU-007 | Registrar interrupción operativa | Operador autorizado | `interrupciones-operativas` |
| CU-008 | Monitorear operación | Operador, gerente o supervisor | `monitoreo-operativo` |
| CU-009 | Gestionar transportistas | Usuario con permiso de gestión | `datos-maestros` |
| CU-010 | Gestionar camiones | Usuario con permiso de gestión | `datos-maestros` |
| CU-011 | Cancelar turno | Transportista u operador autorizado | `turnos-ciclo-de-vida` |
| CU-012 | Gestionar fincas | Usuario con permiso de gestión | `datos-maestros` |

## Reglas transversales

- `integracion-whatsapp-n8n` cubre la comunicación de CU-002, CU-003, CU-006, CU-007 y CU-011.
- `plataforma-y-segregacion` cubre aislamiento por ingenio, persistencia previa a confirmación, errores y trazabilidad.
- La prioridad, capacidad y selección de ventanas afectan CU-002, CU-007 y CU-011, pero conservan decisiones abiertas documentadas.

## Uso por los equipos

Cada issue debe incluir al menos un caso de uso y una capacidad. Una funcionalidad transversal puede tener una épica común y subtareas separadas para frontend y backend, sin duplicar la especificación.
