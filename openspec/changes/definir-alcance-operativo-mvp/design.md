# Diseño de las decisiones operativas del MVP

## Contexto

La misma solicitud puede originarse en la web interna o en WhatsApp/n8n. El backend sigue siendo la única fuente de estado y de decisiones de negocio según ADR-003. La configuración por ingenio se carga al preparar el ambiente; el prototipo visual no constituye contrato ni introduce por sí mismo permisos o reglas.

## Decisiones

### Orden de prioridad

La comparación es lexicográfica: mayor tiempo desde el corte; si empata, flota propia antes que terceros; si persiste el empate, fecha/hora de recepción de solicitud más antigua registrada de forma estable por el backend. No se definen pesos numéricos ni una prioridad independiente por canal. El mismo criterio aplica en asignación y reprogramación. La resolución de una igualdad exacta incluso en esa marca temporal queda por definir antes de implementar si la precisión elegida permite empates.

### Ventanas y capacidad

El solicitante no elige una franja. El backend busca la primera ventana futura compatible con calendario, cupo, interrupciones y prioridad. Duración y cupo pertenecen a la configuración inicial de cada ingenio; el MVP no ofrece edición operativa desde la interfaz. El seed de demostración utiliza 30 minutos y dos camiones por ventana sólo para ese ingenio. Continúan abiertos calendario de recepción, zona horaria, límites horarios y comportamiento ante agotamiento del horizonte de búsqueda.

### Identidad y datos

Cada camión tiene como máximo una asociación activa con un transportista. La asociación es explícita; el historial no se borra al cambiarla. La solicitud contiene transportista, camión, finca, momento de corte y carga estimada. No se fijan aquí unidades, formatos ni campos adicionales opcionales.

### Permisos

Operador y supervisor pueden listar, dar de alta, editar e inhabilitar transportistas, camiones y fincas dentro de su ámbito; pueden crear y cancelar turnos del propio ingenio. La API verifica rol y pertenencia, no depende de la navegación del frontend. Los permisos de otros roles siguen abiertos.

### Cola visual

El backend entrega las ventanas, también las vacías, con cupo, ocupación y disponibilidad vigentes. El frontend sólo las presenta. La actualización puede hacerse con consultas periódicas, sin imponer WebSockets.

## Alternativas descartadas para el MVP

- Preferencia de franja elegida por el transportista.
- Relación activa de muchos transportistas con el mismo camión.
- Cupo/duración fijos para todos los ingenios o editables desde la pantalla de Configuración.
- Cálculo autoritativo de cupos y prioridad en frontend o n8n.

## Riesgos y asuntos pendientes

- OD-004: calendario, zona horaria y límites de ventanas siguen pendientes; no se debe inferir una jornada por defecto.
- OD-005: formatos y validaciones precisas del corte y la carga estimada siguen pendientes.
- OD-006: transacciones concurrentes e idempotencia deben impedir sobreasignación y duplicados.
- OD-007 y OD-013: permisos restantes y campos editables concretos deben cerrarse antes de codificar sus operaciones.
- OD-012: fórmulas de indicadores y frecuencia de refresco siguen pendientes; la cola de cupos no las resuelve.

## Migración y verificación

El código heredado de comercio electrónico no define reglas para AgroFlow. Antes de implementar endpoints se actualizará OpenAPI con las formas exactas y se prepararán migraciones/datos de arranque sin secretos ni personas reales. Las pruebas deberán cubrir orden de prioridad, ausencia de franja preferida, asociación única, cupos por ingenio, permisos y franjas vacías, además de concurrencia y recorridos integrados cuando se cierren sus decisiones técnicas.
