using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Implementations;

public class InternalAuthFacadeService(
    IInternalAuthService internalAuthService,
    IUserConfirmationService userConfirmationService,
    IPasswordResetService passwordResetService) : IInternalAuthFacadeService
{
    private readonly IInternalAuthService _internalAuthService = internalAuthService;
    private readonly IUserConfirmationService _userConfirmationService = userConfirmationService;
    private readonly IPasswordResetService _passwordResetService = passwordResetService;

    public Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken)
        => _internalAuthService.RegisterAsync(registerRequest, cancellationToken);

    public Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken)
        => _internalAuthService.LoginAsync(loginRequest, cancellationToken);

    public Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
        => _userConfirmationService.ConfirmEmailAsync(confirmEmailRequest, cancellationToken);

    public Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken)
        => _userConfirmationService.ResendConfirmationEmailAsync(resendConfirmationEmailRequest, cancellationToken);

    public Task<Result<SuccessApiResponse>> ForgetPasswordAsync(ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
        => _passwordResetService.ForgetPasswordAsync(forgetPasswordRequest, cancellationToken);

    public Task<Result<SuccessApiResponse>> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
        => _passwordResetService.ResetPasswordAsync(resetPasswordRequest, cancellationToken);

    public Task<Result<SuccessApiResponse<RefreshTokenResponseDto>>> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest, string refreshTokenCookie, CancellationToken cancellationToken)
        => _internalAuthService.RefreshTokenAsync(refreshTokenRequest.UserId, Guid.TryParse(refreshTokenCookie, out var parsed) ? parsed : Guid.Empty, cancellationToken);
}