## MODIFIED Requirements

### Requirement: Factores de prioridad (RN-010, RN-011)

AgroFlow SHALL ordenar las solicitudes elegibles mediante una comparación lexicográfica estable que aplique, en este orden:

1. fecha y hora de corte de la caña más antigua;
2. ante igualdad en el minuto de corte, flota `PROPIA` antes que `TERCEROS`;
3. ante nueva igualdad, instante de recepción de la solicitud más antiguo;
4. ante nueva igualdad, identificador estable de la solicitud.

El backend SHALL aplicar el mismo orden en asignaciones iniciales y reprogramaciones y SHALL devolver una explicación breve de los factores que determinaron el resultado. Frontend y n8n MUST NOT recalcular ni sustituir este orden.

#### Scenario: Datos disponibles para priorizar

- **GIVEN** dos solicitudes elegibles para capacidad limitada
- **WHEN** el backend evalúa su prioridad
- **THEN** toma en cuenta el instante de corte, el tipo de flota, el instante de recepción y el identificador estable de cada solicitud en el orden definido

#### Scenario: Prioridad por antigüedad del corte

- **GIVEN** dos solicitudes elegibles con distinto instante de corte y capacidad para una sola
- **WHEN** el backend evalúa la prioridad
- **THEN** prioriza la solicitud con el corte más antiguo

#### Scenario: Desempate por tipo de flota

- **GIVEN** dos solicitudes elegibles con igual minuto de corte y distinto tipo de flota
- **WHEN** el backend evalúa la prioridad
- **THEN** prioriza la solicitud de flota `PROPIA`

#### Scenario: Desempates estables

- **GIVEN** solicitudes elegibles empatadas en corte y tipo de flota
- **WHEN** el backend evalúa la prioridad varias veces sobre el mismo estado
- **THEN** usa primero el instante de recepción y luego el identificador estable y produce el mismo orden

### Requirement: Alternativa ante falta de disponibilidad (RN-016)

La ventana preferida SHALL ser opcional y MUST NOT constituir una reserva. Cuando no se informe una preferencia, AgroFlow SHALL asignar la primera ventana compatible según capacidad y prioridad. Cuando la preferida no tenga capacidad o no sea compatible, AgroFlow SHALL asignar la próxima ventana compatible y SHALL informar el motivo del fallback.

#### Scenario: Solicitud sin ventana preferida

- **GIVEN** una solicitud elegible sin ventana preferida y al menos una ventana compatible
- **WHEN** AgroFlow realiza la asignación
- **THEN** asigna la primera ventana compatible según capacidad y prioridad

#### Scenario: Ventana preferida disponible

- **GIVEN** una solicitud elegible con una ventana preferida compatible y con capacidad
- **WHEN** AgroFlow realiza la asignación
- **THEN** asigna esa ventana como resultado autoritativo

#### Scenario: Ventana preferida completa

- **GIVEN** una solicitud elegible cuya ventana preferida no tiene capacidad
- **WHEN** existe una ventana posterior compatible
- **THEN** AgroFlow asigna la próxima ventana compatible e informa el motivo del fallback

#### Scenario: Ausencia total de capacidad

- **GIVEN** una solicitud elegible sin ventanas compatibles con capacidad
- **WHEN** AgroFlow intenta asignarla
- **THEN** no confirma un turno ni mantiene una reserva aparente e informa la falta de disponibilidad

## ADDED Requirements

### Requirement: Datos mínimos de la solicitud

AgroFlow SHALL exigir para una solicitud de turno un transportista identificado por el canal autorizado, un camión identificado por patente, una finca y la fecha y hora de corte de la caña. La ventana preferida SHALL ser opcional. El ingenio MUST derivarse del contexto autorizado y el tipo de flota MUST obtenerse del camión persistido.

El transportista, el camión, la finca y la asociación transportista-camión MUST estar activos; la fecha y hora de corte MUST NOT ser futura. Una solicitud inválida MUST NOT consumir capacidad. El MVP MUST NOT exigir peso, humedad, variedad, coordenadas, ruta, remito, fotos ni geolocalización.

#### Scenario: Solicitud mínima válida

- **GIVEN** un transportista, camión, finca y asociación activos del ingenio autorizado, una fecha de corte no futura y capacidad compatible
- **WHEN** se presenta la solicitud con los campos mínimos
- **THEN** AgroFlow la considera elegible para priorización y asignación

#### Scenario: Campo obligatorio ausente

- **GIVEN** una solicitud sin camión, finca o fecha y hora de corte
- **WHEN** el backend la valida
- **THEN** AgroFlow la rechaza, identifica el campo requerido y no consume capacidad

#### Scenario: Fecha de corte futura

- **GIVEN** una solicitud cuya fecha y hora de corte es posterior al instante vigente
- **WHEN** el backend la valida
- **THEN** AgroFlow la rechaza sin consumir capacidad

#### Scenario: Referencia inactiva o asociación inválida

- **GIVEN** una solicitud con transportista, camión o finca inactivos, o sin asociación activa entre transportista y camión
- **WHEN** el backend la valida
- **THEN** AgroFlow la rechaza sin asignar una ventana

### Requirement: Ventanas y capacidad del ingenio

AgroFlow SHALL usar ventanas fijas de 30 minutos con intervalos `[inicio, fin)`, MUST exigir una capacidad entera positiva para cada ventana configurada y SHALL mantener esa capacidad por ingenio y fecha operativa. Para el baseline, la fecha operativa SHALL interpretarse en la zona `America/Argentina/Tucuman` y una ventana MUST NOT cruzar esa fecha.

Cada turno activo SHALL consumir una unidad de capacidad. La cancelación de un turno SHALL liberar una unidad y una interrupción SHALL excluir de nuevas asignaciones las ventanas afectadas.

#### Scenario: Asignación dentro de una ventana

- **GIVEN** una ventana configurada de 30 minutos con capacidad disponible en el ingenio
- **WHEN** AgroFlow confirma un turno en esa ventana
- **THEN** consume exactamente una unidad de su capacidad

#### Scenario: Capacidad no positiva

- **GIVEN** una configuración de ventana con capacidad cero o negativa
- **WHEN** un actor con el permiso de configuración de ventanas y capacidad definido por `acceso-y-autorizacion` intenta habilitarla
- **THEN** AgroFlow rechaza la configuración sin ofrecer la ventana

#### Scenario: Límite entre ventanas

- **GIVEN** dos ventanas consecutivas de una fecha operativa
- **WHEN** un instante coincide con el fin de la primera y el inicio de la segunda
- **THEN** AgroFlow lo considera perteneciente únicamente a la segunda ventana

#### Scenario: Ventana de otro ingenio

- **GIVEN** una solicitud de un ingenio y una ventana configurada para otro
- **WHEN** AgroFlow busca capacidad
- **THEN** excluye esa ventana de la asignación

### Requirement: Asignación atómica ante concurrencia

AgroFlow MUST confirmar la creación del turno, la exclusión de otro turno activo para el camión y el consumo de capacidad como un único resultado transaccional. Ante solicitudes concurrentes incompatibles, SHALL persistir únicamente los resultados que respeten capacidad y exclusión y SHALL informar un conflicto recuperable a las restantes.

#### Scenario: Última unidad disputada

- **GIVEN** una ventana con una sola unidad disponible y dos solicitudes concurrentes elegibles
- **WHEN** ambas intentan consumir esa capacidad
- **THEN** AgroFlow confirma como máximo una asignación y la otra no consume capacidad

#### Scenario: Solicitudes concurrentes para el mismo camión

- **GIVEN** un camión sin turno activo y dos solicitudes concurrentes para ese camión
- **WHEN** ambas intentan confirmar un turno
- **THEN** AgroFlow confirma como máximo un turno activo e informa el estado vigente a la operación en conflicto

#### Scenario: Falla durante la asignación

- **GIVEN** una asignación que no puede completar todas sus escrituras
- **WHEN** la transacción falla
- **THEN** AgroFlow no confirma el turno ni deja capacidad consumida parcialmente
