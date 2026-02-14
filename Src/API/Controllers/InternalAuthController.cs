using Microsoft.AspNetCore.Mvc;
using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Application.DTOs.User;
using Application.Utils;
using Asp.Versioning;
using API.Extensions;
using API.ActionFilters;

namespace API.Controllers;
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal-auth")]
public class InternalAuthController(IInternalAuthFacadeService authFacade) : ControllerBase
{
    private readonly IInternalAuthFacadeService _authFacade = authFacade;

    [HttpPost("register")]
    [Idempotent]
    [ProducesResponseType(typeof(SuccessApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto registerRequest,
        CancellationToken cancellationToken)
    {
        var result = await _authFacade.RegisterAsync(registerRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(SuccessApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto loginRequest, 
        CancellationToken cancellationToken)
    {
        var result = await _authFacade.LoginAsync(loginRequest, cancellationToken);
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
       var result = await _authFacade.ConfirmEmailAsync(confirmEmailRequest, cancellationToken);
       return this.ToActionResult(result);
    }

    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationEmail(
        [FromBody] ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, 
        CancellationToken cancellationToken)
    {
        var result = await _authFacade.ResendConfirmationEmailAsync(resendConfirmationEmailRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("forget-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    {
        var result = await _authFacade.ForgetPasswordAsync(forgetPasswordRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        var result = await _authFacade.ResetPasswordAsync(resetPasswordRequest, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(SuccessApiResponse<RefreshTokenResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequest, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
        var result = await _authFacade.RefreshTokenAsync(refreshTokenRequest, refreshToken, cancellationToken);
        return this.ToActionResult(result);
    }
}