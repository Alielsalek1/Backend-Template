using Application.DTOs.InternalAuth;
using Application.Utils;
using Domain.Shared;

namespace Application.Services.Interfaces;
public interface IInternalAuthService
{
    Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken);
}
