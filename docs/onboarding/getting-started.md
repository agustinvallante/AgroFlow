# Incorporación al proyecto

## 1. Entender el producto

Leer en este orden:

1. [Visión y alcance](../product/vision-and-scope.md)
2. [Glosario](../domain/glossary.md)
3. [Catálogo de casos de uso](../product/use-case-catalog.md)
4. [Reglas de negocio y trazabilidad](../product/business-rules.md)
5. [Contexto del sistema](../architecture/system-context.md)
6. [Decisiones abiertas](../planning/open-decisions.md)
7. la capacidad relevante en [`../../openspec/specs/`](../../openspec/specs/)
8. [Replanificación tras la demo](../planning/mvp-replan-2026-09-29.md) para no confundir código integrado con puertas de aceptación cumplidas.

## 2. Preparar el entorno

Requisitos actuales:

- Git;
- .NET SDK 8 para el backend;
- Node.js 22 y npm para el frontend React/Vite existente;
- almacenamiento local para SQLite: la [demo local](../development/local-demo-runbook.md) lo inicializa automáticamente; el perfil SQLite **MVP** elegido en [ADR-006](../architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) aún debe implementarse y validarse. No usar `LocalDemo` como sustituto del perfil seguro del MVP.

Las credenciales, claves JWT y cadenas de conexión no se guardan en Git. Deben suministrarse mediante variables de entorno, secretos de usuario o un almacén aprobado.

## 3. Verificar el estado actual

Desde la raíz del repositorio, comprobar la solución backend y sus pruebas con:

```powershell
dotnet restore backend/Dsw2025Tpi.sln
dotnet build backend/Dsw2025Tpi.sln --configuration Release
dotnet test backend/Dsw2025Tpi.sln --configuration Release --no-build
```

Comprobar el frontend existente con:

```powershell
cd frontend
npm ci
npm run typecheck
npm run lint
npm test
npm run build
```

Para ejecutar los recorridos locales, seguir el [runbook de la demo](../development/local-demo-runbook.md) y el [README del frontend](../../frontend/README.md). Esa ejecución no demuestra todavía seguridad ni aceptación del MVP.

Las especificaciones se validan con:

```powershell
openspec validate --all --strict --no-interactive
```

## 4. Elegir una tarea

- Elegir un issue en [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2) o [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1). Son dos tableros del mismo repositorio.
- Confirmar capacidad y criterios de aceptación.
- Revisar dependencias y decisiones abiertas.
- Para el backend, comenzar por [B00 y las decisiones del recorrido MVP](https://github.com/agustinvallante/AgroFlow/issues/7); reutilizar las rutas de turnos ya publicadas en OpenAPI y no suponer vigentes las rutas históricas de títulos de issues.
- Para frontend, partir de React/Vite conforme a [ADR-005](../architecture/decisions/ADR-005-continuidad-react-vite-para-el-mvp.md), no de la propuesta Next.js aún no integrada.
- Seguir [el flujo del equipo](../development/team-workflow.md).

## 5. Pedir revisión

Usar la plantilla de pull request, incluir la evidencia de pruebas y comprobar la [definición de terminado](../development/definition-of-done.md). Los cambios de contrato o especificación deben revisarse de forma transversal.
