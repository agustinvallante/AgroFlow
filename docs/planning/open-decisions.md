# Registro de decisiones del MVP

Este registro distingue los acuerdos ya tomados de los detalles que todavía impiden cerrar un contrato o escenario verificable. Una respuesta parcial **no** autoriza a completar el resto por suposición. Las decisiones acordadas el 2026-09-25 se fundamentan en [ADR-004](../architecture/decisions/ADR-004-politicas-operativas-del-mvp.md) y deben reflejarse en las capacidades OpenSpec afectadas.

## Decisiones surgidas del relevamiento

| ID | Tema | Acuerdo del MVP | Pendiente concreto | Estado |
|---|---|---|---|---|
| OD-001 | Prioridad | Entre solicitudes elegibles que compiten por capacidad, primero va la de mayor tiempo transcurrido desde el corte de la caña. Ante igualdad, va la flota propia antes que la de terceros; si persiste la igualdad, va la solicitud registrada antes. No se usan pesos. | Definir en el contrato el formato y la validación del momento de corte (`OD-005`) y la concurrencia de solicitudes (`OD-006`). | Política resuelta; dependencias técnicas abiertas |
| OD-002 | Asociación transportista-camión | Un camión puede tener **un solo transportista autorizado activo** a la vez. La asociación es explícita y su cambio no debe reescribir los turnos históricos. | Definir los campos y el mecanismo de gestión de asociaciones dentro de `OD-013`. | Cardinalidad activa resuelta; gestión pendiente |
| OD-003 | Selección de ventana | AgroFlow asigna siempre la **próxima ventana disponible compatible**, calculada por el backend; ni el transportista ni el usuario interno eligen una ventana preferida en el MVP. | Aplicar la capacidad y calendario aprobados en `OD-004` y la protección contra carreras de `OD-006`. | Política resuelta; dependencias técnicas abiertas |

La redacción original de `RN-016` contemplaba una ventana solicitada. Se conserva literalmente en la [matriz histórica](../product/business-rules.md), pero la política vigente para el MVP es la de `OD-003`; no debe agregarse un campo `ventanaPreferida` al contrato por interpretar el PDF de forma aislada.

## Decisiones técnicas y de alcance derivadas

| ID | Tema | Acuerdo del MVP o resultado requerido | Pendiente concreto | Estado |
|---|---|---|---|---|
| OD-004 | Capacidad y ventanas | Duración y cupo configurables **por ingenio mediante datos de arranque**; no se editarán desde la pantalla Configuración en el MVP. El conjunto de demostración usará ventanas de **30 minutos** y **2 camiones** por ventana. Estos valores son datos demo, no constantes de negocio. | Calendario y horario de atención por ingenio, zona horaria, tratamiento de límites, horizonte de búsqueda y ausencia de capacidad. | Parcialmente resuelta |
| OD-005 | Datos mínimos de solicitud | Además del transportista, camión y finca, exigir **fecha y hora de corte** y **carga estimada**. No se solicita ventana preferida. | Formato, unidad de carga, rangos, momento válido de corte, relación con el ingenio y demás validaciones de cada dato. | Parcialmente resuelta |
| OD-006 | Concurrencia e idempotencia | Definir una estrategia para evitar sobreasignación y duplicados; clave idempotente y respuesta de reintentos. | Política completa, sin decisión aprobada aún. | Pendiente |
| OD-007 | Matriz de roles y permisos | **Operador y supervisor del ingenio** pueden listar, dar de alta, editar e inhabilitar transportistas, camiones y fincas. También pueden **crear y cancelar turnos desde la interfaz interna**, siempre dentro de su ingenio y respetando las validaciones del backend. | Permisos restantes de gerente/administración, gestión de asociaciones, lectura de otros recursos y matriz completa de operaciones. | Parcialmente resuelta |
| OD-008 | Seguridad del canal n8n | Definir autenticación entre servicios, validación del remitente, firma, correlación, reintentos y deduplicación. | Política completa, sin decisión aprobada aún. | Pendiente |
| OD-009 | Auditoría y conservación | Definir eventos auditables, actor, motivo, retención y acceso al historial. | Política completa, sin decisión aprobada aún. | Pendiente |
| OD-010 | Entrega de notificaciones | Definir proveedor, plantillas, estados de entrega, reintentos y tratamiento de fallas. | Política completa, sin decisión aprobada aún. | Pendiente |
| OD-011 | Operación y calidad de servicio | Definir observabilidad, copias de seguridad, recuperación, tratamiento de datos personales y cómo se medirá la disponibilidad. | Política mínima para la entrega académica y objetivos productivos posteriores. | Pendiente |
| OD-012 | Indicadores y cola del dashboard | La cola visual del MVP **incluye franjas vacías y cupos restantes**, calculados con datos de la API para el ingenio autorizado. No se exige editar duración, cupo o calendario desde Configuración. | Fórmula de cada indicador, fecha operativa y zona horaria, horizonte y formato de las franjas, e intervalo de actualización periódica. | Parcialmente resuelta |
| OD-013 | Gestión de datos maestros | Transportistas, camiones y fincas tendrán **listado, alta, edición e inhabilitación** en el MVP; no se requiere eliminación física. Se aplican los permisos parciales de `OD-007`. | Campos editables y sus validaciones, gestión de asociaciones transportista-camión y restricciones adicionales por historial o turnos activos. | Parcialmente resuelta |

## Cómo cerrar un detalle pendiente

1. Identificar la capacidad y los recorridos afectados; no implementar el vacío con una suposición.
2. Acordar la opción con el equipo y registrar sus consecuencias en un ADR cuando sea arquitectónica.
3. Crear o actualizar `openspec/changes/` y consolidar el comportamiento aprobado en `openspec/specs/`.
4. Actualizar contrato, pruebas y tareas relacionadas en el mismo pull request.
5. Cambiar el estado de esta tabla solo cuando ya exista una respuesta verificable y enlazada.

Una tarea no se considera terminada si depende de uno de los detalles que siguen pendientes.
