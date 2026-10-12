using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AgroFlow.Application.Services;

public sealed class JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
{
    public const string IngenioIdClaimType = "ingenioId";

    public string GenerateToken(string userId, string role)
    {
        ValidateIdentityAndRole(userId, role);
        return CreateToken(userId, role, null);
    }

    public string GenerateToken(string userId, string role, Guid ingenioId)
    {
        ValidateIdentityAndRole(userId, role);
        if (ingenioId == Guid.Empty)
            throw new ArgumentException("A non-empty ingenio identity is required.", nameof(ingenioId));
        return CreateToken(userId, role, ingenioId);
    }

    private static void ValidateIdentityAndRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A stable user identity is required.", nameof(userId));
        if (role is not ("OPERADOR" or "SUPERVISOR"))
            throw new ArgumentException("The role is not approved for internal login.", nameof(role));
    }

    private string CreateToken(string userId, string role, Guid? ingenioId)
    {
        var jwtConfig = config.GetSection("Jwt");
        var keyText = jwtConfig["Key"];
        if (string.IsNullOrWhiteSpace(keyText) || Encoding.UTF8.GetByteCount(keyText) < 32)
            throw new InvalidOperationException("JWT signing key must contain at least 32 bytes.");
        var issuer = jwtConfig["Issuer"];
        var audience = jwtConfig["Audience"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT issuer and audience must be configured.");

        logger.LogInformation("Generating JWT for internal user {UserId} with role {Role}", userId, role);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };
        if (ingenioId.HasValue)
            claims.Add(new Claim(IngenioIdClaimType, ingenioId.Value.ToString("D")));
        var expiryText = jwtConfig["ExpiresInMinutes"];
        var expiryMinutes = string.IsNullOrWhiteSpace(expiryText) ? 60 : double.Parse(expiryText);
        if (expiryMinutes <= 0) throw new InvalidOperationException("JWT expiration must be positive.");

        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
