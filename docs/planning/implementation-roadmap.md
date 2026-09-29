# Hoja de ruta de implementación del MVP

La [demo local](local-demo-scope.md) dejó una base integrada, pero su perfil sin autenticación, con un solo ingenio y vistas simuladas no alcanza la [aceptación del MVP](../product/mvp-acceptance.md). La [replanificación tras la demo](mvp-replan-2026-09-29.md) registra evidencias y brechas por puerta `G01`–`G12`. Los estados, responsables y prioridades vigentes se consultan en [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2) y [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1); las cifras y estados de esos tableros no se congelan en esta página.

Los identificadores `Bxx` y `Fxx` son prefijos de issues, no versiones de API. OpenSpec define el comportamiento y [OpenAPI](../contracts/openapi.yaml) las operaciones aprobadas. Las rutas históricas en títulos de issues son propuestas: para turnos ya existe el contrato de demo `/api/v1/appointments`, que debe ampliarse para el MVP sin duplicar endpoints por inercia.

## Secuencia por incrementos verificables

| Incremento | Backend | Frontend/integración | Condición para avanzar |
|---|---|---|---|
| 0. Acordar el primer corte | `B00` y decisiones `OD-*`; `B01` limpieza independiente | Revisar contrato con responsables de `F01`, `F06` y n8n | Cambio OpenSpec, OpenAPI y criterio de aceptación del primer recorrido coherentes; plan de SQLite MVP y pruebas de diario/recuperación de `B02` acordado. |
| 1. Base segregada | `B02`–`B05`, `B10` | `F01` acceso/sesión | Identidad, contexto de ingenio, configuración reproducible, rechazo cruzado y pruebas. La API de demo no se expone sin seguridad como perfil MVP. |
| 2. Turno vertical real | `B25`–`B29` como extensiones del servicio actual | `F06` adapta el recorrido HTTP existente | Alta, consulta, transición y cancelación desde UI con estado persistido, prioridad/capacidad/idempotencia y errores previstos. |
| 3. Maestros y asociaciones | `B11`–`B24`, `B38` | `F02`–`F05` | Listado, alta, edición e inhabilitación por rol/ingenio, asociación administrable e historial conservado. |
| 4. Operación visible | `B35`, `B39`, `B33`–`B34` | `F07` dashboard; `F08` interrupciones | Métricas y ventanas incluso vacías desde API; interrupciones y tratamiento por estado visibles sin cálculos de negocio en UI. |
| 5. Canal y entrega | `B30`–`B32`, `B36`–`B37`, `B07` | Workflow en [agroflow-chatbot](https://github.com/BraianMedrano/agroflow-chatbot) | Canal de prueba importable, autenticado y deduplicado; notificación al destinatario indicado por API. |
| 6. Aceptación | `B06` | `F09` y QA transversal | Evidencia de `G01`–`G12`, dos recorridos completos fechados, reinicio, respaldo/restauración y sin defectos bloqueantes. |

Se permite trabajar en paralelo dentro de un incremento cuando el contrato de la porción está acordado. La secuencia no obliga a terminar todos los catálogos antes de probar un turno; sí exige datos de arranque válidos y no confundir ese atajo de integración con la aceptación posterior de `G03`.

## Políticas y decisiones que no debe inventar un equipo

La [ADR-004](../architecture/decisions/ADR-004-politicas-operativas-del-mvp.md) ya fija prioridad por mayor tiempo desde el corte, flota propia y antigüedad de solicitud; un transportista activo por camión; próxima ventana automática; duración/cupo configurados por ingenio y seed demo de 30 minutos/cupo 2; y CRUD con inhabilitación de los tres catálogos. Siguen abiertos los detalles de calendario/zona/horizonte, validaciones, idempotencia, permisos restantes, seguridad n8n, auditoría, notificaciones e indicadores en [OD-004 a OD-013](open-decisions.md). `OD-014` y la [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) eligen **SQLite para el MVP**, pero su perfil seguro/segregado, modo de diario, migraciones y respaldo aún deben implementarse y probarse. WAL es condicional, no una propiedad ya verificada del sistema.

La [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md) mantiene React/Vite. El backend es la única fuente operativa de reglas; frontend y n8n no calculan ventanas, prioridad, cupos, permisos o transiciones.

Completar una lista de issues backend no equivale a entregar el MVP. Para cada comportamiento nuevo o modificado se sigue el flujo de [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md): cambio OpenSpec, contrato, implementación, pruebas y documentación coherentes. El candidato final cumple todas las puertas obligatorias de la [guía](../product/mvp-acceptance.md).
