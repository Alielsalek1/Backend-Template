using Application.Constants.ApiErrors;
using Application.DTOs.InternalAuth;
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
    IDistributedCache cache, 
    IEmailService emailService,
    ILogger<UserConfirmationService> logger) 
    : IUserConfirmationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IDistributedCache _cache = cache;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<UserConfirmationService> _logger = logger;

    public async Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateConfirmEmailRequestAsync(confirmEmailRequest, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Confirming email for {Email}", confirmEmailRequest.Email);

        await _userRepository.ConfirmEmailAsync(confirmEmailRequest.Email, cancellationToken);
        await _cache.RemoveAsync(confirmEmailRequest.Email, cancellationToken);
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
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }
        var storedToken = await _cache.GetStringAsync(confirmEmailRequest.Email, cancellationToken);
        if (confirmEmailRequest.Token != storedToken)
        {
            _logger.LogWarning("Email confirmation failed: Invalid token for {Email}", confirmEmailRequest.Email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.InvalidToken);
        }
        if (await _userRepository.IsEmailConfirmedAsync(confirmEmailRequest.Email, cancellationToken))
        {
            _logger.LogWarning("Email confirmation failed: Email {Email} is already confirmed", confirmEmailRequest.Email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailAlreadyConfirmed);
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

        var confirmationToken = GenerateRandomToken();
        await SetTokenInCache(email, confirmationToken, cancellationToken);
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
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }

        if (await _userRepository.IsEmailConfirmedAsync(email, cancellationToken))
        {
            _logger.LogWarning("Resend confirmation failed: Email {Email} is already confirmed", email);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailAlreadyConfirmed);
        }

        return Result<SuccessApiResponse>.Success(default!);
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

}
