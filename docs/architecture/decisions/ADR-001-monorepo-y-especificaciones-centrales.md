# ADR-001: Monorepo y especificaciones centrales

- Estado: aceptada
- Fecha: 2026-09-17
- Alcance: organización del código y la colaboración

## Contexto

AgroFlow requiere un frontend web, un backend .NET y una integración mediante n8n. El equipo prevé que algunos integrantes trabajen principalmente en frontend y otros en backend. A la vez, ambos lados dependen de los mismos casos de uso, reglas y contratos.

Separar inmediatamente el trabajo en repositorios independientes permitiría historiales y permisos distintos, pero también duplicaría onboarding, decisiones, issues y referencias. Para el tamaño y la etapa actual del proyecto, la coordinación del contrato representa un costo mayor que la independencia de despliegue.

## Decisión

AgroFlow se mantiene como un monorepo con esta separación principal:

```text
AgroFlow/
├── backend/
├── frontend/
├── openspec/
├── docs/
└── .github/
```

- `backend/` y `frontend/` contienen implementaciones independientes dentro del mismo historial.
- `openspec/` contiene el comportamiento compartido y no pertenece exclusivamente a ninguno de los dos equipos.
- `docs/` contiene contexto, contratos, decisiones, operación y guías.
- La automatización de compilación y pruebas se separará por rutas para evitar ejecutar trabajo innecesario.

Cuando el equipo configure la gestión del trabajo, podrá usar dos GitHub Projects: uno para frontend y otro para backend. Esos Projects son tableros de planificación, no repositorios nuevos. Los issues seguirán viviendo en el mismo repositorio y una funcionalidad transversal tendrá una referencia común y tareas vinculadas por área.

## Consecuencias

### Beneficios

- una sola fuente de verdad funcional y arquitectónica;
- cambios de contrato y consumidores revisables en conjunto;
- onboarding, CI, convenciones y seguimiento centralizados;
- trazabilidad directa entre especificación, backend, frontend y pruebas;
- posibilidad de separar vistas y responsables sin duplicar requisitos.

### Costos

- todos los colaboradores con escritura pueden modificar ambas áreas;
- un repositorio contiene tecnologías y comandos diferentes;
- se requieren filtros de CI y propietarios por ruta;
- las revisiones deben evitar que un cambio de una sola área rompa el contrato compartido.

## Reglas derivadas

1. No se crean carpetas `backend/docs` o `frontend/docs` como fuentes documentales alternativas.
2. Las guías específicas se ubican en `docs/backend/` y `docs/frontend/`.
3. Los responsables de cada ruta se declararán mediante `CODEOWNERS` cuando el equipo asigne roles.
4. Los cambios transversales deben enlazar la misma capacidad y el mismo cambio OpenSpec.
5. La rama principal se protegerá y los cambios ingresarán por pull request con verificaciones por ruta.

## Cuándo reconsiderar

La separación en repositorios puede evaluarse si aparecen ciclos de release independientes, restricciones de acceso distintas, equipos autónomos de mayor tamaño o necesidades de despliegue que el monorepo no pueda resolver razonablemente. La preferencia personal o la existencia de dos tableros no es motivo suficiente por sí sola.
