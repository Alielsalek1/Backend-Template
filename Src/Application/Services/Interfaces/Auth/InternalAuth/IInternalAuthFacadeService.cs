using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Utils;
using Domain.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Interfaces;

public interface IInternalAuthFacadeService
{
    Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ForgetPasswordAsync(ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<RefreshTokenResponseDto>>> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest, string refreshTokenCookie, CancellationToken cancellationToken);
}
