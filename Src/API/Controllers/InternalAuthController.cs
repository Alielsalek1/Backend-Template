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
public class InternalAuthController(IInternalAuthService authService, IUserConfirmationService accountConfirmationService) : ControllerBase
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

    // [HttpPost("forget-password")]
    // public async Task<ActionResult<ForgetPasswordResponseDto>> ForgetPassword([FromBody] ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    // {
        
    // }

    // [HttpPost("reset-password")]
    // public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    // {
        
    // }
}
