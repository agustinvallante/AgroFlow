# Hoja de ruta de implementación del MVP

La [demo local](local-demo-scope.md) dejó una base integrada, pero su perfil sin autenticación, con un solo ingenio y vistas simuladas no alcanza la [aceptación del MVP](../product/mvp-acceptance.md). La [replanificación tras la demo](mvp-replan-2026-09-29.md) registra evidencias y brechas por puerta `G01`–`G12`. Los estados, responsables y prioridades vigentes se consultan en [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2) y [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1); las cifras y estados de esos tableros no se congelan en esta página.

Los identificadores `Bxx` y `Fxx` son prefijos de issues, no versiones de API. OpenSpec define el comportamiento y [OpenAPI](../contracts/openapi.yaml) las operaciones aprobadas. Las rutas históricas en títulos de issues son propuestas: para turnos ya existe el contrato de demo `/api/v1/appointments`, que debe ampliarse para el MVP sin duplicar endpoints por inercia.

El [corte de backlog del 2026-10-15](mvp-backlog-sync-2026-10-15.md) distingue lo integrado de los aportes pendientes #47/#79/#80. B01 #8 ya está completada. La propuesta de historial #88 no se transforma en alcance obligatorio ni API disponible. Las issues conservan sus estados y necesitan el refinamiento del contrato antes de considerarse Ready.

## Secuencia por incrementos verificables

| Incremento | Backend | Frontend/integración | Condición para avanzar |
|---|---|---|---|
| 0. Acordar el primer corte | `B00` y decisiones `OD-*`; conservar B01 ya integrada | Revisar contrato con responsables de `B02/B03`, `F01`, `F06` y n8n | Contrato y decisiones del slice coherentes, sin esperar todas las decisiones del MVP; plan de SQLite MVP y pruebas de diario/recuperación de `B02` acordado. |
| 1. Base segregada | `B02`–`B05`, `B10` | `F01` acceso/sesión | Identidad, contexto de ingenio, configuración reproducible, rechazo cruzado y pruebas. La API de demo no se expone sin seguridad como perfil MVP. |
| 2. Turno vertical real | `B25`–`B29` como extensiones del servicio actual | `F06` adapta el recorrido HTTP existente | Alta, consulta, transición y cancelación desde UI con estado persistido, prioridad/capacidad/idempotencia y errores previstos. |
| 3. Maestros y asociaciones | `B11`–`B24`, `B38` | `F02`–`F05` | Listado, alta, edición e inhabilitación por rol/ingenio, asociación administrable e historial conservado. |
| 4. Operación visible | `B35`, `B39`, `B33`–`B34` | `F07` dashboard; `F08` interrupciones | Métricas y ventanas incluso vacías desde API; interrupciones y tratamiento por estado visibles sin cálculos de negocio en UI. |
| 5. Canal y entrega | `B30`–`B32`, `B36`–`B37`, `B07` | Reutilizar `chatbot/workflows/Chatbot.json` tras [#79](https://github.com/agustinvallante/AgroFlow/pull/79); repositorio externo como origen histórico | Canal de prueba importable, autenticado y deduplicado; notificación al destinatario indicado por API. Empaquetado y saludo no son E2E. |
| 6. Aceptación | `B06` | `F09` y QA transversal | Evidencia de `G01`–`G12`, dos recorridos completos fechados, reinicio, respaldo/restauración y sin defectos bloqueantes. |

Se permite trabajar en paralelo dentro de un incremento cuando el contrato de la porción está acordado. La secuencia no obliga a terminar todos los catálogos antes de probar un turno; sí exige datos de arranque válidos y no confundir ese atajo de integración con la aceptación posterior de `G03`.

Separar coordinación de prerrequisito: B28 provee estados a B32; B29 libera cupo que B39 consulta; B25–B29 proveen la API a F06. No hacer que sus consumidores sean a su vez bloqueos de inicio. La fundación de #47 se revisa/reutiliza sin asumir CRUD publicado ni contratos de idempotencia/sesión aprobados; #80 prepara controles y no implementa seguridad/E2E del canal.

## Políticas y decisiones que no debe inventar un equipo

La [ADR-004](../architecture/decisions/ADR-004-politicas-operativas-del-mvp.md) ya fija prioridad por mayor tiempo desde el corte, flota propia y antigüedad de solicitud; un transportista activo por camión; próxima ventana automática; duración/cupo configurados por ingenio y seed demo de 30 minutos/cupo 2; y CRUD con inhabilitación de los tres catálogos. Siguen abiertos los detalles de calendario/zona/horizonte, validaciones, idempotencia, permisos restantes, seguridad n8n, auditoría, notificaciones e indicadores en [OD-004 a OD-013](open-decisions.md). `OD-014` y la [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) eligen **SQLite para el MVP**, pero su perfil seguro/segregado, modo de diario, migraciones y respaldo aún deben implementarse y probarse. WAL es condicional, no una propiedad ya verificada del sistema.

La [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md) mantiene React/Vite. El backend es la única fuente operativa de reglas; frontend y n8n no calculan ventanas, prioridad, cupos, permisos o transiciones.

Completar una lista de issues backend no equivale a entregar el MVP. Para cada comportamiento nuevo o modificado se sigue el flujo de [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md): cambio OpenSpec, contrato, implementación, pruebas y documentación coherentes. El candidato final cumple todas las puertas obligatorias de la [guía](../product/mvp-acceptance.md).

## Después de la presentación académica

El MVP se presenta con SQLite y datos ficticios o simulados. La [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) establece PostgreSQL como destino para el primer cliente real: migrar y validar antes de comenzar su operación. Esta etapa no agrega PostgreSQL a las issues del MVP ni exige mantener ambos motores. Su plan incluirá proveedor/migraciones de AgroFlow e Identity, UTC y zona del ingenio, restricciones/errores, pruebas reales de concurrencia y recorridos integrados, configuración y respaldo/restauración.

La reserva orientativa es de 2–3 semanas calendario, basada en 5–10 días hábiles de backend dedicado con apoyo de QA, si sólo hay datos simulados recreables. No es un plazo garantizado: debe recalcularse con el MVP final y el entorno del cliente. Si ya hay datos operativos que conservar, hace falta un plan específico de transferencia, corte y reversión, no sólo un cambio de proveedor. Los supuestos y el rango alternativo se mantienen en ADR-006, sin congelarlos en el backlog actual.
