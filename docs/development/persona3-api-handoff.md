# Entrega parcial de Persona 3: API de turnos de la demo local

Esta rama implementa la parte de Persona 3 que puede desarrollarse contra el contrato `0.3.0` ya publicado por Persona 1. La fuente de verdad sigue siendo [`docs/contracts/openapi.yaml`](../contracts/openapi.yaml) junto con [`openspec/changes/demo-local-turnos-basicos/`](../../openspec/changes/demo-local-turnos-basicos/). El [traspaso de Persona 1](persona1-contract-handoff.md) resume las precisiones nuevas sin sustituir el contrato. Este documento registra límites de integración: **no declara terminados los criterios de aceptación ni habilita el recorrido local completo**.

## Frontera de trabajo

- Persona 1 ya publicó el contrato HTTP de la demo. Si se necesita cambiar una respuesta, un filtro o una regla observable, primero hay que acordarlo con quien mantiene OpenAPI/OpenSpec.
- Persona 3 prepara las cuatro operaciones `/api/v1/appointments`, su validación, reglas de transición y pruebas aisladas. La lógica de negocio debe vivir en el backend; el dashboard y n8n sólo consumen respuestas.
- Persona 2 aporta el modelo de turno y ventana, persistencia local, asociaciones y seed. No hay aún una implementación productiva del puerto de almacenamiento usado por Persona 3. Las pruebas con dobles de almacenamiento verifican la lógica y el contrato HTTP, **no** la persistencia real, la concurrencia ni la liberación física de cupo.

## Integración pendiente con Persona 2

Al integrar las ramas, el adaptador de persistencia debe:

1. Resolver las referencias activas del único ingenio sembrado y comprobar la asociación camión-transportista: teléfono E.164 con `+` por comparación exacta, patente comparando sólo letras y dígitos sin distinguir mayúsculas, y código de finca sin distinguir mayúsculas ni aplicar otra normalización. No recibir ni requerir UUIDs del cliente. El seed debe respetar la unicidad de estas claves normalizadas.
2. Exponer ventanas, ocupación, calendario y zona horaria del ingenio sembrado desde datos de arranque, no desde constantes de dominio. `AppointmentService` elige la primera ventana futura con cupo y prueba la siguiente si perdió el cupo por una carrera.
3. Confirmar atómicamente la ventana elegida, crear el turno `ASIGNADO` y reservar su cupo. Un turno no terminal para el mismo camión o la falta de capacidad deben dejar la base intacta y producir el conflicto canónico. Una lectura previa seguida de una escritura sin control no alcanza.
4. Consultar turnos y detalle desde los datos persistidos; los filtros se combinan con AND y el orden es por inicio de ventana ascendente. Si falta `date`, usar **siempre** la fecha local actual del ingenio, incluso con `status`, `truckPlate` o `phone`; comparar la fecha local de inicio de cada ventana. Sin coincidencias, devolver `[]`. La zona `America/Argentina/Tucuman` proviene del seed, no del cliente ni de una constante general.
5. Persistir cada transición con comprobación del estado vigente. La cancelación permitida conserva el turno consultable y libera el cupo en la misma transacción. Si el estado cambió concurrentemente, devolver conflicto sin sobrescribirlo.
6. Entregar los `date-time` observables (`cutAt`, `createdAt` y ventana) con el desplazamiento horario explícito del ingenio sembrado. El instante debe conservarse aunque el `cutAt` recibido tenga otro desplazamiento.
7. Inyectar `IAppointmentStore` y `IAppointmentService` en el host HTTP y completar el arranque local sin las dependencias obligatorias del backend heredado (SQL Server LocalDB, Identity/JWT y seed de ecommerce). El proyecto Data referencia hoy sólo a Domain: el adaptador podrá agregar una referencia a Application o ubicarse en otro proyecto sin crear dependencias circulares. Exponer `/health`: `200` JSON `Healthy` o `503` Problem Details `SERVICE_UNAVAILABLE`; comprobar CORS para el dashboard local y el puerto `5000`. El endpoint heredado `/healthcheck` no satisface el contrato.

Hasta completar esa integración, no debe mergearse esta rama como una API de demo lista ni presentarse como funcional de extremo a extremo. Las pruebas HTTP se ejecutan en un host aislado y las del servicio con un almacenamiento simulado; ninguna levanta la aplicación heredada con datos persistidos.

## Aclaraciones ya resueltas y decisiones aún abiertas

- Persona 1 resolvió que `phone` sin `date` consulta únicamente los turnos de **hoy** en la zona del ingenio. Para turnos de otra fecha, el consumidor debe enviar `date` o consultar un `id` conocido; n8n no debe inferir otra regla. Los parámetros de consulta no documentados, repetidos o vacíos devuelven `400 VALIDATION_ERROR`.
- Los errores por JSON mal formado o cuerpo ausente no deben inventar un campo `body` en `errors`; `errors` se omite cuando no hay una propiedad o un parámetro identificable. Verificar también que un POST sin `Content-Type: application/json` no salga con el error heredado fuera de Problem Details.
- El contrato requiere `cutAt` con desplazamiento horario explícito y carga positiva, pero no define si un corte futuro o muy antiguo debe rechazarse. No agregar ese límite por intuición en la demo.
- La demo busca entre ventanas futuras sembradas. El horizonte general de búsqueda, idempotencia y política de concurrencia del MVP completo siguen abiertos en `docs/planning/open-decisions.md`; no atribuirles una resolución por esta implementación local.

## Verificación que falta para cerrar Persona 3

- Levantar la API con la base local limpia y seed repetible de Persona 2.
- Probar HTTP real contra el host configurado: `/health` (`200`/`503`), alta `201` con `Location`, filtros y detalle `200`, validación `400`, referencia/turno inexistente `404` y conflictos `409`; comprobar `application/problem+json`, `code`, `traceId`, y que `phone` sin `date` usa hoy.
- Recorrer hasta `FINALIZADO`, rechazar saltos y estados terminales; cancelar otro turno, comprobar cupo reutilizable y repetir después de reiniciar el proceso para verificar persistencia.
- Ejecutar el recorrido del [`runbook local`](local-demo-runbook.md) desde frontend y n8n y recién entonces marcar la sección 3 de [`tasks.md`](../../openspec/changes/demo-local-turnos-basicos/tasks.md) como completada.
