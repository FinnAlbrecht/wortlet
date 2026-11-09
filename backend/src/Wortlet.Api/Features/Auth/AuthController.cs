using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wortlet.Application.Auth;

namespace Wortlet.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimits.Auth)]
    public Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct) => auth.RegisterAsync(request, ct);

    [HttpPost("login")]
    [EnableRateLimiting(RateLimits.Auth)]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    [Authorize]
    [HttpGet("me")]
    public Task<UserDto> Me(CancellationToken ct) => auth.MeAsync(ct);
}
