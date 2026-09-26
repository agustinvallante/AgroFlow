# Delta: Consulta de turnos

## ADDED Requirements

### Requirement: Listado local y filtros canónicos (CU-003, CU-004)

La demo local SHALL listar turnos del único ingenio sembrado y SHALL admitir los filtros opcionales `date`, `status`, `truckPlate` y `phone` definidos en OpenAPI. El filtro `phone` SHALL permitir que el chatbot consulte por el número del remitente sin resolver previamente un UUID de transportista. Sin filtros, SHALL devolver los turnos de la fecha local actual ordenados por inicio de ventana ascendente. La consulta MUST NOT modificar datos.

#### Scenario: Listado por defecto

- **GIVEN** turnos sembrados o creados para la fecha local actual
- **WHEN** se consulta el listado sin filtros
- **THEN** la API devuelve esos turnos ordenados por inicio de ventana ascendente

#### Scenario: Filtros combinados

- **GIVEN** turnos con fechas, estados, patentes y teléfonos de transportista diferentes
- **WHEN** se combinan filtros válidos, incluido `phone` cuando corresponde
- **THEN** la API devuelve únicamente los turnos que cumplen todos los filtros

#### Scenario: Consulta del chatbot por teléfono

- **GIVEN** un mensaje entrante desde un teléfono ficticio asociado a un transportista del seed
- **WHEN** n8n lista turnos con el filtro `phone`
- **THEN** la API devuelve únicamente los turnos de ese transportista sin requerir una consulta de datos maestros

#### Scenario: Filtro inválido

- **GIVEN** una fecha, estado o teléfono fuera del formato definido en OpenAPI
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
