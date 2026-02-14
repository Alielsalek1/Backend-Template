using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Services.Interfaces;
using Domain.Models.User;
using Domain.Shared;
using Domain.Enums;
using Application.Constants.ApiErrors;
using Mapster;
using Application.Utils;
using Application.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class InternalAuthService(
        IUserRepository userRepository,
        IEmailService emailService,
        IJwtTokenProvider tokenProvider,
        ILogger<InternalAuthService> logger,
        ConfirmationTokenCacheService tokenCacheService
    ) : IInternalAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly IJwtTokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<InternalAuthService> _logger = logger;
    private readonly ConfirmationTokenCacheService _tokenCacheService = tokenCacheService;

    public async Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken)
    {
        var user = CreateUser(registerRequest);
        var uniquenessResult = await ValidateUserIdentifiersUniquenessAsync(user, cancellationToken);
        if (!uniquenessResult.IsSuccess)
        {
            return uniquenessResult;
        }
        _logger.LogInformation("User identifiers are unique for email: {Email}, username: {Username}", user.Email, user.Username);

        await _userRepository.AddUserAsync(user, cancellationToken);
        _logger.LogInformation("User created successfully with email: {Email}", user.Email);

        var confirmationToken = ConfirmationTokenCacheService.GenerateRandomToken();
        await _tokenCacheService.SetTokenAsync(user.Email, confirmationToken, cancellationToken);
        await _emailService.SendConfirmationEmailAsync(user.Email, confirmationToken, cancellationToken);
        _logger.LogInformation("Confirmation email sent successfully to {Email}", user.Email);

        return Result<SuccessApiResponse<RegisterResponseDto>>.Success(new SuccessApiResponse<RegisterResponseDto>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "User registered successfully. Please check your email for the confirmation code.",
            Data = new RegisterResponseDto
            {
                UserId = user.Id
            }
        });
    }
    private static User CreateUser(RegisterRequestDto registerRequest)
    {
        // map using Mapster
        var userCreationParams = registerRequest.Adapt<UserCreationParams>();
        userCreationParams = userCreationParams with 
        { 
            PasswordHash = HashPassword(registerRequest.Password),
            AuthScheme = AuthScheme.Internal
        };
        var user = new User(userCreationParams)
        {
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30)
        };
        return user;
    }
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
    }
    private async Task<Result<SuccessApiResponse<RegisterResponseDto>>> ValidateUserIdentifiersUniquenessAsync(User user, CancellationToken cancellationToken)
    {
        if (await IsEmailInUseAsync(user.Email, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Email {Email} already in use", user.Email);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(UserErrors.EmailAlreadyExists);
        }
        if (await IsUsernameInUseAsync(user.Username, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Username {Username} already in use", user.Username);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(UserErrors.UsernameAlreadyExists);
        }
        if (!string.IsNullOrEmpty(user.PhoneNumber) && await IsPhoneNumberInUseAsync(user.PhoneNumber, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Phone number {PhoneNumber} already in use", user.PhoneNumber);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(UserErrors.PhoneNumberAlreadyExists);
        }
        return Result<SuccessApiResponse<RegisterResponseDto>>.Success(default!);
    }
    private async Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken)
    {
        return await _userRepository.IsEmailInUseAsync(email, cancellationToken);
    }
    private async Task<bool> IsUsernameInUseAsync(string username, CancellationToken cancellationToken)
    {
        return await _userRepository.IsUsernameInUseAsync(username, cancellationToken);
    }
    private async Task<bool> IsPhoneNumberInUseAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        return await _userRepository.IsPhoneNumberInUseAsync(phoneNumber, cancellationToken);
    }

    public async Task<Result<SuccessApiResponse<LoginResponseDto>>> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken)
    {
        var user = await GetUserByEmailOrUsernameAsync(loginRequest.UsernameOrEmail, cancellationToken);
        var validationResult = ValidateLoginRequest(user, loginRequest);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("User {UsernameOrEmail} passed validation checks, proceeding with login", loginRequest.UsernameOrEmail);

        var accessToken = _tokenProvider.GenerateAccessToken(user!);
        _logger.LogInformation("User {UsernameOrEmail} logged in successfully", loginRequest.UsernameOrEmail);

        return Result<SuccessApiResponse<LoginResponseDto>>.Success(new SuccessApiResponse<LoginResponseDto>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Login successful.",
            Data = new LoginResponseDto
            {
                UserId = user!.Id,
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken.ToString()
            }
        });
    }
    private async Task<User?> GetUserByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken)
    {
        var userByEmailAsync = await _userRepository.GetUserByEmailAsync(emailOrUsername, cancellationToken);
        if (userByEmailAsync != null)
        {
            return userByEmailAsync;
        }
        var userByUsernameAsync = await _userRepository.GetUserByUsernameAsync(emailOrUsername, cancellationToken);
        return userByUsernameAsync;
    }
    private Result<SuccessApiResponse<LoginResponseDto>> ValidateLoginRequest(User? user, LoginRequestDto loginRequest)
    {
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} not found", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.InvalidCredentials);
        }
        if (IsExternalAuthScheme(user.AuthScheme))
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} is trying to login with wrong auth scheme: {AuthScheme}", loginRequest.UsernameOrEmail, user.AuthScheme);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.WrongAuthScheme);
        }
        if (!user.IsEmailVerified)
        {
            _logger.LogWarning("Login failed: Email not verified for user {UsernameOrEmail}", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.EmailNotConfirmed);
        }
        if (IsPasswordIncorrect(loginRequest.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {UsernameOrEmail}", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.InvalidCredentials);
        }
        return Result<SuccessApiResponse<LoginResponseDto>>.Success(default!);
    }
    private static bool IsPasswordIncorrect(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash) == false;
    }
    private static bool IsExternalAuthScheme(AuthScheme authScheme)
    {
        return authScheme == AuthScheme.External;
    }

    public async Task<Result<SuccessApiResponse<RefreshTokenResponseDto>>> RefreshTokenAsync(Guid userId, Guid refreshToken, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        var existenceResult = ValidateUserExistence(user, userId);
        if (!existenceResult.IsSuccess)
        {
            return existenceResult;
        }
        var validationResult = ValidateRefreshToken(user!, refreshToken, userId);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Refresh token validated successfully for user {UserId}", userId);

        var newAccessToken = _tokenProvider.GenerateAccessToken(user!);
        _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(new SuccessApiResponse<RefreshTokenResponseDto>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Access token refreshed successfully.",
            Data = new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken
            }
        });
    }
    private Result<SuccessApiResponse<RefreshTokenResponseDto>> ValidateUserExistence(User? user, Guid userId)
    {
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(UserErrors.UserNotFound);
        }
        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(default!);
    }
    private Result<SuccessApiResponse<RefreshTokenResponseDto>> ValidateRefreshToken(User user, Guid refreshToken, Guid userId)
    {
        if (refreshToken == Guid.Empty)
        {
            _logger.LogWarning("Token refresh failed: Missing refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(AuthErrors.MissingRefreshToken);
        }
        if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Invalid or expired refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(AuthErrors.InvalidRefreshToken);
        }
        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(default!);
    }
}