# ADR-002: Jerarquía documental y OpenSpec

- Estado: aceptada
- Fecha: 2026-09-17
- Alcance: fuentes de verdad y gestión de cambios

## Contexto

El proyecto dispone de documentos académicos, un prototipo visual, una base de backend y conversaciones de planificación. Estas fuentes no tienen el mismo grado de autoridad y pueden contener omisiones o contradicciones. Sin una jerarquía explícita, frontend, backend e integración podrían implementar interpretaciones diferentes.

## Decisión

Se adopta OpenSpec para describir el comportamiento vigente y gestionar sus cambios. La autoridad documental queda ordenada así:

1. `openspec/specs/`: comportamiento vigente, expresado como requisitos y escenarios verificables.
2. `docs/contracts/`: forma técnica exacta de interacción, incluido el contrato OpenAPI cuando se incorpore.
3. `docs/architecture/decisions/`: decisiones aceptadas, contexto y consecuencias.
4. `docs/product/`, `docs/domain/`, `docs/architecture/`, `docs/operations/` y guías por área: explicación, contexto y uso.
5. README: navegación e instrucciones rápidas; no duplican especificaciones.
6. PDF, diagramas del prototipo y otros antecedentes: evidencia histórica y material de relevamiento.

El código y las pruebas deben demostrar conformidad con esa documentación. Una diferencia detectada no convierte automáticamente al código existente en la nueva regla: se registra y se resuelve mediante el flujo de cambio.

## Flujo de cambio

1. Crear o enlazar un issue que identifique capacidad, caso de uso y resultado esperado.
2. Registrar la propuesta en `openspec/changes/<identificador>-<nombre>/`.
3. Resolver en `docs/planning/open-decisions.md` cualquier decisión que bloquee escenarios verificables.
4. Revisar el cambio entre las áreas afectadas.
5. Implementar contrato, backend, frontend, integración y pruebas según corresponda.
6. Al completar el cambio, consolidar el comportamiento aprobado en `openspec/specs/` y archivar la propuesta según el flujo de OpenSpec.

Las especificaciones se organizan por capacidad de negocio, no por tecnología. Por ejemplo, la asignación de un turno se define una vez aunque requiera trabajo en la API, la interfaz y n8n.

## Resolución de conflictos

- Si un PDF contradice una especificación vigente, prevalece la especificación y la discrepancia se registra con trazabilidad.
- Si el contrato contradice una especificación, se detiene el cambio y se corrige uno de los dos mediante una decisión explícita.
- Si el código contradice la especificación o el contrato, se considera deuda o defecto hasta que el equipo apruebe formalmente otro comportamiento.
- Si no existe una respuesta acordada, se registra como decisión abierta; no se completa el vacío con una suposición silenciosa.

## Consecuencias

### Beneficios

- una definición funcional compartida por todas las áreas;
- cambios revisables antes de invertir en implementación;
- trazabilidad entre antecedentes, escenarios, issues y pruebas;
- incorporación de nuevos integrantes sin depender de conocimiento oral.

### Costos

- cada cambio funcional requiere mantener especificación y contrato;
- se necesita disciplina para archivar propuestas y evitar documentos paralelos;
- las contradicciones deben resolverse antes de marcar una funcionalidad como terminada.

## Política para fuentes académicas

Los PDF originales no se publican automáticamente en un repositorio público, especialmente si contienen datos personales. Su procedencia y cobertura se documentan en [trazabilidad de fuentes](../../product/source-traceability.md). Una vez migrado un requisito a OpenSpec, el PDF deja de ser la fuente normativa de ese comportamiento.
