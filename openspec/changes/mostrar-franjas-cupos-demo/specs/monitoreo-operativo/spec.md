# Delta: Monitoreo operativo

## ADDED Requirements

### Requirement: Franjas del panel en LocalDemo

El panel general de LocalDemo SHALL leer `GET /api/v1/windows` y representar
todas las ventanas recibidas, incluidas las vacías y completas, mostrando
inicio, fin, capacidad, ocupados y disponibles del backend. MUST NOT recalcular
disponibilidad desde turnos ni suponer duración, cupo o cantidad fija de franjas.
SHALL permitir una fecha YYYY-MM-DD opcional; sin fecha SHALL delegar hoy al
servidor. Los horarios SHALL conservar la fecha y el desplazamiento del ingenio
emitidos por la API, incluso al cruzar medianoche.

#### Scenario: Franjas vacías y completas

- **GIVEN** ventanas vacías, parcialmente ocupadas y sin cupos, con distintas capacidades y duraciones
- **WHEN** se abre el panel
- **THEN** aparecen todas en el orden recibido y con los valores exactos de capacidad, ocupados y disponibles, sin inventar tipos de flota

#### Scenario: Fecha del ingenio

- **GIVEN** un navegador en otra zona horaria y una ventana que cruza la medianoche del ingenio
- **WHEN** se consulta sin fecha o con una fecha elegida
- **THEN** sin fecha se omite date, con fecha se envía la elegida y el rótulo conserva las fechas y el desplazamiento recibidos

#### Scenario: Día sin ventanas

- **GIVEN** una respuesta exitosa con un arreglo vacío
- **WHEN** se muestra la lectura
- **THEN** se informa que no hay ventanas configuradas para esa fecha, sin inventar franjas ni presentar un error

### Requirement: Refresco y recuperación de franjas en LocalDemo

El panel SHALL releer ventanas al abrirse, cada cuatro segundos sin acumular
consultas del mismo ciclo, a pedido y después de una transición desde el panel.
Altas o cancelaciones desde otra pantalla o canal SHALL reflejarse mediante
esa relectura periódica o al volver al panel. Una falla inicial SHALL mostrar
error y reintento sin afirmar que la fecha está vacía. Una falla posterior
SHALL conservar la última lectura válida de esa fecha, marcada como
desactualizada, y reintentar. Cambiar fecha MUST NOT mostrar datos ni aceptar
respuestas tardías de la fecha anterior. El filtro de ventanas MUST NOT limitar
la lectura independiente de camiones activos de otras fechas.

#### Scenario: Crear y cancelar

- **GIVEN** el panel abierto y una ventana con cupo
- **WHEN** la API confirma un alta o cancelación desde otro canal y llega la siguiente lectura
- **THEN** el panel muestra la ocupación y disponibilidad devueltas por la API, sin actualización optimista de cupos

#### Scenario: Cancelar desde el panel

- **GIVEN** un camión en curso que se cancela desde el panel
- **WHEN** finaliza la acción
- **THEN** se solicita inmediatamente una nueva lectura de ventanas

#### Scenario: Falla y recuperación

- **GIVEN** una lectura válida seguida por una falla de red
- **WHEN** se refrescan las franjas
- **THEN** la última lectura permanece marcada como desactualizada hasta una respuesta exitosa que reemplaza los datos y retira el aviso

#### Scenario: Error inicial

- **GIVEN** que todavía no hubo una lectura válida para la fecha elegida
- **WHEN** la API falla
- **THEN** se informa el error con reintento y no se presenta un día vacío

#### Scenario: Cambio de fecha con peticiones pendientes

- **GIVEN** una consulta pendiente de una fecha anterior
- **WHEN** se selecciona otra fecha y luego responde la consulta anterior
- **THEN** esa respuesta se ignora y sólo se muestran datos o errores de la fecha elegida, conservando aparte los camiones en curso de cualquier fecha

### Requirement: Acceso a ventanas mediante el intermediario local

El navegador SHALL consultar las ventanas en su mismo origen sin claves.
El proxy Vite SHALL agregar la clave del dashboard exclusivamente del lado
servidor al GET exacto de ventanas, con query opcional, sólo para clientes
loopback con Host local y mismo origen. MUST NOT reenviar claves AgroFlow
aportadas por el navegador, habilitar mutaciones/subrutas de ventanas ni abrir
las rutas del canal.

#### Scenario: Lectura local sin secreto en el navegador

- **GIVEN** la clave configurada exclusivamente en el servidor Vite
- **WHEN** el panel consulta ventanas desde el mismo origen local
- **THEN** el proxy agrega esa clave hacia la API sin incluirla en solicitudes, almacenamiento ni bundle del navegador

#### Scenario: Ruta u origen no autorizado

- **GIVEN** un cliente remoto, Host externo, origen cruzado, mutación o subruta de ventanas
- **WHEN** intenta usar el intermediario
- **THEN** la petición se bloquea y no recibe la clave del servidor
