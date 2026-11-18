# Usar en el dashboard los datos que la API ya publica

## Problema

- El alta manual de turnos pedía teléfono, patente y código de finca como texto libre, aunque la API publica transportistas, camiones asociados y el catálogo de fincas. Un error de tipeo sólo aparecía al rechazarlo la API.
- El proxy de Vite no agregaba la clave del dashboard a `GET /api/v1/farms`, que el backend autoriza para el dashboard, y respondía 401.
- El Chatbot sólo pedía la primera página de conversaciones e ignoraba `nextCursor`.
- La API publica la edición de transportistas activos, pero el dashboard sólo permitía darlos de baja: corregir un DNI o un WhatsApp exigía darlos de baja y volver a crearlos.
- Una carga estimada vacía se enviaba como 0 y la API respondía con un error que no señalaba el campo.
- Si fallaba una página posterior de mensajes se descartaban también las ya leídas, y al llegar al tope de páginas el historial se cortaba sin aviso.

## Cambio

- El proxy admite `GET /api/v1/farms` como ruta de sólo lectura, igual que ventanas y camiones.
- El alta manual ofrece transportistas habilitados, sus patentes asociadas y fincas activas, leídos de la API al abrir el formulario.
- Los transportistas activos ofrecen Editar, con sus datos precargados, mediante `PUT /api/v1/drivers/{id}`.
- El alta manual exige la carga estimada antes de enviarla.
- Conversaciones y mensajes se recorren hasta `nextCursor: null`, con un tope de 20 páginas. Una lectura incompleta muestra lo obtenido junto con un aviso.

No cambia el contrato HTTP ni el backend. La API sigue validando el alta.

## Capacidades

- [turnos-solicitud-y-asignacion](specs/turnos-solicitud-y-asignacion/spec.md)
- [conversaciones-whatsapp](specs/conversaciones-whatsapp/spec.md)
- [datos-maestros](specs/datos-maestros/spec.md)
