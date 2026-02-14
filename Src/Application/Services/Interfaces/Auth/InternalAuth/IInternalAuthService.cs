using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Utils;
using Domain.Shared;

namespace Application.Services.Interfaces;
public interface IInternalAuthService
{
    Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse<RefreshTokenResponseDto>>> RefreshTokenAsync(Guid userId, Guid refreshToken, CancellationToken cancellationToken);
}
