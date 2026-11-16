using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroFlow.Api.Contracts.Appointments;
using AgroFlow.Application.Services;
using AgroFlow.Data.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AgroFlow.Api.Security;

internal static class MvpJwtAuthentication
{
    public static void AddMvpJwt(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt");
        var key = jwt["Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32 ||
            string.IsNullOrWhiteSpace(jwt["Issuer"]) || string.IsNullOrWhiteSpace(jwt["Audience"]))
        {
            throw new InvalidOperationException("Configure Jwt:Key (al menos 32 bytes), Jwt:Issuer y Jwt:Audience para el MVP.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                NameClaimType = JwtRegisteredClaimNames.Sub,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = ValidateUserAsync,
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.Headers.WWWAuthenticate = "Bearer";
                    return AppointmentProblems.WriteAsync(context.HttpContext, 401, "UNAUTHENTICATED");
                },
                OnForbidden = context => AppointmentProblems.WriteAsync(context.HttpContext, 403, "FORBIDDEN")
            };
        });
    }

    private static async Task ValidateUserAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        var subjects = principal.FindAll(JwtRegisteredClaimNames.Sub).ToArray();
        var tenants = principal.FindAll(JwtTokenService.IngenioIdClaimType).ToArray();
        var roles = principal.FindAll(ClaimTypes.Role).ToArray();
        if (subjects.Length != 1 || string.IsNullOrWhiteSpace(subjects[0].Value) ||
            tenants.Length != 1 || !Guid.TryParse(tenants[0].Value, out var ingenioId) ||
            ingenioId == Guid.Empty || roles.Length != 1)
        {
            context.Fail("Identidad interna inválida.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AgroFlowUser>>();
        var user = await users.FindByIdAsync(subjects[0].Value);
        if (user is not { IsActive: true } || user.IngenioId != ingenioId)
        {
            context.Fail("Identidad interna inválida.");
            return;
        }

        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Count != 1 || currentRoles[0] != roles[0].Value)
            context.Fail("Identidad interna inválida.");
    }
}
