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
- Node.js y el administrador de paquetes que se adopte al importar el frontend;
- una configuración local de base de datos según la guía futura del backend.

Las credenciales, claves JWT y cadenas de conexión no se guardan en Git. Deben suministrarse mediante variables de entorno, secretos de usuario o un almacén aprobado.

## 3. Verificar el estado actual

El backend heredado puede comprobarse desde la raíz con:

```powershell
dotnet restore backend/Dsw2025Tpi.sln
dotnet build backend/Dsw2025Tpi.sln --configuration Release
```

El frontend todavía es un espacio preparado para integrar el prototipo. Sus comandos definitivos se documentarán en `docs/frontend/` al incorporar el código.

Las especificaciones se validan con:

```powershell
openspec validate --all --strict --no-interactive
```

## 4. Elegir una tarea

- Elegir un issue en [AgroFlow — Backend](https://github.com/users/agustinvallante/projects/2) o [AgroFlow — Frontend](https://github.com/users/agustinvallante/projects/1). Son dos tableros del mismo repositorio.
- Confirmar capacidad y criterios de aceptación.
- Revisar dependencias y decisiones abiertas.
- Para el backend, comenzar por el [contrato y las decisiones del recorrido MVP](https://github.com/agustinvallante/AgroFlow/issues/7); las rutas de los issues por endpoint son propuestas hasta su aprobación en OpenAPI.
- Seguir [el flujo del equipo](../development/team-workflow.md).

## 5. Pedir revisión

Usar la plantilla de pull request, incluir la evidencia de pruebas y comprobar la [definición de terminado](../development/definition-of-done.md). Los cambios de contrato o especificación deben revisarse de forma transversal.
