using Microsoft.AspNetCore.Mvc;
using Application.DTOs.InternalAuth;
using Application.Services;
using Application.Utils;
using Asp.Versioning;
using API.Extensions;
using API.ActionFilters;
using Application.Services.Interfaces;

namespace API.Controllers;
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal-auth")]
public class InternalAuthController(
    IInternalAuthService authService, 
    IUserConfirmationService accountConfirmationService
    ) : ControllerBase
{
    private readonly IInternalAuthService _authService = authService;
    private readonly IUserConfirmationService _accountConfirmationService = accountConfirmationService;

    [HttpPost("register")]
    [Idempotent]
    [ProducesResponseType(typeof(SuccessApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto registerRequest,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(registerRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(SuccessApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto loginRequest, 
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(loginRequest, cancellationToken);
        if (result.IsSuccess)
        {
            var refreshToken = result.Data.Data.RefreshToken;
            this.AddRefreshTokenCookie(refreshToken);
        }
        return this.ToActionResult(result);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequestDto confirmEmailRequest,
        CancellationToken cancellationToken)
    {
       var result = await _accountConfirmationService.ConfirmEmailAsync(confirmEmailRequest, cancellationToken);
       return this.ToActionResult(result);
    }

    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationEmail(
        [FromBody] ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, 
        CancellationToken cancellationToken)
    {
        var result = await _accountConfirmationService.ResendConfirmationEmailAsync(resendConfirmationEmailRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(SuccessApiResponse<RefreshTokenResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequest, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
        _ = Guid.TryParse(refreshToken, out var parsedRefreshToken);
        var result = await _authService.RefreshTokenAsync(refreshTokenRequest.UserId, parsedRefreshToken, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("forget-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgetPasswordAsync(forgetPasswordRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(resetPasswordRequest, cancellationToken);
        return this.ToActionResult(result);
    }
}