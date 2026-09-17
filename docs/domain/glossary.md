# Glosario del dominio

Este glosario define el lenguaje compartido. Las reglas de comportamiento correspondientes se encuentran en `openspec/specs/`.

## Términos principales

### Ingenio

Establecimiento industrial que recibe y procesa la caña. Es el límite principal de aislamiento de datos y operación.

### Zafra

Período anual de cosecha y procesamiento de la caña de azúcar. Puede utilizarse como período de análisis y planificación, pero no sustituye la fecha operativa de cada turno.

### Transportista

Persona o entidad habilitada para operar uno o más camiones. La cardinalidad definitiva entre transportista y camión está pendiente de decisión; el sistema utilizará una asociación explícita para no fijarla accidentalmente.

### Camión

Vehículo identificado por una patente normalizada y clasificado por tipo de flota. Puede quedar inactivo, pero no se elimina cuando tiene historial.

### Finca

Origen o procedencia de la carga. En el MVP contiene una referencia descriptiva de ubicación; coordenadas, polígonos y PostGIS quedan fuera de alcance.

### Turno

Reserva operativa para que un camión sea recibido por un ingenio en una ventana determinada. Incluye actor solicitante, camión, finca, prioridad, estado, fechas y trazabilidad.

### Turno activo

Turno que todavía puede influir en la operación o consumir capacidad. La definición exacta se deriva de los estados no terminales especificados.

### Ventana horaria

Intervalo de recepción con capacidad limitada. Su duración, capacidad y forma de selección deben definirse antes de implementar el motor de asignación.

### Prioridad

Orden relativo utilizado para asignar capacidad. Debe considerar tiempo desde el corte y tipo de flota; la fórmula, pesos y desempates siguen pendientes de aprobación.

### Interrupción operativa

Evento que reduce o suspende temporalmente la capacidad de un ingenio y puede requerir reprogramar o notificar turnos.

### Fecha operativa

Fecha bajo la cual el ingenio agrupa su jornada. Debe interpretarse con la zona horaria configurada para el ingenio.

### n8n

Adaptador de integración que normaliza mensajes de WhatsApp y comunica respuestas. No es fuente de verdad ni decide reglas de negocio.

## Estados del turno

```text
ASIGNADO -> EN_CAMINO -> EN_ESPERA -> INGRESADO -> EN_DESCARGA -> FINALIZADO
     |           |            |
     `-----------+------------+-> CANCELADO
```

- `ASIGNADO`: reserva confirmada y persistida.
- `EN_CAMINO`: el transportista informó que inició el viaje.
- `EN_ESPERA`: el camión llegó y espera autorización de ingreso.
- `INGRESADO`: el camión ingresó al proceso interno.
- `EN_DESCARGA`: la descarga está en curso.
- `FINALIZADO`: operación terminada; estado terminal.
- `CANCELADO`: turno cancelado sin borrado físico; estado terminal.

Las transiciones permitidas y la autoridad de cada actor están definidas normativamente en la especificación de ciclo de vida de turnos.
