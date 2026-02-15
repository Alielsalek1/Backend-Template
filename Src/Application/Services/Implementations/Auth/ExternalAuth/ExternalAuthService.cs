using System.Runtime.InteropServices;
using Application.Constants.ApiErrors;
using Application.DTOs.ExternalAuth;
using Application.DTOs.User;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Enums;
using Domain.Models;
using Domain.Models.User;
using Domain.Shared;
using Google.Apis.Auth;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class ExternalAuthService(
    IUserRepository userRepo,
    IJwtTokenProvider tokenProvider, 
    IConfiguration configuration,
    IGoogleAuthValidator googleAuthValidator,
    ILogger<ExternalAuthService> logger) 
    : IExternalAuthService
{
    private readonly IUserRepository _userRepository = userRepo;
    private readonly IJwtTokenProvider _tokenProvider = tokenProvider;
    private readonly IConfiguration _configuration = configuration;
    private readonly IGoogleAuthValidator _googleAuthValidator = googleAuthValidator;
    private readonly ILogger<ExternalAuthService> _logger = logger;

    public async Task<Result<SuccessApiResponse<GoogleAuthResponseDto>>> GoogleLoginAsync(GoogleAuthRequestDto authRequest, CancellationToken ct)
    {        
        var payloadResult = await ValidateAndGetGooglePayloadAsync(authRequest.IdToken);
        if (!payloadResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(payloadResult.Error);
        }
        var payload = payloadResult.Data;
        
        var user = await _userRepository.GetUserByEmailAsync(payload.Email, ct);
        
        var validationResult = ValidateGoogleLogin(payload, user);
        if (!validationResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(validationResult.Error);
        }

        if (user is null)
        {
            user = await CreateExternalUserAsync(payload, ct);
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
    private Result<User?> ValidateGoogleLogin(GoogleJsonWebSignature.Payload payload, User? user)
    {
        if (user != null && user.AuthScheme != AuthScheme.External)
        {
            _logger.LogWarning("User with email {Email} exists but is not an external auth user", payload.Email);
            return Result<User?>.Failure(UserErrors.EmailAlreadyExists);
        }

        return Result<User?>.Success(user);
    }
    private async Task<Result<GoogleJsonWebSignature.Payload>> ValidateAndGetGooglePayloadAsync(string idToken)
    {
        var googleClientId = _configuration["Google:ClientId"];
        _logger.LogInformation("Validating Google token against ClientId: {ClientId}", googleClientId ?? "NULL");
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
            return Result<GoogleJsonWebSignature.Payload>.Failure(AuthErrors.InvalidCredentials);
        }
    }
    private async Task<User> CreateExternalUserAsync(GoogleJsonWebSignature.Payload payload, CancellationToken ct)
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

        var user = new User(userCreationParams);
        user.MarkEmailAsVerified();

        await _userRepository.AddUserAsync(user, ct);
        return user;
    }

    public async Task<Result<SuccessApiResponse<GoogleAuthResponseDto>>> LinkGoogleAccountAsync(GoogleAuthRequestDto authRequest, Guid userId, CancellationToken ct)
    {        
        var payloadResult = await ValidateAndGetGooglePayloadAsync(authRequest.IdToken);
        if (!payloadResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(payloadResult.Error);
        }
        var payload = payloadResult.Data;

        var user = await _userRepository.GetUserByIdAsync(userId, ct);
        var validationResult = await ValidateGoogleAccountLinkingAsync(payload, user, ct);
        if (!validationResult.IsSuccess)
        {
            return Result<SuccessApiResponse<GoogleAuthResponseDto>>.Failure(validationResult.Error);
        }

        await UpdateExternalUser(user!, payload, ct);
        var accessToken = _tokenProvider.GenerateAccessToken(user!);
        _logger.LogInformation("Google account linked successfully for user {UserId}", user!.Id);

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
    private async Task<Result<User>> ValidateGoogleAccountLinkingAsync(GoogleJsonWebSignature.Payload payload, User? user, CancellationToken ct)
    {
        if (user == null)
        {
            _logger.LogWarning("Cannot link Google account: User not found");
            return Result<User>.Failure(UserErrors.UserNotFound);
        }
        if (!user.IsGuest())
        {
            _logger.LogWarning("Cannot link Google account: User {UserId} is not a guest user", user.Id);
            return Result<User>.Failure(UserErrors.UserIsNotGuest);
        }
        if (await _userRepository.IsEmailInUseAsync(payload.Email, ct) == true)
        {
            _logger.LogWarning("Cannot link Google account: Email {Email} is already in use", payload.Email);
            return Result<User>.Failure(UserErrors.EmailAlreadyExists);
        }
        return Result<User>.Success(user);
    }
    private async Task UpdateExternalUser(User user, GoogleJsonWebSignature.Payload payload, CancellationToken ct)
    {
        user.PromoteGuestToExternalUser(payload.Email, payload.Email.Split('@')[0]);
        await _userRepository.UpdateUserAsync(user, ct);
    }
}