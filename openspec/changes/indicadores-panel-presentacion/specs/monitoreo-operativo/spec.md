# Delta: Indicadores del panel para la presentación

## ADDED Requirements

### Requirement: Indicadores temporales del panel para la presentación

Mientras la API no publique el origen de los turnos ni el estado de la
molienda, el Panel general SHALL completar sus indicadores de la siguiente
forma, como excepción temporal de la presentación al cálculo desde el estado
almacenado por el backend:

- Camiones en espera ahora SHALL contar los turnos `EN_ESPERA` recibidos en
  `GET /api/v1/appointments?active=true`, sin filtrar por fecha de ventana, y
  MUST NOT mostrar un cero cuando no hay una respuesta válida.
- Turnos gestionados por el bot SHALL contar los turnos activos recibidos que
  el mismo navegador no creó con "Nuevo turno", junto con el total de turnos
  activos, y SHALL identificarse en pantalla como estimado para la demo.
- Estado de la molienda SHALL simularse en el navegador, iniciar en Operando y
  alternar con un interruptor. MUST NOT enviar avisos, registrar
  interrupciones ni modificar turnos, y SHALL indicarse como simulado.
- Espera promedio en canchón SHALL mostrarse sin dato.

Estas medidas MUST NOT presentarse como datos del backend ni usarse fuera del
Panel general.

#### Scenario: Camiones en espera con un turno de otro día

- **GIVEN** una respuesta activa con un turno `EN_CAMINO` y otro `EN_ESPERA` cuya ventana es de otro día
- **WHEN** se muestra el panel
- **THEN** Camiones en espera ahora indica 1

#### Scenario: Falla en la primera carga

- **GIVEN** que falla la primera consulta de turnos activos
- **WHEN** se muestra el panel
- **THEN** Camiones en espera ahora y Turnos gestionados por el bot muestran "—"

#### Scenario: Turno cargado desde el dashboard

- **GIVEN** dos turnos activos, uno creado con "Nuevo turno" en este navegador
- **WHEN** se muestra el panel
- **THEN** Turnos gestionados por el bot indica 1 de 2 turnos activos

#### Scenario: Detener la molienda simulada

- **GIVEN** la molienda simulada en Operando
- **WHEN** el usuario toca el interruptor
- **THEN** la tarjeta y el interruptor muestran Detenida sin generar cambios operativos
