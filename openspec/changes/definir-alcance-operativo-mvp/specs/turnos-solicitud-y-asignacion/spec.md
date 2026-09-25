## MODIFIED Requirements

### Requirement: Factores de prioridad (RN-010, RN-011)

Cuando varias solicitudes elegibles compitan por capacidad, AgroFlow SHALL priorizar primero la que lleve más tiempo transcurrido desde el corte de la caña. Si ese tiempo empata, SHALL priorizar el camión de flota propia frente al de terceros. Si también empata el tipo de flota, SHALL usar primero la fecha y hora de recepción de solicitud más antigua, registrada por el backend de forma estable. El backend SHALL aplicar este mismo orden al asignar y al reprogramar turnos.

#### Scenario: Mayor espera desde el corte

- **GIVEN** dos solicitudes elegibles para capacidad limitada
- **WHEN** una de ellas tiene un corte anterior al de la otra
- **THEN** AgroFlow le da prioridad sin importar el tipo de flota

#### Scenario: Desempate por flota y solicitud

- **GIVEN** dos solicitudes elegibles con el mismo tiempo desde el corte
- **WHEN** compiten por capacidad limitada
- **THEN** AgroFlow atiende primero la de flota propia y, si ambas tienen el mismo tipo de flota, la registrada antes

### Requirement: Alternativa ante falta de disponibilidad (RN-016)

AgroFlow SHALL asignar automáticamente la primera ventana futura compatible con el calendario y el cupo del ingenio, las interrupciones vigentes y la política de prioridad, sin solicitar una preferencia de horario. Si una ventana está completa, SHALL continuar con la próxima compatible; MUST NOT confirmar un turno sin ventana.

#### Scenario: Primera ventana sin cupo

- **GIVEN** una solicitud elegible y una primera ventana compatible sin capacidad
- **WHEN** existe una ventana posterior compatible con cupo
- **THEN** AgroFlow asigna esa próxima ventana disponible sin pedir al solicitante una alternativa

## ADDED Requirements

### Requirement: Datos mínimos de la solicitud

Una solicitud de turno SHALL identificar al transportista, camión y finca; MUST incluir el momento de corte de la caña y la carga estimada. AgroFlow MUST NOT aceptar una ventana preferida como criterio de asignación para el MVP. La forma exacta y las validaciones de los campos que siguen abiertos en `OD-005` se definirán en el contrato.

#### Scenario: Solicitud con datos requeridos

- **GIVEN** un transportista, camión y finca elegibles
- **WHEN** AgroFlow recibe una solicitud con momento de corte y carga estimada válidos
- **THEN** puede evaluar su prioridad y buscar una ventana sin requerir la elección de una franja

#### Scenario: Solicitud incompleta

- **GIVEN** una solicitud sin momento de corte o sin carga estimada
- **WHEN** el backend la valida
- **THEN** AgroFlow la rechaza sin asignar una ventana

### Requirement: Configuración inicial de ventanas por ingenio

AgroFlow SHALL determinar las ventanas de cada ingenio mediante la duración y el cupo configurados para ese ingenio al iniciar el ambiente. En el MVP MUST NOT requerir una pantalla para modificar estos valores durante la operación. El calendario de recepción, la zona horaria, los límites horarios y el horizonte de búsqueda continúan sujetos a `OD-004`.

#### Scenario: Ingenios con capacidades distintas

- **GIVEN** dos ingenios con duración o cupo de ventana diferentes en su configuración inicial
- **WHEN** AgroFlow consulta o asigna sus ventanas
- **THEN** aplica la configuración correspondiente a cada ingenio sin utilizar un cupo global fijo

### Requirement: Creación interna de turnos

AgroFlow SHALL permitir que un operador o supervisor autorizado solicite un turno desde la interfaz interna para su ingenio, sujeto a las mismas reglas de datos, prioridad, asociación y capacidad que una solicitud proveniente de WhatsApp.

#### Scenario: Solicitud desde la interfaz interna

- **GIVEN** un operador o supervisor autorizado de un ingenio y datos válidos de transportista, camión y finca
- **WHEN** solicita un turno desde la interfaz interna
- **THEN** el backend asigna la próxima ventana compatible conforme a las mismas reglas aplicadas al canal conversacional
