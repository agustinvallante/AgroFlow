using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dsw2025Tpi.Application.Services;

public class JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
{
    public const string IngenioIdClaimType = "ingenioId";

    public string GenerateToken(string userId, string role, Guid ingenioId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("A stable user identity is required.", nameof(userId));
        }

        if (role is not ("OPERADOR" or "SUPERVISOR"))
        {
            throw new ArgumentException("The role is not approved for internal login.", nameof(role));
        }

        if (ingenioId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty ingenio identity is required.", nameof(ingenioId));
        }

        logger.LogInformation("Generating JWT for internal user {UserId} with role {Role}", userId, role);

        var jwtConfig = config.GetSection("Jwt");
        var keyText = jwtConfig["Key"] ?? throw new ArgumentNullException("Jwt Key");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role),
            new(IngenioIdClaimType, ingenioId.ToString("D"))
        };

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtConfig["ExpiresInMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
