# Guías del frontend

Aquí se documentarán aspectos exclusivos de la aplicación web: instalación, estructura, componentes, navegación, accesibilidad, manejo de estado y pruebas de interfaz.

El prototipo orienta la experiencia visual, pero no reemplaza OpenSpec ni el contrato. La interfaz debe representar el estado confirmado por el backend y no calcular por su cuenta prioridad, capacidad, permisos o transiciones.

Para el MVP, la cola visual debe mostrar las franjas del día —incluidas las vacías— y los cupos restantes obtenidos de la API. La duración y el cupo se preparan por ingenio al iniciar el ambiente; su edición desde la pantalla de configuración no forma parte del MVP. El calendario y la zona horaria siguen pendientes de `OD-004`. La interfaz interna permite a operador y supervisor gestionar los tres catálogos y crear o cancelar turnos de su ingenio conforme a las specs aprobadas.

## Estado actual

`frontend/` es un espacio reservado. La incorporación del prototipo debe preservar trazabilidad con las capacidades y sustituir datos simulados por el contrato aprobado de forma incremental.
