using Dsw2025Tpi.Api.Contract;
using Dsw2025Tpi.Api.Authorization;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers;

[ApiController]
[Route("/api/auth")]
public sealed class AuthenticateController(
    UserManager<AgroFlowUser> userManager,
    SignInManager<AgroFlowUser> signInManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    private const string InvalidCredentialsMessage = "Credenciales inválidas.";

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(InvalidCredentialsMessage);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(InvalidCredentialsMessage);
        }

        var userRoles = await userManager.GetRolesAsync(user);
        var role = userRoles.Count == 1 ? userRoles[0] : null;
        if (string.IsNullOrWhiteSpace(user.Id) ||
            user.IngenioId is not Guid ingenioId ||
            ingenioId == Guid.Empty ||
            role is null ||
            !AgroFlowRoles.ApprovedInternalLogin.Contains(role, StringComparer.Ordinal))
        {
            return Unauthorized(InvalidCredentialsMessage);
        }

        var token = jwtTokenService.GenerateToken(user.Id, role, ingenioId);
        return Ok(new LoginResponse(
            token,
            new LoginUserInfo(user.Email, user.Id, user.UserName, role, ingenioId)));
    }
}
