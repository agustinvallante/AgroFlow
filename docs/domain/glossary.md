# Glosario del dominio

Este glosario define el lenguaje compartido. Las reglas de comportamiento correspondientes se encuentran en `openspec/specs/`.

## Términos principales

### Ingenio

Establecimiento industrial que recibe y procesa la caña. Es el límite principal de aislamiento de datos y operación.

### Zafra

Período anual de cosecha y procesamiento de la caña de azúcar. Puede utilizarse como período de análisis y planificación, pero no sustituye la fecha operativa de cada turno.

### Transportista

Persona o entidad habilitada para operar uno o más camiones mediante asociaciones explícitas. En el MVP, un camión puede tener como máximo un transportista autorizado **activo** a la vez; las asociaciones revocadas conservan su historial.

### Camión

Vehículo identificado por una patente normalizada y clasificado por tipo de flota. Puede quedar inactivo, pero no se elimina cuando tiene historial.

### Finca

Origen o procedencia de la carga. En el MVP contiene una referencia descriptiva de ubicación; coordenadas, polígonos y PostGIS quedan fuera de alcance.

### Turno

Reserva operativa para que un camión sea recibido por un ingenio en una ventana determinada. La solicitud identifica transportista, camión y finca e informa fecha/hora de corte y carga estimada. El turno conserva prioridad, estado, fechas y trazabilidad. El formato y la unidad exacta de la carga estimada siguen pendientes de contrato.

### Turno activo

Turno que todavía puede influir en la operación o consumir capacidad. La definición exacta se deriva de los estados no terminales especificados.

### Ventana horaria

Intervalo de recepción con capacidad limitada. AgroFlow asigna automáticamente la próxima ventana compatible; el transportista no elige una franja en el MVP. Duración y cupo se configuran por ingenio mediante datos de arranque, sin edición desde la interfaz en esta etapa. El escenario de demostración usa ventanas de 30 minutos y dos camiones de cupo, sin convertir esos valores en reglas fijas del producto. El calendario de recepción, la zona horaria y sus límites requieren una decisión adicional.

### Prioridad

Orden relativo utilizado para asignar capacidad: mayor tiempo transcurrido desde el corte primero; a igualdad, flota propia antes que flota de terceros; si persiste el empate, fecha/hora de solicitud como desempate estable. Los detalles de medición y tratamiento de datos inválidos se cierran en el contrato y las pruebas.

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
