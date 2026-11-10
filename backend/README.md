# Backend de AgroFlow

Base técnica en .NET 8 reutilizada de [ICS2026-backend](https://github.com/agustinvallante/ICS2026-backend).

La solución `AgroFlow.sln` tiene cinco proyectos (`Api`, `Application`, `Domain`, `Data` y `Tests`) en .NET 8. La demo local de turnos utiliza SQLite y las migraciones de `AgroFlowDbContext`. Los módulos heredados de productos, pedidos y clientes se retiraron en B01.

El perfil MVP usa ASP.NET Core Identity y turnos en el mismo archivo SQLite. Las cuentas internas se vinculan a un ingenio y las lecturas protegidas usan JWT. El [inventario de migraciones](../docs/backend/legacy-migration-inventory.md) registra el retiro del contexto SQL Server y la continuidad del esquema de turnos.
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

`appsettings.Development.json` activa `LocalDemo`; el arranque crea/migra SQLite y aplica el seed ficticio. Consultar el [runbook de la demo](../docs/development/local-demo-runbook.md).

## Probar las lecturas internas del MVP

Desde la raíz del repositorio, en PowerShell 7:

```powershell
$env:LocalDemo__Enabled = 'false'
$env:Jwt__Key = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:MvpSeed__Password = Read-Host 'Contraseña para las cuentas ficticias' -MaskInput
dotnet run --project backend/AgroFlow.Api --no-launch-profile --urls http://localhost:5000
```

El archivo predeterminado es `agroflow-mvp.db`, relativo al directorio de la API. `ConnectionStrings__AgroFlowMvpDb` permite elegir un archivo SQLite distinto. Para ensayar con una base de la demo anterior, detener la API, preparar una copia respaldada y seleccionar explícitamente esa copia con ruta absoluta, `Mode=ReadWrite` y `Foreign Keys=True`. El arranque agrega tablas de Identity e índice de lectura, conservando los IDs de turnos. El login anterior de SQL Server requiere provisionar cuentas nuevas con ingenio en SQLite.

El seed académico se habilita con `MvpSeed__Password`, una contraseña de al menos ocho caracteres con mayúscula, minúscula, número y símbolo. Crea dos ingenios y las cuentas ficticias `operador-mvp-a`, `supervisor-mvp-a` y `operador-mvp-b`. Se aplica una vez por archivo y conserva los hashes y los datos en reinicios. Retirar la variable después de inicializar la base.

1. Iniciar sesión por `POST /api/auth/login` con `username` y `password`; usar el `token` de la respuesta como `Authorization: Bearer <token>`.
2. Consultar `GET /api/v1/appointments?date=2026-07-15` y usar un `id` de `items` en `GET /api/v1/appointments/{id}`. La fecha explícita coincide con la del seed ficticio.
3. Cambiar a la cuenta del otro ingenio y consultar el mismo ID. Debe responder `404 APPOINTMENT_NOT_FOUND`, igual que con un ID inexistente.

El login y las lecturas internas son el alcance del corte. El dashboard y el bot de la demo usan `LocalDemo`. [Contrato de lecturas](../docs/contracts/openapi.mvp-appointment-reads.yaml)
