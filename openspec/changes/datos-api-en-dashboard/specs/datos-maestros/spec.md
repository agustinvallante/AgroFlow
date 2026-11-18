# Delta: Datos maestros

## ADDED Requirements

### Requirement: Corrección de transportistas activos desde el dashboard

El dashboard SHALL permitir corregir nombre, DNI y WhatsApp de un transportista activo mediante la edición que publica AgroFlow API, con los datos actuales precargados y las mismas reglas de formato del alta. SHALL mostrar el transportista que devuelve la API después de guardar. MUST NOT ofrecer la edición de un transportista inactivo, porque la API no la admite. Los conflictos de identidad SHALL resolverse en la API, que conserva la unicidad del DNI y del WhatsApp.

#### Scenario: Corregir un dato

- **GIVEN** un transportista activo
- **WHEN** el operador abre su edición, cambia el nombre y guarda
- **THEN** el dashboard envía los tres datos a la API y muestra el transportista actualizado

#### Scenario: Transportista inactivo

- **GIVEN** un transportista inactivo
- **WHEN** el operador ve el listado
- **THEN** ese transportista no ofrece editar ni reactivar
