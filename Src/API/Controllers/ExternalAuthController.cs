using API.Extensions;
using Application.DTOs.ExternalAuth;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/external-auth")]
public class ExternalAuthController(IExternalAuthService authService, ILogger<ExternalAuthController> logger) : ControllerBase
{
    private readonly IExternalAuthService _authService = authService;
    private readonly ILogger<ExternalAuthController> _logger = logger;

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequestDto authRequest, CancellationToken ct)
    {
        _logger.LogInformation("Processing Google login request");
        var result = await _authService.GoogleLoginAsync(authRequest, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Google login successful for user {UserId}", result.Data.Data.UserId);
            var refreshToken = result.Data.Data.RefreshToken;
            Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                }
            );
        }

        return this.ToActionResult(result);
    }
}