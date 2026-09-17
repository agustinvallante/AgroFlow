# Plataforma y segregación

## Purpose

Define garantías transversales para que las capacidades de AgroFlow mantengan aislamiento, consistencia y una única fuente operativa de verdad.

## Requirements

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
