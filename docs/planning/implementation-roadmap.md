# Hoja de ruta de implementación

Esta hoja ordena dependencias y demostraciones verticales; no fija fechas, responsables ni una prioridad automática dentro de cada bloque. La fuente de trabajo vigente es [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2). Al preparar este plan, el tablero contiene **38 issues en Backlog**: ocho de base/integración (`B00`–`B07`) y treinta de endpoints (`B10`–`B39`). [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1) se planificará por separado; no se deben inferir tareas frontend terminadas a partir del backlog backend.

Los identificadores `Bxx` son prefijos de títulos de issues, no versiones de API. Las rutas mencionadas en esos títulos son **propuestas de trabajo** hasta que `B00` y [OpenAPI](../contracts/openapi.yaml) establezcan el contrato aprobado. Un cambio de ruta debe corregirse en el contrato, las issues afectadas y sus consumidores antes de implementar.

## Secuencia del backlog backend

| Bloque lógico | Issues | Resultado verificable y dependencia principal |
|---|---|---|
| 0. Acuerdo y plataforma | `B00`–`B05` | Formalizar decisiones que bloquean recorridos, retirar el dominio e-commerce heredado, preparar modelo/migraciones/datos demo, acceso segregado, errores/pruebas base e infraestructura transaccional, idempotente y auditable. El trabajo mecánico de `B01` puede avanzar mientras se revisa `B00`; el esquema definitivo no debe adelantarse a las decisiones que lo afectan. |
| 1. Acceso interno | `B10` | Inicio de sesión sobre identidad y contexto de ingenio compartidos de `B03`, sin duplicar la política de autenticación en el endpoint. |
| 2. Datos maestros | `B11`–`B24`, `B38` | Listar, crear, editar e inhabilitar transportistas, camiones y fincas; autorizar, revocar y consultar asociaciones transportista–camión. Requiere el modelo de `B02` y los permisos acordados en `B00`/`B03`. |
| 3. Turnos internos | `B25`–`B29` | Listado y detalle, solicitud con asignación, avance de estado y cancelación con capacidad persistida. `B27` depende especialmente de capacidad, prioridad, concurrencia e idempotencia acordadas en `B00`, `B02` y `B05`. |
| 4. Entrada conversacional | `B30`–`B32` | Contratos de solicitud, consulta y aviso `EN_CAMINO` para n8n que reutilizan las mismas reglas backend del recorrido interno. |
| 5. Interrupciones y entrega | `B33`–`B34`, `B36`–`B37`, `B07` | Registrar/cerrar interrupciones, reprogramar según estado, preparar avisos, registrar entregas y ofrecer un flujo n8n/WhatsApp **importable y reproducible**. `B07` integra los endpoints; no sustituye su implementación ni sus pruebas. |
| 6. Lecturas operativas | `B35`, `B39` | Indicadores y franjas del ingenio, **incluidas las vacías y sus cupos restantes**, calculados con datos reales. Pueden desarrollarse en paralelo con el bloque 5 una vez disponibles sus fuentes persistidas y reglas. |
| 7. Verificación backend | `B06` | Arranque reproducible y recorrido API del MVP, con datos ficticios controlados, pruebas y documentación. Es una evidencia backend, no una declaración de aceptación del producto completo. |

La duración y capacidad de ventanas son configurables **por ingenio mediante datos de arranque** en el MVP; no se requiere editarlas desde la pantalla de Configuración del prototipo. El conjunto inicial de demostración acordado usa ventanas de 30 minutos y cupo de dos camiones, sin fijar esos valores como constantes de dominio. El calendario de recepción, la zona horaria y sus límites siguen abiertos en el [registro de decisiones](open-decisions.md) y se cierran mediante OpenSpec antes de implementar la asignación.

## Integración con frontend y aceptación

Al aprobar `B00` y el contrato inicial, el equipo debe descomponer el trabajo frontend por recorridos: acceso, datos maestros, turnos, operación, dashboard y estados de error. Cada porción necesita una fuente real de API, pruebas y documentación; no basta con conectar el prototipo a respuestas simuladas. El [flujo de trabajo del equipo](../development/team-workflow.md) describe cómo coordinar cambios compartidos entre ambos Projects.

Completar las 38 issues backend no equivale por sí mismo a entregar el MVP. El candidato debe satisfacer la [especificación de aceptación](../../openspec/specs/aceptacion-del-mvp/spec.md) y todas las puertas de la [guía de aceptación](../product/mvp-acceptance.md), incluidas interfaz, n8n, persistencia, recuperación y demostración repetible.

Cada comportamiento nuevo o modificado sigue el flujo de [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md): cambio OpenSpec, contrato, implementación, pruebas y documentación coherentes. Una decisión abierta no se completa mediante una suposición local en un endpoint.
