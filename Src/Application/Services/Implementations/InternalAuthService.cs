using Application.DTOs.InternalAuth;
using Application.Services.Interfaces;
using Domain.Models.Users;
using Domain.Shared;
using Domain.Enums;
using Domain.Models;
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
        IDistributedCache cache,
        ITokenProvider tokenProvider,
        ILogger<InternalAuthService> logger
    ) : IInternalAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly IDistributedCache _cache = cache;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<InternalAuthService> _logger = logger;

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

        var confirmationToken = GenerateRandomToken();
        await SetTokenInCache(user.Email, confirmationToken, cancellationToken);
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
    private static string GenerateRandomToken()
    {
        return new Random().Next(100000, 999999).ToString();
    }
    private async Task SetTokenInCache(string email, string token, CancellationToken cancellationToken)
    {
        await _cache.SetStringAsync($"{email}", token, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);
    }
    private async Task<Result<SuccessApiResponse<RegisterResponseDto>>> ValidateUserIdentifiersUniquenessAsync(User user, CancellationToken cancellationToken)
    {
        if (await IsEmailInUseAsync(user.Email, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Email {Email} already in use", user.Email);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(InternalAuthErrors.EmailAlreadyExists);
        }
        if (await IsUsernameInUseAsync(user.Username, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Username {Username} already in use", user.Username);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(InternalAuthErrors.UsernameAlreadyExists);
        }
        if (!string.IsNullOrEmpty(user.PhoneNumber) && await IsPhoneNumberInUseAsync(user.PhoneNumber, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Phone number {PhoneNumber} already in use", user.PhoneNumber);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(InternalAuthErrors.PhoneNumberAlreadyExists);
        }
        return Result<SuccessApiResponse<RegisterResponseDto>>.Success(default!);
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
    private static bool IsPasswordIncorrect(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash) == false;
    }
    private static bool IsExternalAuthScheme(AuthScheme authScheme)
    {
        return authScheme == AuthScheme.External;
    }
    private Result<SuccessApiResponse<LoginResponseDto>> ValidateLoginRequest(User? user, LoginRequestDto loginRequest)
    {
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} not found", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(InternalAuthErrors.InvalidCredentials);
        }

        if (IsExternalAuthScheme(user.AuthScheme))
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} is trying to login with wrong auth scheme: {AuthScheme}", loginRequest.UsernameOrEmail, user.AuthScheme);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(InternalAuthErrors.WrongAuthScheme);
        }

        if (!user.IsEmailVerified)
        {
            _logger.LogWarning("Login failed: Email not verified for user {UsernameOrEmail}", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(InternalAuthErrors.EmailNotConfirmed);
        }

        if (IsPasswordIncorrect(loginRequest.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {UsernameOrEmail}", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(InternalAuthErrors.InvalidCredentials);
        }

        return Result<SuccessApiResponse<LoginResponseDto>>.Success(default!);
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
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(InternalAuthErrors.UserNotFound);
        }
        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(default!);
    }
    private Result<SuccessApiResponse<RefreshTokenResponseDto>> ValidateRefreshToken(User user, Guid refreshToken, Guid userId)
    {
        if (refreshToken == Guid.Empty)
        {
            _logger.LogWarning("Token refresh failed: Missing refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(InternalAuthErrors.MissingRefreshToken);
        }
        if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Invalid or expired refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(InternalAuthErrors.InvalidRefreshToken);
        }
        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(default!);
    }

    public async Task<Result<SuccessApiResponse>> ForgetPasswordAsync(ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    {
        var email = forgetPasswordRequest.Email;
        var validationResult = await ValidateForgetPasswordRequestAsync(email, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Forget password request validated for {Email}", email);

        var confirmationToken = GenerateRandomToken();
        await SetTokenInCache(email, confirmationToken, cancellationToken);        
        await _emailService.SendPasswordResetEmailAsync(email, confirmationToken, cancellationToken);
        _logger.LogInformation("Password reset email sent successfully to {Email}", email);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Password reset email sent successfully. Please check your email for the reset code.",
        });
    }
    private async Task<Result<SuccessApiResponse>> ValidateForgetPasswordRequestAsync(string email, CancellationToken cancellationToken)
    {
        if (await IsEmailInUseAsync(email, cancellationToken) == false)
        {
            _logger.LogWarning("Forget password failed: User {Email} not found", email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }
        if (await _userRepository.IsEmailConfirmedAsync(email, cancellationToken) == false)
        {
            _logger.LogWarning("Forget password failed: Email {Email} not confirmed", email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailNotConfirmed);
        }
        return Result<SuccessApiResponse>.Success(default!);
    }

    public async Task<Result<SuccessApiResponse>> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateResetPasswordRequestAsync(resetPasswordRequest, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Reset password request validated for {Email}", resetPasswordRequest.Email);

        var newHashedPassword = HashPassword(resetPasswordRequest.NewPassword);
        await _userRepository.UpdatePasswordByEmailAsync(resetPasswordRequest.Email, newHashedPassword, cancellationToken);
        await _cache.RemoveAsync(resetPasswordRequest.Email, cancellationToken);
        _logger.LogInformation("Password reset successfully for {Email}", resetPasswordRequest.Email);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Password reset successful.",
        });
    }
    private async Task<Result<SuccessApiResponse>> ValidateResetPasswordRequestAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        if (await IsEmailInUseAsync(resetPasswordRequest.Email, cancellationToken) == false)
        {
            _logger.LogWarning("Password reset failed: User {Email} not found", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }

        if (await _userRepository.IsEmailConfirmedAsync(resetPasswordRequest.Email, cancellationToken) == false)
        {
            _logger.LogWarning("Password reset failed: Email {Email} not confirmed", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailNotConfirmed);
        }

        var storedToken = await _cache.GetStringAsync(resetPasswordRequest.Email, cancellationToken);
        if (resetPasswordRequest.Token != storedToken)
        {
            _logger.LogWarning("Password reset failed: Invalid token for {Email}", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.InvalidToken);
        }

        return Result<SuccessApiResponse>.Success(default!);
    }
}