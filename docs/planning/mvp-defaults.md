# Defaults y propuestas para el MVP

> **Estado: reconciliado con `main`, ADR-004 y B00.** Este documento registra los límites de las decisiones aprobadas y conserva ideas para una revisión futura; no es un baseline normativo. Solo tienen autoridad las porciones aprobadas explícitamente que se indican abajo. Una conversación o propuesta aceptada no se vuelve normativa por aparecer aquí ni por archivarse un cambio documental. Cada opción abierta requiere aprobación explícita y un cambio OpenSpec de la capacidad propietaria antes de implementarse.

## Propósito y autoridad

Esta referencia registra los límites de las decisiones del MVP. El comportamiento normativo corresponde a las especificaciones de capacidades en [`openspec/specs/`](../../openspec/specs/), de acuerdo con [ADR-002](../architecture/decisions/ADR-002-jerarquia-documental-y-openspec.md). El backend sigue siendo la fuente operativa de verdad, según [ADR-003](../architecture/decisions/ADR-003-backend-fuente-de-verdad.md).

El prototipo de [`docs/contexto/AgroFlow-Dashboard/README.md`](../contexto/AgroFlow-Dashboard/README.md) es solo una referencia visual. No define comportamiento, campos, contratos ni datos operativos.

## Resumen del estado de las decisiones

| ID | Porción aprobada en `main`, ADR-004 o B00 | Aspectos aún abiertos |
|---|---|---|
| OD-001 | Prioridad por mayor tiempo desde el corte de caña; la flota propia desempata; luego, la solicitud anterior | Formato y validación del corte; detalles de concurrencia |
| OD-002 | Como máximo un transportista autorizado activo por camión; se conservan asociaciones históricas | Gestión de asociaciones y permisos |
| OD-003 | No se elige una ventana preferida; el backend asigna la próxima ventana compatible | Detalles dependientes de las reglas abiertas de calendario y capacidad |
| OD-004 | Duración y capacidad configurables por ingenio mediante datos iniciales; 30 minutos y cupo 2 son solo datos de demostración | Calendario, zona horaria, límites y horizonte de búsqueda |
| OD-005 | Se requieren fecha y hora del corte y carga estimada, además de transportista, camión y finca | Unidades, formato, rangos y validaciones restantes |
| OD-006 | No hay una solución detallada aprobada por esta corrección | Concurrencia, idempotencia, conflictos y reintentos |
| OD-007 | Operador y supervisor pueden gestionar los tres catálogos y crear/cancelar turnos internos dentro de su ingenio | Matriz completa de roles y otros permisos |
| OD-008 | No hay una solución detallada aprobada por esta corrección | Seguridad del canal n8n y política de integración |
| OD-009 | No hay una solución detallada aprobada por esta corrección | Eventos auditables, conservación y acceso |
| OD-010 | No hay una solución detallada aprobada por esta corrección | Entrega de notificaciones, estados y reintentos |
| OD-011 | No hay una solución detallada aprobada por esta corrección | Operación, recuperación, privacidad y calidad de servicio |
| OD-012 | La cola incluye franjas vacías y cupos restantes obtenidos del estado de la API | Fórmulas de indicadores, fecha operativa, zona horaria, horizonte y cadencia de actualización |
| OD-013 | Operador y supervisor pueden listar, crear, editar e inhabilitar transportistas, camiones y fincas | Campos editables y validaciones, gestión de asociaciones y reactivación |

Consultá [`open-decisions.md`](open-decisions.md) para el registro vigente de decisiones abiertas y [ADR-004](../architecture/decisions/ADR-004-politicas-operativas-del-mvp.md) para las políticas aprobadas y sus detalles explícitamente pendientes.

## Decisiones aprobadas, con límites

### OD-001 — Prioridad

Entre solicitudes elegibles que compiten por capacidad, tiene prioridad la de mayor tiempo transcurrido desde el corte de caña; ante un empate, prevalece la flota propia y luego la solicitud registrada antes. No se aprobaron pesos, desempate por minuto, desempate por identificador ni una explicación obligatoria. El backend aplica la prioridad en la capacidad propietaria `turnos-solicitud-y-asignacion`.

### OD-002 — Cardinalidad transportista/camión

Un camión puede tener como máximo un transportista autorizado activo a la vez; sus asociaciones corresponden al ingenio del camión y se conserva el historial de asociaciones inactivas. Los detalles y permisos para gestionar asociaciones siguen abiertos.

### OD-003 — Selección de ventana

En el MVP, quien solicita no elige una ventana preferida. El backend selecciona la próxima ventana compatible. La idea histórica de capturar una preferencia no forma parte de la solicitud ni del contrato del MVP.

### OD-004 — Duración y capacidad

La duración y la capacidad se configuran por ingenio mediante datos iniciales o de carga semilla, no desde la interfaz de configuración del MVP. La ventana de 30 minutos y el cupo 2 son únicamente valores de datos de demostración, no reglas fijas de negocio. Siguen abiertos el calendario, la zona horaria, los límites y el horizonte de búsqueda; este documento no los determina.

### OD-005 — Datos mínimos de solicitud

La solicitud requiere fecha y hora del corte de caña y carga estimada, además de transportista, camión y finca. No se solicita una ventana preferida. Las unidades de carga, los formatos, los rangos y las demás validaciones siguen abiertos; no deben inferirse.

### OD-007 — Roles y permisos parciales

Según ADR-004 y `main`, operador y supervisor pueden listar, crear, editar e inhabilitar los tres catálogos, y crear/cancelar turnos internos dentro de su ingenio. Esto no aprueba una matriz completa de roles ni permisos para gestionar asociaciones, configurar capacidad, reintentar notificaciones u otras operaciones.

### OD-012 — Acuerdo parcial sobre la cola

La cola del MVP incluye las franjas vacías y los cupos restantes, obtenidos del estado de la API para el ingenio autorizado. La aprobación proviene de la respuesta del backend para esos elementos; no cierra el resto de OD-012. Siguen abiertos las fórmulas de los indicadores, la fecha operativa, la zona horaria, el horizonte y la cadencia de actualización. No se aprobó un intervalo de consulta periódica.

### OD-013 — Acuerdo parcial sobre catálogos

Está aprobado que operador y supervisor puedan listar, crear, editar e inhabilitar transportistas, camiones y fincas. Siguen abiertos los campos editables, la validación de esos campos, la gestión de asociaciones y la reactivación. No se debe inferir más operaciones CRUD ni permisos adicionales.

## Propuestas futuras no normativas conservadas para revisión

Las propuestas de OD-006 y OD-008 a OD-011 que se describen en esta sección no fueron aprobadas por `main` ni por B00. Son ideas que pueden revisarse o rechazarse; no autorizan implementación. Lo mismo aplica a los detalles propuestos de OD-012 y OD-013 que no figuran en sus aprobaciones parciales.

### OD-006 — Propuesta de concurrencia e idempotencia

**Estado:** propuesta no aprobada por la rama principal vigente ni por B00; no autoriza implementación.

Una propuesta anterior consideró claves de idempotencia estables para operaciones de creación y modificación, identificadores de correlación, persistencia de solicitudes y resultados, reproducción de resultados ante entradas idénticas, rechazo de reutilización de una clave con entradas distintas, reserva transaccional de capacidad, restricciones de unicidad y concurrencia optimista. Esta corrección no aprueba ninguna parte de ese diseño o comportamiento detallado.

### OD-008 — Propuesta de seguridad del canal n8n

**Estado:** propuesta no aprobada por la rama principal vigente ni por B00; no autoriza implementación.

Una propuesta anterior consideró validar la autenticidad del webhook, usar una credencial de servicio separada y acotada, validar al remitente e incorporar identificadores de mensaje, correlación y deduplicación persistida. Esto no constituye un contrato aprobado de seguridad del canal.

### OD-009 — Propuesta de auditoría

**Estado:** propuesta no aprobada por la rama principal vigente ni por B00; no autoriza implementación.

Una propuesta anterior consideró registros de auditoría append-only con actor o servicio, ingenio, instante, acción, resultado, correlación, referencias limitadas al antes y después, conservación y acceso de solo lectura. La cobertura de eventos, la conservación y las reglas de acceso siguen sin decidirse.

### OD-010 — Propuesta de notificaciones

**Estado:** propuesta no aprobada por la rama principal vigente ni por B00; no autoriza implementación.

Una propuesta anterior consideró un outbox durable, estados de entrega, deduplicación, un adaptador reproducible del proveedor, límites y esperas progresivas para reintentos y reintento manual. No están aprobados una cantidad de reintentos, permisos por rol —incluida cualquier asignación exclusiva a supervisor o administrador—, una garantía de entrega ni el comportamiento de un proveedor.

### OD-011 — Propuesta de operación

**Estado:** propuesta no aprobada por la rama principal vigente ni por B00; no autoriza implementación.

Una propuesta anterior consideró un entorno académico reproducible, comprobaciones de salud, logs estructurados, respaldo y restauración, y métricas de calidad de servicio. Las decisiones de persistencia, la evidencia requerida, las reglas de recuperación y los objetivos de servicio siguen sin decidirse.

### OD-012 — Propuestas de detalle para el dashboard

**Estado:** los detalles de esta propuesta no están aprobados por la rama principal vigente ni por B00; no autorizan implementación.

Propuestas anteriores describieron conteos diarios por estado, una fecha operativa local, una zona horaria, un instante de cálculo y consultas periódicas. Estos detalles no están aprobados. En particular, no se aprobó una zona horaria ni un intervalo de actualización de 30 segundos. La aprobación parcial se limita a incluir franjas vacías y cupos restantes provenientes de la API.

### OD-013 — Propuestas de detalle para catálogos

**Estado:** los detalles de esta propuesta no están aprobados por la rama principal vigente ni por B00; no autorizan implementación.

Propuestas anteriores describieron conjuntos de campos, validaciones, flujos de asociación, historial y reactivación. Solo están aprobadas parcialmente las operaciones de listar, crear, editar e inhabilitar para operador y supervisor sobre transportistas, camiones y fincas. La elección de campos, la gestión de asociaciones y la reactivación requieren decisiones explícitas.

## Los defaults de API de B00 siguen abiertos

Las rutas, los DTO y las convenciones de paginación de B00 siguen abiertos. Este documento no selecciona rutas de endpoints, estructuras de DTO, convenciones de paginación, encabezados ni códigos de estado. Las opciones de API deben decidirse explícitamente y quedar representadas en el contrato correspondiente y en el cambio OpenSpec de la capacidad propietaria.

## Cómo convertir una propuesta en comportamiento

1. Identificar la capacidad afectada y resolver explícitamente la decisión abierta.
2. Registrar en un ADR las decisiones arquitectónicas cuando corresponda y actualizar B00 si es responsable de esa decisión.
3. Crear o actualizar la propuesta en `openspec/changes/` de la capacidad propietaria y obtener su revisión y aprobación.
4. Consolidar el comportamiento aprobado en `openspec/specs/` según el proceso del proyecto.
5. Actualizar el contrato de API, las pruebas y la documentación junto con el cambio de comportamiento.
6. Implementar solo después de la aprobación; los detalles sin resolver no son defaults de implementación.
