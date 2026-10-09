# Plataforma y segregación

## Purpose

Define garantías transversales para que las capacidades de AgroFlow mantengan aislamiento, consistencia y una única fuente operativa de verdad.

## Requirements

### Requirement: Superficie HTTP exclusiva del dominio AgroFlow

La API de AgroFlow MUST NOT publicar operaciones ni contratos de productos, pedidos o clientes del comercio electrónico de origen. El retiro de esas rutas SHALL preservar los recorridos de turnos y salud vigentes en el perfil de demo local.

#### Scenario: Rutas heredadas ausentes

- **GIVEN** la API iniciada en el perfil de demo local
- **WHEN** se inspecciona Swagger o se solicitan las rutas de productos, pedidos y registro de clientes heredadas
- **THEN** las operaciones no aparecen en Swagger y las solicitudes no encuentran una ruta publicada

#### Scenario: Recorrido de turnos preservado

- **GIVEN** la API iniciada con la base y el seed de la demo local
- **WHEN** se consulta salud y se crea, lista o modifica un turno conforme al contrato vigente
- **THEN** el backend conserva el comportamiento y el estado persistido de esos recorridos

### Requirement: Continuidad de SQLite durante el renombre

El renombre de la API SHALL permitir reutilizar explícitamente su archivo SQLite previo sin borrar ni sustituir turnos ni historial de migraciones. Las rutas relativas SHALL resolverse contra la raíz de contenido de la API. Si existen archivos en la ubicación histórica y sólo se configuró una ruta relativa, el arranque MUST abortar antes de inicializar otra base y SHALL indicar el procedimiento de respaldo y selección absoluta. La API MUST NOT mover, copiar, borrar ni combinar esos archivos automáticamente.

#### Scenario: Base anterior detectada sin selección explícita

- **GIVEN** archivos SQLite de la demo anterior en la ubicación histórica y una cadena con ruta relativa
- **WHEN** se inicia la API renombrada
- **THEN** el arranque aborta antes de migrar o sembrar una base alternativa e indica el procedimiento de transición

#### Scenario: Reutilización después del respaldo

- **GIVEN** una base previa respaldada con turnos y una ruta absoluta explícita en modo ReadWrite
- **WHEN** se inicia y reinicia la API renombrada
- **THEN** los turnos conservan IDs y estados, las migraciones previas permanecen registradas, las pendientes se aplican y el seed no duplica los datos existentes

#### Scenario: Ruta de reutilización inexistente

- **GIVEN** una ruta absoluta en modo ReadWrite que no identifica un archivo existente
- **WHEN** se inicia la API renombrada
- **THEN** el arranque falla sin crear una base vacía en esa ubicación

### Requirement: Aislamiento efectivo por ingenio

El backend MUST validar la pertenencia al ingenio en cada operación con datos operativos y MUST NOT depender únicamente de filtros o elementos visibles en el frontend.

#### Scenario: Identificador válido de otro ingenio

- **GIVEN** un usuario autenticado y un recurso existente perteneciente a otro ingenio
- **WHEN** el usuario intenta consultarlo o modificarlo mediante su identificador
- **THEN** AgroFlow rechaza la operación sin revelar ni alterar el recurso

### Requirement: Una única fuente operativa de verdad

AgroFlow SHALL considerar al backend y su estado persistido como autoridad operativa; el frontend y n8n MUST NOT mantener decisiones alternativas de prioridad, capacidad, permisos o transiciones.

#### Scenario: Diferencia entre una vista y el backend

- **GIVEN** una vista local desactualizada y un estado más reciente persistido
- **WHEN** el consumidor vuelve a consultar o intenta operar
- **THEN** utiliza la respuesta vigente del backend y no impone el estado local

### Requirement: Confirmación posterior a persistencia

AgroFlow MUST confirmar una mutación únicamente después de que su estado haya sido persistido correctamente.

#### Scenario: Persistencia fallida

- **GIVEN** una operación válida cuya escritura no finaliza correctamente
- **WHEN** el backend prepara la respuesta
- **THEN** informa que la operación no fue confirmada y no expone un estado ficticio como vigente

### Requirement: Errores sin información sensible

AgroFlow MUST devolver errores suficientes para que los consumidores actúen sin exponer credenciales, secretos, datos internos innecesarios ni la existencia de recursos no autorizados.

#### Scenario: Error de autorización o validación

- **GIVEN** una solicitud inválida o no autorizada
- **WHEN** la API construye la respuesta de error
- **THEN** ofrece un resultado estable y accionable sin incluir información sensible
