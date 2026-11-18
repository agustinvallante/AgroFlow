# Delta: Recuperación del listado de transportistas

## ADDED Requirements

### Requirement: Lectura recuperable de transportistas en LocalDemo

La vista de transportistas SHALL ofrecer Reintentar ante un error inicial y
Actualizar después de una carga válida. Una respuesta exitosa SHALL limpiar
el error correspondiente. Si falla un refresco, SHALL conservar el último
listado, resumen y filtros, mostrando datos desactualizados. Un error MUST NOT
representarse como una lista vacía exitosa. Las lecturas atrasadas o que terminen
después de desmontar la vista MUST NOT reemplazar el resultado vigente.

#### Scenario: Recuperación después de una falla inicial

- **GIVEN** un listado que no pudo cargarse
- **WHEN** el usuario reintenta y la API responde correctamente
- **THEN** se retira el error y se muestran los datos recibidos sin navegar fuera de la vista

#### Scenario: Fallo de actualización con datos previos

- **GIVEN** transportistas válidos visibles y filtros seleccionados
- **WHEN** falla una actualización
- **THEN** se conservan los datos y filtros, se informa la desactualización y Actualizar permite intentar nuevamente

#### Scenario: Actualizaciones fuera de orden

- **GIVEN** una actualización pendiente y otra posterior ya completada
- **WHEN** termina la primera
- **THEN** no reemplaza el listado de la consulta más reciente
