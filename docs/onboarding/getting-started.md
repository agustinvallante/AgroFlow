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

## 2. Preparar el entorno

Requisitos actuales:

- Git;
- .NET SDK 8 para el backend;
- Node.js 22 y npm para el frontend actual;
- permiso de escritura para la base SQLite de la demo local.

Las credenciales, claves JWT y cadenas de conexión no se guardan en Git. Deben suministrarse mediante variables de entorno, secretos de usuario o un almacén aprobado.

Si ya tenés una base de la demo anterior, seguí la [transición no destructiva](../development/local-demo-runbook.md#actualización-desde-la-demo-anterior-sin-perder-turnos) antes de iniciar la API renombrada. Git no mueve la SQLite ignorada: el arranque exige una ruta absoluta explícita para reutilizarla y conservar turnos.

## 3. Verificar el estado actual

El backend puede comprobarse desde la raíz con:

```powershell
dotnet restore backend/AgroFlow.sln
dotnet build backend/AgroFlow.sln --configuration Release
dotnet test backend/AgroFlow.sln --configuration Release
```

El frontend React/Vite ya está integrado; sus comandos se documentan en `docs/frontend/` y `frontend/package.json`.

Las especificaciones se validan con:

```powershell
npx --yes @fission-ai/openspec@1.13.1 validate --all --strict --no-interactive
```

## 4. Elegir una tarea

- Elegir un issue en [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2) o [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1). Son dos tableros del mismo repositorio.
- Confirmar capacidad y criterios de aceptación.
- Revisar dependencias y decisiones abiertas.
- Para el backend, comenzar por el [contrato y las decisiones del recorrido MVP](https://github.com/agustinvallante/AgroFlow/issues/7); las rutas de los issues por endpoint son propuestas hasta su aprobación en OpenAPI.
- Seguir [el flujo del equipo](../development/team-workflow.md).

## 5. Pedir revisión

Usar la plantilla de pull request, incluir la evidencia de pruebas y comprobar la [definición de terminado](../development/definition-of-done.md). Los cambios de contrato o especificación deben revisarse de forma transversal.
