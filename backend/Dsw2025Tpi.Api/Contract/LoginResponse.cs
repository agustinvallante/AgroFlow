namespace Dsw2025Tpi.Api.Contract;

public sealed record LoginResponse(string Token, LoginUserInfo UserInfo);

public sealed record LoginUserInfo(
    string? Email,
    string IdentityId,
    string? Name,
    string Role,
    Guid IngenioId);
