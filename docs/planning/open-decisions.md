# Decisiones abiertas

Este registro impide que una ambigüedad se convierta accidentalmente en código. Una decisión permanece **pendiente** hasta que el equipo la aprueba, registra su fundamento en un ADR o cambio OpenSpec y actualiza las capacidades afectadas.

## Decisiones expresamente abiertas en el relevamiento

| ID | Decisión | Alternativas o preguntas | Bloquea | Estado |
|---|---|---|---|---|
| OD-001 | Fórmula de prioridad | Definir precedencia, pesos y desempates entre tiempo desde el corte, flota propia y flota de terceros. | Motor de asignación y reprogramación | Pendiente |
| OD-002 | Cardinalidad transportista-camión | Confirmar si la relación es uno a muchos o muchos a muchos. Se requiere una asociación explícita en cualquier caso. | Modelo de datos y autorizaciones | Pendiente |
| OD-003 | Elección de ventana | Definir si el transportista expresa una preferencia o si AgroFlow asigna siempre la próxima ventana. El fallback de RN-016 ya exige la próxima compatible cuando la solicitada no tiene lugar. | Conversación de WhatsApp, UI y contrato de solicitud | Pendiente |

## Decisiones técnicas derivadas necesarias

Estas preguntas no sustituyen reglas del PDF; son detalles necesarios para poder implementarlas y verificarlas.

| ID | Decisión | Resultado que debe documentarse | Bloquea | Estado |
|---|---|---|---|---|
| OD-004 | Modelo de capacidad | Duración de una ventana, capacidad por ingenio, calendario, zona horaria y tratamiento de límites horarios. | Asignación, cancelación e interrupciones | Pendiente |
| OD-005 | Datos mínimos de una solicitud | Campos obligatorios, formatos, momento de corte y validaciones de finca, camión y carga. | Contrato de alta de turno | Pendiente |
| OD-006 | Concurrencia e idempotencia | Estrategia para evitar sobreasignación y duplicados; clave idempotente y respuesta de reintentos. | API de mutaciones e integración | Pendiente |
| OD-007 | Matriz de roles y permisos | Roles válidos y operaciones permitidas para operador, supervisor, gerente y administración. | Autorización y navegación | Pendiente |
| OD-008 | Seguridad del canal n8n | Autenticación entre servicios, validación del remitente, firma, correlación, reintentos y deduplicación. | Integración con WhatsApp | Pendiente |
| OD-009 | Auditoría y conservación | Eventos auditables, actor, motivo, retención y acceso al historial. | Operaciones críticas y cumplimiento | Pendiente |
| OD-010 | Entrega de notificaciones | Proveedor, plantillas, estados de entrega, reintentos y tratamiento de fallas. | Interrupciones y cancelaciones | Pendiente |
| OD-011 | Operación y calidad de servicio | Medición del objetivo de disponibilidad, observabilidad, copias de seguridad, recuperación y tratamiento de datos personales. | Puesta en producción | Pendiente |
| OD-012 | Indicadores y refresco del dashboard | Fórmulas de cada indicador, fecha operativa e intervalo de actualización periódica. | Dashboard funcional | Pendiente |

## Cómo cerrar una decisión

1. Reunir a las áreas afectadas y documentar alternativas y restricciones.
2. Elegir una opción y registrar sus consecuencias en un ADR cuando sea arquitectónica.
3. Crear o actualizar el cambio correspondiente en `openspec/changes/`.
4. Actualizar contrato, specs, pruebas y tareas relacionadas en el mismo pull request.
5. Cambiar el estado de esta tabla a **Resuelta** y enlazar la decisión resultante.

No se debe marcar como terminada una tarea cuya conducta depende de una decisión pendiente.
