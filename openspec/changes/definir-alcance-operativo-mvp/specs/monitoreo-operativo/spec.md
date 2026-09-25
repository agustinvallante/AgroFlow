## ADDED Requirements

### Requirement: Cola de ventanas y cupos del MVP

AgroFlow SHALL ofrecer al dashboard las ventanas de recepción del ingenio consultado para la fecha operativa, incluidas las franjas sin turnos, junto con su cupo configurado, ocupación vigente y cupos restantes. El frontend SHALL representar esos datos recibidos del backend sin recalcular la disponibilidad como una regla independiente. La interpretación exacta de la fecha operativa y el calendario continúa sujeta a `OD-004` y `OD-012`.

#### Scenario: Franja vacía

- **GIVEN** una ventana configurada para el ingenio sin turnos que consuman capacidad
- **WHEN** un usuario autorizado consulta la cola visual
- **THEN** la ventana aparece con ocupación cero y todos sus cupos disponibles

#### Scenario: Capacidad liberada por cancelación

- **GIVEN** una ventana cuyo turno fue cancelado y cuya cancelación ya está persistida
- **WHEN** el dashboard vuelve a consultar la cola
- **THEN** el backend informa el cupo liberado y el frontend muestra la disponibilidad vigente
