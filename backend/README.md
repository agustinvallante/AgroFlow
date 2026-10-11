# Backend de AgroFlow

Base técnica en .NET 8 reutilizada de [ICS2026-backend](https://github.com/agustinvallante/ICS2026-backend).

La solución `AgroFlow.sln` tiene cinco proyectos (`Api`, `Application`, `Domain`, `Data` y `Tests`) en .NET 8. La demo local de turnos utiliza SQLite y las migraciones de `AgroFlowDbContext`. Los módulos heredados de productos, pedidos y clientes se retiraron en B01.

ASP.NET Core Identity/JWT y su migración histórica permanecen como base técnica **fuera** de `LocalDemo`; no constituyen todavía la autenticación ni los permisos por ingenio del MVP (B03). Ya no existe registro público ni carga de `admins.json`/`customers.json`. La migración de comercio electrónico no forma parte de AgroFlow; no se promete compatibilidad con esa base. Ver [inventario de migraciones](../docs/backend/legacy-migration-inventory.md).
La [ADR-006](../docs/architecture/decisions/ADR-006-sqlite-para-persistencia-del-mvp.md) elige SQLite para el MVP académico y PostgreSQL antes de la operación del primer cliente real. No vuelve seguro al perfil demo ni afirma validado WAL para el MVP. La [replanificación tras la demo](../docs/planning/mvp-replan-2026-09-29.md) especifica las brechas. Antes de iniciar sobre datos previos al renombre, aplicar la [transición no destructiva](../docs/development/local-demo-runbook.md#actualización-desde-la-demo-anterior-sin-perder-turnos).

Antes de modificar comportamiento, consultar:

- [especificaciones vigentes](../openspec/specs/);
- [documentación específica del backend](../docs/backend/README.md);
- [contratos compartidos](../docs/contracts/README.md);
- [decisiones abiertas](../docs/planning/open-decisions.md).

## Ejecutar una verificación

```powershell
dotnet restore AgroFlow.sln
dotnet build AgroFlow.sln --configuration Release
dotnet test AgroFlow.sln --configuration Release
```

Para levantar la demo local desde `backend/AgroFlow.Api`, sin SQL Server ni JWT:

```powershell
dotnet run --launch-profile http --urls http://localhost:5000
```

`appsettings.Development.json` activa `LocalDemo`; el arranque crea/migra SQLite y aplica el seed ficticio. Consultar el [runbook de la demo](../docs/development/local-demo-runbook.md). Fuera de ese perfil, el andamiaje Identity requiere `ConnectionStrings__AgroFlowIdentity`, `Jwt__Key` (mínimo 32 bytes), `Jwt__Issuer` y `Jwt__Audience`; no se entregan cuentas iniciales y ese flujo no es aún el MVP. Para una clave local no versionada:

```powershell
dotnet user-secrets --project AgroFlow.Api set "Jwt:Key" "una-clave-local-de-al-menos-32-caracteres"
```

No compartir secretos ni datos reales.
