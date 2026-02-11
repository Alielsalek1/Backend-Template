using Application.Constants.ApiErrors;
using Application.DTOs.InternalAuth;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Services.Implementations;

public class UserConfirmationService(
    IUserRepository userRepository, 
    IDistributedCache cache, 
    IEmailService emailService) 
    : IUserConfirmationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IDistributedCache _cache = cache;
    private readonly IEmailService _emailService = emailService;

    public async Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken)
    {
        if (!await _userRepository.IsEmailInUseAsync(confirmEmailRequest.Email, cancellationToken))
        {
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }
        var storedToken = await _cache.GetStringAsync(confirmEmailRequest.Email, cancellationToken);
        if (confirmEmailRequest.Token != storedToken)
        {
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.InvalidToken);
        }
        // confirm the email and check if it was already confirmed
        if (await _userRepository.ConfirmEmailAsync(confirmEmailRequest.Email, cancellationToken) == false)
        {
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailAlreadyConfirmed);
        }
        await _cache.RemoveAsync(confirmEmailRequest.Email, cancellationToken);
        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Email confirmation successful.",
        });
    }

    public async Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var email = resendConfirmationEmailRequest.Email;
        // user didn't even register
        if (await _userRepository.IsEmailInUseAsync(email, cancellationToken) == false)
        {
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }
        // check if the user is already registered
        if (await _userRepository.IsEmailConfirmedAsync(email, cancellationToken))
        {
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.EmailAlreadyConfirmed);
        }

        // Make the Confirmation Token which is a random 6 digits number
        var confirmationToken = new Random().Next(100000, 999999).ToString();

        // Store it in Redis with an expiry of 10 minutes (900 seconds in test)
        // Note: The key is just the user email because the InstanceName "MyBackendTemplate_" is added by the cache provider
        await _cache.SetStringAsync($"{email}", confirmationToken, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);

        // Send the confirmation email
        var emailBody = $"""
            <div style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;">
                <div style="background-color: #4f46e5; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="margin: 0; font-size: 24px; font-weight: 700;">Welcome to The Forge</h1>
                </div>
                <div style="padding: 40px; line-height: 1.8; color: #1e293b;">
                    <p style="font-size: 16px; margin-bottom: 24px;">Greetings, traveler!</p>
                    <p style="font-size: 16px; margin-bottom: 24px;">Your journey into the <strong>Backend Odyssey</strong> is about to begin. To verify your identity and unlock your realm, please use the following mystical code:</p>
                    <div style="display: block; width: fit-content; margin: 32px auto; padding: 20px 40px; background-color: #f8fafc; border: 2px dashed #4f46e5; border-radius: 12px; font-size: 32px; font-weight: 800; color: #4f46e5; letter-spacing: 8px; text-align: center; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);">
                        {confirmationToken}
                    </div>
                    <p style="font-size: 14px; color: #64748b; margin-top: 32px;">This code will lose its power in <strong>10 minutes</strong>. If you did not initiate this summoning, you may safely ignore this parchment.</p>
                </div>
                <div style="font-size: 12px; color: #94a3b8; text-align: center; padding: 20px; border-top: 1px solid #f1f5f9;">
                    &copy; 2026 The Architect's Forge. Powered by .NET 9 & Distributed Wisdom.
                </div>
            </div>
            """;

        await _emailService.SendEmailAsync(email, "Activate Your Realm - Verification Code", emailBody, cancellationToken);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Confirmation email resent successfully. Please check your email for the new confirmation code.",
        });
    }
}