# Delta de consulta interna de turnos del MVP

## ADDED Requirements

### Requirement: Permisos y aislamiento de las lecturas internas del MVP

El listado y el detalle internos del MVP MUST exigir un JWT válido de usuario interno con ámbito de ingenio verificable. AgroFlow MUST autorizar a operador y supervisor de ese ingenio y MUST aplicar el ámbito antes de filtrar, contar o proyectar datos. Un identificador de recurso, filtro de consulta o clave de LocalDemo MUST NOT reemplazar ese ámbito.

#### Scenario: Operador y supervisor del ingenio

- **GIVEN** un operador y un supervisor válidos del ingenio A y turnos de A y B
- **WHEN** cada uno lista turnos y consulta el detalle de un turno de A
- **THEN** sólo puede obtener turnos de A y el total del listado sólo cuenta los de A

#### Scenario: Identidad ausente o no válida

- **GIVEN** una lectura sin token, con token inválido o vencido o sin identidad de ingenio utilizable
- **WHEN** se solicita listado o detalle
- **THEN** AgroFlow responde `401 UNAUTHENTICATED` sin consultar turnos

#### Scenario: Rol sin permiso

- **GIVEN** un usuario autenticado con ingenio válido y un rol que no permite la lectura
- **WHEN** solicita listado o detalle
- **THEN** AgroFlow responde `403 FORBIDDEN` sin consultar turnos

#### Scenario: Detalle ajeno o inexistente

- **GIVEN** un usuario autorizado de A, un turno de B y un identificador inexistente
- **WHEN** consulta ambos detalles
- **THEN** ambas consultas responden `404 APPOINTMENT_NOT_FOUND` con la misma estructura y mensaje, sin incluir datos del turno

### Requirement: Fecha operativa y paginación de la cola interna

AgroFlow SHALL interpretar la fecha del listado en la zona horaria configurada para el ingenio autenticado. El inicio de la ventana MUST caer en el intervalo entre la medianoche local de esa fecha incluida y la medianoche local siguiente excluida. Sin fecha explícita, SHALL usar el día calendario actual en esa zona. La consulta MUST combinar los filtros aprobados, ordenar por instante de inicio y luego por identificador ascendente, y paginar en la base de datos. Los parámetros, sus límites y la estructura de respuesta SHALL seguir el [contrato HTTP](../../../../../docs/contracts/openapi.mvp-appointment-reads.yaml).

#### Scenario: Ventanas en el límite del día local

- **GIVEN** dos ventanas que empiezan en la medianoche local del día consultado y del día siguiente
- **WHEN** el operador lista la fecha del primer inicio
- **THEN** la consulta incluye la primera ventana y excluye la segunda, con independencia de la fecha UTC

#### Scenario: Fecha por defecto con filtros combinados

- **GIVEN** turnos de varias fechas y estados y una hora UTC cuya fecha difiere del día actual del ingenio
- **WHEN** el operador lista por estado y patente sin informar fecha
- **THEN** sólo aparecen turnos del día actual del ingenio que cumplen ambos filtros

#### Scenario: Orden estable con ventanas simultáneas

- **GIVEN** turnos del mismo ingenio con igual inicio de ventana que ocupan más de una página
- **WHEN** se recorren las páginas con el mismo filtro y sin cambios concurrentes
- **THEN** cada turno aparece una sola vez, con desempate por identificador y un total común después de aplicar los filtros

#### Scenario: Sin coincidencias o página fuera de rango

- **GIVEN** un listado válido sin coincidencias o con una página posterior a la última
- **WHEN** el usuario autorizado solicita el listado
- **THEN** AgroFlow responde `200` con `items` vacío y el total de turnos que cumplen los filtros

#### Scenario: Parámetros inválidos

- **GIVEN** una lectura autenticada y autorizada con un parámetro desconocido, repetido, vacío o fuera de su rango
- **WHEN** la API valida la solicitud
- **THEN** responde `400 VALIDATION_ERROR` sin exponer datos de turnos

### Requirement: Proyección vigente para cola y detalle

El listado interno MUST incluir en cada fila los campos que consume la cola, conforme a OpenAPI, sin requerir una consulta de detalle por fila. Listado y detalle SHALL devolver valores persistidos, con timestamps RFC 3339 en la zona del ingenio. Las lecturas MUST incluir turnos históricos que cumplen el filtro y MUST NOT modificar estado, cupo o prioridad ni generar eventos de transición.

#### Scenario: Lectura de un turno terminal después de reiniciar

- **GIVEN** un turno cancelado con ventana y estado persistidos antes del reinicio
- **WHEN** el usuario autorizado consulta el detalle y lista su fecha y estado
- **THEN** ambas lecturas reflejan la misma ventana y el estado `CANCELADO` y conservan los datos persistidos
