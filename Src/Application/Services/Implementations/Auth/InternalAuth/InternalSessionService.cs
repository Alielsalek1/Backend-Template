using Application.Constants.ApiErrors;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Enums;
using Domain.Models.User;
using Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations.Auth;

public class InternalSessionService(
    IUserRepository userRepository,
    IJwtTokenProvider tokenProvider,
    ILogger<InternalSessionService> logger
    ) : IInternalSessionService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtTokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<InternalSessionService> _logger = logger;

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
        if (UserExists(user) == false)
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} not found", loginRequest.UsernameOrEmail);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.InvalidCredentials);
        }
        if (IsExternalAuthScheme(user!.AuthScheme))
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} is trying to login with wrong auth scheme: {AuthScheme}", loginRequest.UsernameOrEmail, user.AuthScheme);
            return Result<SuccessApiResponse<LoginResponseDto>>.Failure(AuthErrors.WrongAuthScheme);
        }
        if (user.IsEmailVerified == false)
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

    public async Task<Result<SuccessApiResponse<GuestLoginResponseDto>>> GuestLoginAsync(CancellationToken cancellationToken)
    {
        var user = CreateGuestUser();
        await _userRepository.AddUserAsync(user, cancellationToken);
        var accessToken = _tokenProvider.GenerateAccessToken(user);
        _logger.LogInformation("Guest user created and logged in successfully with user ID {UserId}", user.Id);
        
        return Result<SuccessApiResponse<GuestLoginResponseDto>>.Success(new SuccessApiResponse<GuestLoginResponseDto>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Guest login successful.",
            Data = new GuestLoginResponseDto
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken.ToString()
            }
        });
    }
    private static User CreateGuestUser()
    {
        return new User(new GuestUserCreationParams());
    }

    public async Task<Result<SuccessApiResponse<RefreshTokenResponseDto>>> RefreshTokenAsync(Guid userId, Guid refreshToken, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        var validationResult = ValidateRefreshToken(user, refreshToken, userId);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Refresh token validated successfully for user {UserId}", userId);

        user!.GenerateNewRefreshToken();
        await _userRepository.UpdateUserAsync(user, cancellationToken);
        _logger.LogInformation("New refresh token generated and saved for user {UserId}", userId);

        var newAccessToken = _tokenProvider.GenerateAccessToken(user!);
        _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(new SuccessApiResponse<RefreshTokenResponseDto>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Access token refreshed successfully.",
            Data = new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = user.RefreshToken.ToString()
            }
        });
    }
    private bool UserExists(User? user)
    {
        if (user == null)
        {
            _logger.LogWarning("User not found");
            return false;
        }
        return true;
    }
    private Result<SuccessApiResponse<RefreshTokenResponseDto>> ValidateRefreshToken(User? user, Guid refreshToken, Guid userId)
    {
        if (UserExists(user) == false)
        {
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(UserErrors.UserNotFound);
        }
        if (refreshToken == Guid.Empty)
        {
            _logger.LogWarning("Token refresh failed: Missing refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(AuthErrors.MissingRefreshToken);
        }
        if (user!.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Invalid or expired refresh token for user {UserId}", userId);
            return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Failure(AuthErrors.InvalidRefreshToken);
        }
        return Result<SuccessApiResponse<RefreshTokenResponseDto>>.Success(default!);
    }
}