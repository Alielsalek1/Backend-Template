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
    IUserConfirmationService accountConfirmationService,
    ILogger<InternalAuthController> logger) : ControllerBase
{
    private readonly IInternalAuthService _authService = authService;
    private readonly IUserConfirmationService _accountConfirmationService = accountConfirmationService;
    private readonly ILogger<InternalAuthController> _logger = logger;

    [HttpPost("register")]
    [Idempotent]
    [ProducesResponseType(typeof(SuccessApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto registerRequest,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing registration request for email: {Email}", registerRequest.Email);
        var result = await _authService.RegisterAsync(registerRequest, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning("Registration failed for {Email}: {Message}", registerRequest.Email, result.Error.message);
        }
        else
        {
            _logger.LogInformation("User registered successfully: {Email}", registerRequest.Email);
        }
        return this.ToActionResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(SuccessApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto loginRequest, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login request for: {UsernameOrEmail}", loginRequest.UsernameOrEmail);
        var result = await _authService.LoginAsync(loginRequest, cancellationToken);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Login successful for user: {UserId}", result.Data.Data.UserId);
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

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequestDto confirmEmailRequest,
        CancellationToken cancellationToken)
    {
       _logger.LogInformation("Processing email confirmation request for {Email}", confirmEmailRequest.Email);
       var result = await _accountConfirmationService.ConfirmEmailAsync(confirmEmailRequest, cancellationToken);
       if (result.IsFailure)
       {
           _logger.LogWarning("Email confirmation failed for {Email}: {Message}", confirmEmailRequest.Email, result.Error.message);
       }
       else
       {
           _logger.LogInformation("Email confirmed successfully for {Email}", confirmEmailRequest.Email);
       }
       return this.ToActionResult(result);
    }

    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationEmail(
        [FromBody] ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing resend confirmation email request for {Email}", resendConfirmationEmailRequest.Email);
        var result = await _accountConfirmationService.ResendConfirmationEmailAsync(resendConfirmationEmailRequest, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning("Resend confirmation failed for {Email}: {Message}", resendConfirmationEmailRequest.Email, result.Error.message);
        }
        else
        {
            _logger.LogInformation("Confirmation email resent for {Email}", resendConfirmationEmailRequest.Email);
        }
        return this.ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(SuccessApiResponse<RefreshTokenResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing token refresh request for user {UserId}", refreshTokenRequest.UserId);
        var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
        
        // Handle missing refresh token cookie
        if (string.IsNullOrEmpty(refreshToken) || !Guid.TryParse(refreshToken, out var parsedRefreshToken))
        {
            _logger.LogWarning("Refresh token cookie missing or invalid for user {UserId}", refreshTokenRequest.UserId);
            parsedRefreshToken = Guid.Empty;
        }
        
        var result = await _authService.RefreshTokenAsync(refreshTokenRequest.UserId, parsedRefreshToken, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning("Token refresh failed for user {UserId}: {Message}", refreshTokenRequest.UserId, result.Error.message);
        }
        else
        {
            _logger.LogInformation("Token refreshed successfully for user {UserId}", refreshTokenRequest.UserId);
        }
        return this.ToActionResult(result);
    }

    [HttpPost("forget-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing forget password request for {Email}", forgetPasswordRequest.Email);
        var result = await _authService.ForgetPasswordAsync(forgetPasswordRequest, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning("Forget password failed for {Email}: {Message}", forgetPasswordRequest.Email, result.Error.message);
        }
        else
        {
            _logger.LogInformation("Forget password email sent for {Email}", forgetPasswordRequest.Email);
        }
        return this.ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(SuccessApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing reset password request for {Email}", resetPasswordRequest.Email);
        var result = await _authService.ResetPasswordAsync(resetPasswordRequest, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning("Reset password failed for {Email}: {Message}", resetPasswordRequest.Email, result.Error.message);
        }
        else
        {
            _logger.LogInformation("Password reset successfully for {Email}", resetPasswordRequest.Email);
        }
        return this.ToActionResult(result);
    }
}