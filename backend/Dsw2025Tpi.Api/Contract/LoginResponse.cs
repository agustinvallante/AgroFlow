using System.Text.Json.Serialization;

namespace Dsw2025Tpi.Api.Contract;

public sealed record LoginResponse(string Token, LoginUserInfo UserInfo);

public sealed record LoginUserInfo(
    string? Email,
    string IdentityId,
    Guid? CustomerId,
    string? Name,
    string Role,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? IngenioId);
