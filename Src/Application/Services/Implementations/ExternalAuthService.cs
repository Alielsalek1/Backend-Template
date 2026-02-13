using Application.Constants.Errors;
using Application.DTOs.ExternalAuth;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Enums;
using Domain.Models;
using Domain.Models.Users;
using Domain.Shared;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class ExternalAuthService(
    IUserRepository userRepo,
    ITokenProvider tokenProvider, 
    IConfiguration configuration,
    IGoogleAuthValidator googleAuthValidator,
    ILogger<ExternalAuthService> logger) 
    : IExternalAuthService
{
    private readonly IUserRepository _userRepository = userRepo;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IConfiguration _configuration = configuration;
    private readonly IGoogleAuthValidator _googleAuthValidator = googleAuthValidator;
    private readonly ILogger<ExternalAuthService> _logger = logger;

    public async Task<Result<SuccessApiResponse<GoogleAuthResponseDto>>> GoogleLoginAsync(GoogleAuthRequestDto authRequest, CancellationToken ct)
    {
        _logger.LogInformation("Attempting Google login");
        GoogleJsonWebSignature.Payload payload;
        var _googleClientId = _configuration["Google:ClientId"];
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId } 
            };
            
            payload = await _googleAuthValidator.ValidateAsync(authRequest.IdToken, settings);
            _logger.LogInformation("Google token validated for email: {Email}", payload.Email);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(ExternalAuthErrors.InvalidCredentials);
        }

        var user = await _userRepository.GetUserByEmailAsync(payload.Email, ct);

        if (user != null && user.AuthScheme != AuthScheme.External)
        {
            _logger.LogWarning("User with email {Email} exists but is not an external auth user", payload.Email);
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(ExternalAuthErrors.EmailAlreadyInUse);
        }

        if (user is null)
        {
            _logger.LogInformation("Creating new external user for email: {Email}", payload.Email);
            UserCreationParams userCreationParams = new()
            {
                Email = payload.Email,
                Username = payload.Email.Split('@')[0], // Simple username generation, you might want to improve this
                PasswordHash = new string('0', 60), // Dummy hash of correct length
                Role = Roles.User,
                AuthScheme = AuthScheme.External
            };
            user = new User(userCreationParams)
            {
                IsEmailVerified = true // Since Google has already verified their email
            };

            await _userRepository.AddUserAsync(user, ct);
        }
        else
        {
            _logger.LogInformation("Existing user found for email: {Email}", payload.Email);
        }

        // 4. Generate YOUR custom JWT and Refresh Token
        _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);
        var accessToken = _tokenProvider.GenerateAccessToken(user);

        return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Success(new SuccessApiResponse<GoogleAuthResponseDto>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Google authentication successful.",
            Data = new GoogleAuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken.ToString(),
                UserId = user.Id
            }
        });
    }
}