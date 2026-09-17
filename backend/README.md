# Backend de AgroFlow

Base técnica en .NET 8 reutilizada de [ICS2026-backend](https://github.com/agustinvallante/ICS2026-backend).

La solución actual aporta una arquitectura por capas, Entity Framework Core, SQL Server, ASP.NET Core Identity, autenticación JWT, Swagger, CORS, health checks y manejo centralizado de errores. Los modelos de comercio electrónico (`Product`, `Order`, `Customer`) son temporales: servirán como referencia de implementación, pero deberán reemplazarse por el dominio de AgroFlow.

Antes de modificar comportamiento, consultar:

- [especificaciones vigentes](../openspec/specs/);
- [documentación específica del backend](../docs/backend/README.md);
- [contratos compartidos](../docs/contracts/README.md);
- [decisiones abiertas](../docs/planning/open-decisions.md).

## Ejecutar una verificación

```powershell
dotnet restore Dsw2025Tpi.sln
dotnet build Dsw2025Tpi.sln
```

Antes de ejecutar la API fuera del entorno Development, configurar como mínimo:

- `ConnectionStrings__Dsw2025TpiEntities`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

Para configurar la clave local sin versionarla:

```powershell
dotnet user-secrets --project Dsw2025Tpi.Api set "Jwt:Key" "una-clave-local-de-al-menos-32-caracteres"
```

No compartir secretos ni archivos locales de administradores.
