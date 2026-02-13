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
        
        var payloadResult = await ValidateAndGetGooglePayloadAsync(authRequest.IdToken);
        if (!payloadResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(payloadResult.Error);
        }
        var payload = payloadResult.Data;

        var user = await _userRepository.GetUserByEmailAsync(payload.Email, ct);
        var validationResult = ValidateExternalUser(user, payload.Email);
        if (!validationResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(validationResult.Error);
        }

        if (user is null)
        {
            user = await CreateAndAddExternalUserAsync(payload, ct);
        }
        else
        {
            _logger.LogInformation("Existing user found for email: {Email}", payload.Email);
        }

        var accessToken = _tokenProvider.GenerateAccessToken(user);
        _logger.LogInformation("Google login successful for user {UserId}", user.Id);

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
    private async Task<Result<GoogleJsonWebSignature.Payload>> ValidateAndGetGooglePayloadAsync(string idToken)
    {
        var googleClientId = _configuration["Google:ClientId"];
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleClientId] 
            };
            
            var payload = await _googleAuthValidator.ValidateAsync(idToken, settings);
            _logger.LogInformation("Google token validated for email: {Email}", payload.Email);
            return Result<GoogleJsonWebSignature.Payload>.Success(payload);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
            return Result<GoogleJsonWebSignature.Payload>.Failure(ExternalAuthErrors.InvalidCredentials);
        }
    }
    private Result<SuccessApiResponse> ValidateExternalUser(User? user, string email)
    {
        if (user != null && user.AuthScheme != AuthScheme.External)
        {
            _logger.LogWarning("User with email {Email} exists but is not an external auth user", email);
            return Result<SuccessApiResponse>.Failure(ExternalAuthErrors.EmailAlreadyInUse);
        }
        return Result<SuccessApiResponse>.Success(default!);
    }
    private async Task<User> CreateAndAddExternalUserAsync(GoogleJsonWebSignature.Payload payload, CancellationToken ct)
    {
        _logger.LogInformation("Creating new external user for email: {Email}", payload.Email);
        
        var userCreationParams = new UserCreationParams
        {
            Email = payload.Email,
            Username = payload.Email.Split('@')[0],
            PasswordHash = new string('0', 60),
            Role = Roles.User,
            AuthScheme = AuthScheme.External
        };

        var user = new User(userCreationParams)
        {
            IsEmailVerified = true
        };

        await _userRepository.AddUserAsync(user, ct);
        return user;
    }
}