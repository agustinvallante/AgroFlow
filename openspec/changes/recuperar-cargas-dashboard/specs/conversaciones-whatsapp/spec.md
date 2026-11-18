# Delta: Recuperación del historial en el dashboard

## ADDED Requirements

### Requirement: Recuperación de la carga inicial de conversaciones

En el dashboard LocalDemo, un error inicial al leer conversaciones SHALL mostrar
el error y una acción Reintentar. El polling de quince segundos SHALL continuar.
Una respuesta posterior exitosa, automática o manual, SHALL retirar el error
inicial y mostrar la respuesta válida, sin exigir navegar a otra sección.
Un fallo HTTP MUST NOT presentarse como una lista vacía válida.

#### Scenario: Recuperación automática

- **GIVEN** que falló la primera lectura de conversaciones
- **WHEN** un refresco periódico posterior obtiene una respuesta válida
- **THEN** se retira el error inicial y se muestran las conversaciones o el estado vacío que confirme la API

#### Scenario: Reintento manual

- **GIVEN** una vista con error inicial
- **WHEN** el usuario pulsa Reintentar
- **THEN** se realiza una nueva lectura y, si vuelve a fallar, el error y la posibilidad de reintento siguen disponibles

### Requirement: Preservación de datos durante fallas de refresco

Después de una lectura válida, un error del listado SHALL conservar las últimas
conversaciones y la selección, mostrando un aviso de datos desactualizados.
Un error al refrescar mensajes SHALL conservar los mensajes ya obtenidos de la
misma conversación y mostrar el error. Una recuperación exitosa SHALL limpiar
el aviso correspondiente. Estas lecturas MUST NOT generar mensajes ni mutaciones.

#### Scenario: Fallo posterior con conversación abierta

- **GIVEN** una conversación y mensajes válidos visibles
- **WHEN** falla una consulta posterior del listado o de sus mensajes
- **THEN** permanecen los datos ya obtenidos de esa conversación junto al error y una actualización posterior permite recuperarlos

### Requirement: Respuestas vigentes para la selección actual

El frontend SHALL descartar resultados y errores de lecturas anteriores a una
consulta más reciente, de conversaciones que ya no son la selección vigente o
de vistas desmontadas. Al actualizar el listado, SHALL conservar la selección
si sigue presente; si dejó de estar, SHALL retirar sus mensajes y seleccionar
una conversación recibida o ninguna si la lista está vacía.

#### Scenario: Cambio de conversación con lectura pendiente

- **GIVEN** una lectura de mensajes pendiente de una conversación anterior
- **WHEN** el usuario selecciona otra y luego llega aquella respuesta o error
- **THEN** no reemplaza los mensajes ni el error de la selección actual

#### Scenario: Listado o falla inicial atrasados

- **GIVEN** dos lecturas del listado que terminan fuera de orden
- **WHEN** ya se aplicó el resultado de la consulta más reciente
- **THEN** la respuesta anterior no reemplaza ese listado ni vuelve a bloquear la pantalla con un error viejo

#### Scenario: Conversación retirada del listado

- **GIVEN** una conversación seleccionada con mensajes visibles
- **WHEN** un listado válido deja de incluirla
- **THEN** sus mensajes se retiran y no se reutilizan al mostrar otra conversación
