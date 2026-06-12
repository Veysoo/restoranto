using Microsoft.AspNetCore.Mvc;
using RestaurantOS.Api.Models;
using RestaurantOS.Api.Services;
using RestaurantOS.Application.DTOs.Auth;
using RestaurantOS.Application.Interfaces;

namespace RestaurantOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwt;

    public AuthController(IAuthService authService, JwtTokenService jwt)
    {
        _authService = authService;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new LoginResponse
        {
            Token = _jwt.CreateToken(result),
            UserId = result.UserId,
            FullName = result.FullName,
            Username = result.Username,
            Role = result.Role
        });
    }
}
