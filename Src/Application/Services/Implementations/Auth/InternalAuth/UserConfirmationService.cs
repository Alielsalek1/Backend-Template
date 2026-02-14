using Application.Constants.ApiErrors;
using Application.DTOs.Auth;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class UserConfirmationService(
    IUserRepository userRepository, 
    IEmailService emailService,
    ILogger<UserConfirmationService> logger,
    ConfirmationTokenCacheService tokenCacheService) 
    : IUserConfirmationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<UserConfirmationService> _logger = logger;
    private readonly ConfirmationTokenCacheService _tokenCacheService = tokenCacheService;

    public async Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateConfirmEmailRequestAsync(confirmEmailRequest, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Confirming email for {Email}", confirmEmailRequest.Email);

        await _userRepository.ConfirmEmailAsync(confirmEmailRequest.Email, cancellationToken);
        await _tokenCacheService.DeleteTokenAsync(confirmEmailRequest.Email, cancellationToken);
        _logger.LogInformation("Email confirmation successful for {Email}", confirmEmailRequest.Email);

        
        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Email confirmation successful.",
        });
    }
    private async Task<Result<SuccessApiResponse>> ValidateConfirmEmailRequestAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
    {
        if (await _userRepository.IsEmailInUseAsync(confirmEmailRequest.Email, cancellationToken) == false)
        {
            _logger.LogWarning("Email confirmation failed: User with email {Email} not found", confirmEmailRequest.Email);
            return Result<SuccessApiResponse>.Failure(UserErrors.UserNotFound);
        }
        var storedToken = await _tokenCacheService.GetTokenAsync(confirmEmailRequest.Email, cancellationToken);
        if (confirmEmailRequest.Token != storedToken)
        {
            _logger.LogWarning("Email confirmation failed: Invalid token for {Email}", confirmEmailRequest.Email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.InvalidToken);
        }
        if (await _userRepository.IsEmailConfirmedAsync(confirmEmailRequest.Email, cancellationToken))
        {
            _logger.LogWarning("Email confirmation failed: Email {Email} is already confirmed", confirmEmailRequest.Email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.EmailAlreadyConfirmed);
        }
        return Result<SuccessApiResponse>.Success(default!);
    }

    public async Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var email = resendConfirmationEmailRequest.Email;
        var validationResult = await ValidateResendConfirmationEmailRequestAsync(email, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Generating new confirmation token and resending email to {Email}", email);

        var confirmationToken = ConfirmationTokenCacheService.GenerateRandomToken();
        await _tokenCacheService.SetTokenAsync(email, confirmationToken, cancellationToken);
        await _emailService.SendConfirmationEmailAsync(email, confirmationToken, cancellationToken);
        _logger.LogInformation("Confirmation email resent successfully to {Email}", email);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Confirmation email resent successfully. Please check your email for the new confirmation code.",
        });
    }
    private async Task<Result<SuccessApiResponse>> ValidateResendConfirmationEmailRequestAsync(string email, CancellationToken cancellationToken)
    {
        if (await _userRepository.IsEmailInUseAsync(email, cancellationToken) == false)
        {
            _logger.LogWarning("Resend confirmation failed: User with email {Email} not found", email);
            return Result<SuccessApiResponse>.Failure(UserErrors.UserNotFound);
        }

        if (await _userRepository.IsEmailConfirmedAsync(email, cancellationToken))
        {
            _logger.LogWarning("Resend confirmation failed: Email {Email} is already confirmed", email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.EmailAlreadyConfirmed);
        }

        return Result<SuccessApiResponse>.Success(default!);
    }
}
