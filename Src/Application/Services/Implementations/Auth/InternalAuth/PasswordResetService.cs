using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Domain.Shared;
using Application.Constants.ApiErrors;
using Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Utils;

namespace Application.Services.Implementations;

public class PasswordResetService(
    IUserRepository userRepository,
    IEmailService emailService,
    ILogger<PasswordResetService> logger,
    ConfirmationTokenCacheService tokenCacheService
) : IPasswordResetService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<PasswordResetService> _logger = logger;
    private readonly ConfirmationTokenCacheService _tokenCacheService = tokenCacheService;

    public async Task<Result<SuccessApiResponse>> ForgetPasswordAsync(ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken)
    {
        var email = forgetPasswordRequest.Email;
        var validationResult = await ValidateForgetPasswordRequestAsync(email, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }
        _logger.LogInformation("Forget password request validated for {Email}", email);

        var confirmationToken = ConfirmationTokenCacheService.GenerateRandomToken();
        await _tokenCacheService.SetTokenAsync(email, confirmationToken, cancellationToken);
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
        if (await _userRepository.IsEmailInUseAsync(email, cancellationToken) == false)
        {
            _logger.LogWarning("Forget password failed: User {Email} not found", email);
            return Result<SuccessApiResponse>.Failure(UserErrors.UserNotFound);
        }
        if (await _userRepository.IsEmailConfirmedAsync(email, cancellationToken) == false)
        {
            _logger.LogWarning("Forget password failed: Email {Email} not confirmed", email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.EmailNotConfirmed);
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

        var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(resetPasswordRequest.NewPassword, BCrypt.Net.BCrypt.GenerateSalt());
        await _userRepository.UpdatePasswordByEmailAsync(resetPasswordRequest.Email, newHashedPassword, cancellationToken);
        await _tokenCacheService.DeleteTokenAsync(resetPasswordRequest.Email, cancellationToken);
        _logger.LogInformation("Password reset successfully for {Email}", resetPasswordRequest.Email);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Password reset successful.",
        });
    }
    private async Task<Result<SuccessApiResponse>> ValidateResetPasswordRequestAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        if (await _userRepository.IsEmailInUseAsync(resetPasswordRequest.Email, cancellationToken) == false)
        {
            _logger.LogWarning("Password reset failed: User {Email} not found", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(UserErrors.UserNotFound);
        }
        if (await _userRepository.IsEmailConfirmedAsync(resetPasswordRequest.Email, cancellationToken) == false)
        {
            _logger.LogWarning("Password reset failed: Email {Email} not confirmed", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.EmailNotConfirmed);
        }
        var storedToken = await _tokenCacheService.GetTokenAsync(resetPasswordRequest.Email, cancellationToken);
        if (resetPasswordRequest.Token != storedToken)
        {
            _logger.LogWarning("Password reset failed: Invalid token for {Email}", resetPasswordRequest.Email);
            return Result<SuccessApiResponse>.Failure(AuthErrors.InvalidToken);
        }
        return Result<SuccessApiResponse>.Success(default!);
    }
}
