# Entrega parcial de Persona 3: API de turnos de la demo local

Esta rama implementa la parte de Persona 3 que puede desarrollarse contra el contrato ya publicado. La fuente de verdad sigue siendo [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml) junto con [`openspec/changes/demo-local-turnos-basicos/`](../../openspec/changes/demo-local-turnos-basicos/). Este documento registra límites de integración: **no declara terminados los criterios de aceptación ni habilita el recorrido local completo**.

## Frontera de trabajo

- Persona 1 ya publicó el contrato HTTP de la demo. Si se necesita cambiar una respuesta, un filtro o una regla observable, primero hay que acordarlo con quien mantiene OpenAPI/OpenSpec.
- Persona 3 prepara las cuatro operaciones `/api/v1/appointments`, su validación, reglas de transición y pruebas aisladas. La lógica de negocio debe vivir en el backend; el dashboard y n8n sólo consumen respuestas.
- Persona 2 aporta el modelo de turno y ventana, persistencia local, asociaciones y seed. No hay aún una implementación productiva del puerto de almacenamiento usado por Persona 3. Las pruebas con dobles de almacenamiento verifican la lógica y el contrato HTTP, **no** la persistencia real, la concurrencia ni la liberación física de cupo.

## Integración pendiente con Persona 2

Al integrar las ramas, el adaptador de persistencia debe:

1. Resolver por teléfono E.164, patente y código de finca las referencias activas del único ingenio sembrado, y comprobar que el camión está asociado al transportista. No recibir ni requerir UUIDs del cliente.
2. Exponer ventanas, ocupación, calendario y zona horaria del ingenio sembrado desde datos de arranque, no desde constantes de dominio. `AppointmentService` elige la primera ventana futura con cupo y prueba la siguiente si perdió el cupo por una carrera.
3. Confirmar atómicamente la ventana elegida, crear el turno `ASIGNADO` y reservar su cupo. Un turno no terminal para el mismo camión o la falta de capacidad deben dejar la base intacta y producir el conflicto canónico. Una lectura previa seguida de una escritura sin control no alcanza.
4. Consultar turnos y detalle desde los datos persistidos; los filtros se combinan con AND y el orden es por inicio de ventana ascendente. La fecha por defecto debe evaluarse en `America/Argentina/Tucuman` según el perfil local, no en la zona del cliente.
5. Persistir cada transición con comprobación del estado vigente. La cancelación permitida conserva el turno consultable y libera el cupo en la misma transacción. Si el estado cambió concurrentemente, devolver conflicto sin sobrescribirlo.
6. Inyectar `IAppointmentStore` y `IAppointmentService` en el host HTTP y completar el arranque local sin las dependencias obligatorias del backend heredado (SQL Server LocalDB, Identity/JWT y seed de ecommerce). El proyecto Data referencia hoy sólo a Domain: el adaptador podrá agregar una referencia a Application o ubicarse en otro proyecto sin crear dependencias circulares. Exponer `/health` como marca OpenAPI y comprobar CORS para el dashboard local. El endpoint heredado `/healthcheck` no satisface el contrato.

Hasta completar esa integración, no debe mergearse esta rama como una API de demo lista ni presentarse como funcional de extremo a extremo. Actualmente pasan 43 pruebas de backend, incluidas pruebas HTTP en un host aislado y pruebas del servicio con un almacenamiento simulado; ninguna levanta la aplicación heredada con datos persistidos.

## Puntos a confirmar con Persona 1

- El texto de `GET /api/v1/appointments` dice que **sin filtros** se usa la fecha local actual, mientras que la descripción del parámetro `date` dice “por defecto, la fecha local actual”. Hay que confirmar si `phone` sin `date` consulta todas las fechas o sólo la fecha actual; esto afecta al chatbot. Las pruebas y la implementación deben seguir la interpretación explícitamente aprobada y actualizar OpenAPI si cambia.
- El contrato requiere `cutAt` con desplazamiento horario explícito y carga positiva, pero no define si un corte futuro o muy antiguo debe rechazarse. No agregar ese límite por intuición en la demo.
- La demo busca entre ventanas futuras sembradas. El horizonte general de búsqueda, idempotencia y política de concurrencia del MVP completo siguen abiertos en `docs/planning/open-decisions.md`; no atribuirles una resolución por esta implementación local.

## Verificación que falta para cerrar Persona 3

- Levantar la API con la base local limpia y seed repetible de Persona 2.
- Probar HTTP real contra el host configurado: alta `201` con `Location`, filtros y detalle `200`, validación `400`, referencia/turno inexistente `404` y conflictos `409`; comprobar `application/problem+json`, `code` y `traceId`.
- Recorrer hasta `FINALIZADO`, rechazar saltos y estados terminales; cancelar otro turno, comprobar cupo reutilizable y repetir después de reiniciar el proceso para verificar persistencia.
- Ejecutar el recorrido del [`runbook local`](local-demo-runbook.md) desde frontend y n8n y recién entonces marcar la sección 3 de [`tasks.md`](../../openspec/changes/demo-local-turnos-basicos/tasks.md) como completada.
