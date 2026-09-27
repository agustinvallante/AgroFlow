# Delta: Consulta de turnos

## ADDED Requirements

### Requirement: Listado local y filtros canónicos (CU-003, CU-004)

La demo local SHALL listar turnos del único ingenio sembrado y SHALL admitir los filtros opcionales `date`, `status`, `truckPlate` y `phone` definidos en OpenAPI. Cuando no se informa `date`, la API SHALL usar la fecha local actual del ingenio sembrado, también si se informan otros filtros. Todos los filtros SHALL combinarse con AND y el resultado SHALL ordenarse por inicio de ventana ascendente; sin coincidencias SHALL devolver una colección vacía. El filtro `phone` SHALL permitir que el chatbot consulte por el número del remitente sin resolver previamente un UUID de transportista. La consulta MUST NOT modificar datos.

#### Scenario: Listado por defecto

- **GIVEN** turnos sembrados o creados para la fecha local actual y para otras fechas
- **WHEN** se consulta el listado sin filtros
- **THEN** la API devuelve sólo los turnos de la fecha local actual ordenados por inicio de ventana ascendente

#### Scenario: Filtros sin fecha

- **GIVEN** turnos que cumplen `status`, `truckPlate` o `phone` en la fecha local actual y en otra fecha
- **WHEN** se consulta el listado con esos filtros sin informar `date`
- **THEN** la API devuelve únicamente los turnos de la fecha local actual que cumplen todos los filtros

#### Scenario: Filtros combinados con fecha explícita

- **GIVEN** turnos con fechas, estados, patentes y teléfonos de transportista diferentes
- **WHEN** se informa `date` junto con otros filtros válidos
- **THEN** la API devuelve únicamente los turnos de esa fecha que cumplen todos los filtros

#### Scenario: Consulta del chatbot por teléfono

- **GIVEN** un mensaje entrante desde un teléfono ficticio asociado a un transportista del seed
- **WHEN** n8n lista turnos con el filtro `phone` sin informar `date`
- **THEN** la API devuelve únicamente los turnos de ese transportista para la fecha local actual del ingenio, sin requerir una consulta de datos maestros

#### Scenario: Consulta sin coincidencias

- **GIVEN** un `phone` o una patente sin turnos en la fecha consultada, o inexistentes en el seed
- **WHEN** se consulta el listado con ese filtro
- **THEN** la API responde éxito con una colección vacía sin modificar turnos

#### Scenario: Filtro inválido

- **GIVEN** una fecha, estado, patente o teléfono fuera del formato definido en OpenAPI, o un parámetro no documentado, repetido o vacío
- **WHEN** se consulta el listado
- **THEN** la API responde un error de validación sin modificar turnos

### Requirement: Detalle local vigente (CU-005)

La demo local SHALL devolver por identificador el detalle vigente de un turno del ingenio sembrado y MUST NOT alterar su estado, ventana ni capacidad.

#### Scenario: Detalle existente

- **GIVEN** un turno persistido
- **WHEN** se consulta su identificador
- **THEN** la API devuelve el mismo estado y datos vigentes que utiliza el backend

#### Scenario: Identificador inexistente

- **GIVEN** un UUID bien formado que no corresponde a un turno
- **WHEN** se consulta el detalle
- **THEN** la API responde el error de recurso inexistente definido en OpenAPI sin revelar datos adicionales
