# Delta: Camiones en curso de cualquier fecha

## ADDED Requirements

### Requirement: Consulta activa independiente de la cola diaria

La lista de camiones en curso del panel SHALL consultar
`GET /api/v1/appointments?active=true` sin fecha. SHALL mostrar los turnos
recibidos en `EN_CAMINO`, `EN_ESPERA`, `INGRESADO` y `EN_DESCARGA`, con
fecha, hora y desplazamiento de ventana. Los turnos `ASIGNADO` MUST NOT
mostrarse ni contarse en Camiones en curso, porque todavía no iniciaron el
viaje; se consultan en Cola de turnos para su fecha y no se crea un grupo
separado de asignados dentro del panel. Esta lista MUST NOT cambiar
los indicadores diarios ni crear reglas de asignación o transiciones.

#### Scenario: Camión atrasado o asignado para otro día

- **GIVEN** turnos en curso con ventanas anteriores y posteriores al día actual
- **WHEN** se consulta o refresca el panel
- **THEN** se muestran los turnos recibidos que están efectivamente en curso, con sus fechas identificables

#### Scenario: Turno asignado que todavía no inició el viaje

- **GIVEN** una respuesta activa con un turno ASIGNADO y otro EN_CAMINO
- **WHEN** se muestra el panel
- **THEN** sólo el EN_CAMINO se muestra y cuenta en Camiones en curso, y el ASIGNADO permanece consultable en la cola para su fecha

#### Scenario: Estado terminal persistido

- **GIVEN** un camión visible que alcanza FINALIZADO o CANCELADO
- **WHEN** se confirma la transición y se vuelve a consultar, o lo detecta el polling
- **THEN** deja de aparecer en camiones en curso sin modificar la cola diaria

#### Scenario: Fallas de consulta activa

- **GIVEN** un panel que consulta periódicamente la API
- **WHEN** falla la primera carga o un refresco posterior
- **THEN** se informa el error, no se afirma un cero falso en la primera carga y se conservan los datos previos marcados como desactualizados cuando existen
