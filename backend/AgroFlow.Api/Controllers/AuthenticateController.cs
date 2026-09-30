using AgroFlow.Application.Dtos;
using AgroFlow.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AgroFlow.Api.Controllers;

/// <summary>
/// Base de login de Identity, aislada del perfil de demo. La identidad por
/// ingenio y los roles del MVP se implementarán en B03.
/// </summary>
[ApiController]
[Route("/api/auth")]
public sealed class AuthenticateController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthenticateController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
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
        if (user is null)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!passwordCheck.Succeeded)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();
        if (string.IsNullOrEmpty(role))
        {
            return Unauthorized("Credenciales inválidas.");
        }

        return Ok(new
        {
            Token = _jwtTokenService.GenerateToken(user.UserName!, role),
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
