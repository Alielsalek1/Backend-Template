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

public class InternalAccountService(
        IUserRepository userRepository,
        IEmailService emailService,
        IJwtTokenProvider tokenProvider,
        ILogger<InternalAccountService> logger,
        ConfirmationTokenCacheService tokenCacheService
    ) : IInternalAccountService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly IJwtTokenProvider _tokenProvider = tokenProvider;
    private readonly ILogger<InternalAccountService> _logger = logger;
    private readonly ConfirmationTokenCacheService _tokenCacheService = tokenCacheService;

    public async Task<Result<SuccessApiResponse<RegisterResponseDto>>> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken)
    {
        var user = CreateUserForRegisteration(registerRequest);
        var uniquenessResult = await ValidateRegisterRequestAsync(user, cancellationToken);
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
    private static User CreateUserForRegisteration(RegisterRequestDto registerRequest)
    {
        // map using Mapster
        var userCreationParams = registerRequest.Adapt<UserCreationParams>();
        userCreationParams = userCreationParams with 
        { 
            PasswordHash = HashPassword(registerRequest.Password),
            AuthScheme = AuthScheme.Internal
        };
        var user = new User(userCreationParams);
        return user;
    }
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt());
    }
    private async Task<Result<SuccessApiResponse<RegisterResponseDto>>> ValidateRegisterRequestAsync(User user, CancellationToken cancellationToken)
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

    public async Task<Result<SuccessApiResponse<RegisterResponseDto>>> GuestPromoteAsync(RegisterRequestDto registerRequest, Guid userId, CancellationToken cancellationToken)
    {
        var user = CreateUserForRegisteration(registerRequest);
        user.SetGuestId(userId);

        var validateGuestPromoteRequestAsyncResult = await ValidateGuestPromoteRequestAsync(user, cancellationToken);
        if (!validateGuestPromoteRequestAsyncResult.IsSuccess)        
        {
            return validateGuestPromoteRequestAsyncResult;
        }
        _logger.LogInformation("User identifiers are unique for email: {Email}, username: {Username}", user.Email, user.Username);

        await _userRepository.UpdateUserAsync(user, cancellationToken);
        _logger.LogInformation("User promoted successfully with email: {Email}", user.Email);

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
    private async Task<Result<SuccessApiResponse<RegisterResponseDto>>> ValidateGuestPromoteRequestAsync(User user, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetUserByIdAsync(user.Id, cancellationToken);
        if (UserExists(existingUser) == false)
        {
            _logger.LogWarning("Guest promotion failed: User with ID {UserId} not found", user.Id);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(UserErrors.UserNotFound);
        }
        if (existingUser!.IsGuest() == false)
        {
            _logger.LogWarning("Guest promotion failed: User with ID {UserId} is not a guest user", user.Id);
            return Result<SuccessApiResponse<RegisterResponseDto>>.Failure(UserErrors.UserIsNotGuest);
        }

        var uniquenessResult = await ValidateRegisterRequestAsync(user, cancellationToken);
        if (!uniquenessResult.IsSuccess)        
        {
            return uniquenessResult;
        }
        return Result<SuccessApiResponse<RegisterResponseDto>>.Success(default!);
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
}