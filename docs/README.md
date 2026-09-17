# Documentación de AgroFlow

Este directorio es la entrada humana a la documentación. Los requisitos normativos y escenarios verificables viven en [`../openspec/specs/`](../openspec/specs/); aquí se conserva el contexto necesario para entenderlos y aplicarlos.

## Orden de lectura recomendado

1. [Visión y alcance del MVP](product/vision-and-scope.md)
2. [Guía de aceptación del MVP](product/mvp-acceptance.md)
3. [Glosario del dominio](domain/glossary.md)
4. [Catálogo de casos de uso](product/use-case-catalog.md)
5. [Matriz de reglas de negocio](product/business-rules.md)
6. [Contexto del sistema](architecture/system-context.md)
7. [Decisiones abiertas](planning/open-decisions.md)
8. [Hoja de ruta](planning/implementation-roadmap.md)
9. [Flujo de trabajo](development/team-workflow.md)
10. [Definición de terminado](development/definition-of-done.md)

## Contenido

- `product/`: problema, alcance, actores, casos de uso y trazabilidad con las fuentes académicas.
- `domain/`: lenguaje compartido y conceptos del negocio.
- `architecture/`: contexto, límites y registros de decisiones arquitectónicas.
- `contracts/`: contratos compartidos entre frontend, backend e integraciones.
- `backend/`: guías específicas del backend que no definen reglas de negocio.
- `frontend/`: guías específicas del frontend y experiencia de usuario.
- `development/`: incorporación, contribución, pruebas y calidad.
- `operations/`: despliegue, observabilidad, respaldo y continuidad.
- `planning/`: decisiones pendientes y secuencia de implementación.
- `reference/`: procedencia y estado de las fuentes originales.

## Jerarquía de autoridad

Si dos documentos se contradicen, se debe detener la implementación y resolver la diferencia en el mismo pull request. El orden de autoridad es:

1. especificaciones vigentes en `openspec/specs/`;
2. contratos técnicos aprobados en `docs/contracts/`;
3. ADR aceptados en `docs/architecture/decisions/`;
4. documentación explicativa y README;
5. documentos académicos de referencia.

El código o una pantalla existente no modifican por sí solos la especificación. Toda diferencia debe registrarse como un cambio OpenSpec.
