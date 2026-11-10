using AgroFlow.Api.Authorization;
using AgroFlow.Application.Dtos;
using AgroFlow.Application.Services;
using AgroFlow.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Login de usuarios internos del MVP.
/// </summary>
[ApiController]
[Route("/api/auth")]
public sealed class AuthenticateController : ControllerBase
{
    private readonly UserManager<AgroFlowUser> _userManager;
    private readonly SignInManager<AgroFlowUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthenticateController(
        UserManager<AgroFlowUser> userManager,
        SignInManager<AgroFlowUser> signInManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user is not { IsActive: true } || user.IngenioId == Guid.Empty)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!passwordCheck.Succeeded)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count != 1 || !AgroFlowRoles.ApprovedInternalLogin.Contains(roles[0]))
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var role = roles[0];

        return Ok(new
        {
            Token = _jwtTokenService.GenerateToken(user.Id, role, user.IngenioId),
            UserInfo = new
            {
                Email = user.Email,
                IdentityId = user.Id,
                Name = user.UserName,
                Role = role
            }
        });
    }
}
