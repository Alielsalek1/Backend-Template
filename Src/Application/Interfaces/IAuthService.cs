using Application.DTOs.InternalAuth;
using Application.Utils;
using Domain.Shared;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken);
}
