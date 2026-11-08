# Delta: Datos maestros

## ADDED Requirements

### Requirement: Consulta de fincas por nombre habitual (CU-012)

AgroFlow SHALL permitir consultar en sólo lectura las fincas del ingenio, filtradas por actividad y por una búsqueda con el nombre o el código tal como lo expresa una persona. La búsqueda SHALL comparar palabras completas del código y del nombre sin distinguir mayúsculas, tildes ni separadores, descartando la palabra genérica "finca" y los artículos, y SHALL exigir que cada palabra buscada coincida. MUST NOT devolver coincidencias parciales o por similitud, ni fincas de otro ingenio. Cuando varias fincas coinciden SHALL devolverlas todas; cuando ninguna coincide SHALL devolver una colección vacía. La consulta MUST exigir una credencial del canal o del dashboard y MUST NOT modificar datos.

#### Scenario: Código exacto o nombre natural

- **GIVEN** una finca activa con código `FINCA-NORTE` y nombre `Finca Norte`
- **WHEN** se consulta con `FINCA-NORTE`, `Finca Norte` o `norte`
- **THEN** AgroFlow devuelve únicamente esa finca con su código canónico

#### Scenario: Nombre compartido

- **GIVEN** dos fincas activas cuyos nombres contienen la misma palabra
- **WHEN** se consulta con esa palabra
- **THEN** AgroFlow devuelve ambas sin elegir una

#### Scenario: Finca inexistente o inactiva

- **GIVEN** un nombre que no corresponde a ninguna finca activa
- **WHEN** se consulta sólo entre fincas activas
- **THEN** AgroFlow responde éxito con una colección vacía

#### Scenario: Consulta sin credencial

- **GIVEN** una solicitud sin clave del canal ni del dashboard, o con una clave inválida
- **WHEN** consulta las fincas
- **THEN** AgroFlow la rechaza como no autenticada sin exponer el catálogo

#### Scenario: Falla de la consulta

- **GIVEN** que la persistencia no puede leer el catálogo
- **WHEN** se consultan las fincas
- **THEN** AgroFlow responde un error interno y nunca una colección vacía
