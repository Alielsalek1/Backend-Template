using Application.DTOs.InternalAuth;
using Application.Interfaces;
using Application.Utils;
using Domain.Shared;

namespace Application.Services;

public class AuthService : IAuthService
{
    public async Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken)
    {
        return Result<SuccessApiResponse<RegisterResponseDto>>.Success(new SuccessApiResponse<RegisterResponseDto>()
        {
            StatusCode = 201,
            Message = "User registered successfully",
            Data = new RegisterResponseDto(),        
        });
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
    }

    public async Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken)
    {
        return Result<SuccessApiResponse<LoginResponseDto>>.Success(new SuccessApiResponse<LoginResponseDto>());
    }

    public async Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
    {

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse());
    }

    public async Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken)
    {

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse());
    }
}
