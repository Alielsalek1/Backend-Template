using Application.Constants.ApiErrors;
using Application.DTOs.User;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Utils;
using Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class UserService(IUserRepository userRepo, ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _userRepository = userRepo;
    private readonly ILogger<UserService> _logger = logger;

    public async Task<Result<SuccessApiResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("Attempting to update profile for user {UserId}", userId);
        var user = await _userRepository.GetUserByIdAsync(userId, ct);

        if (user is null)
        {
            _logger.LogWarning("Profile update failed: User {UserId} not found", userId);
            return Result<SuccessApiResponse>.Failure(InternalAuthErrors.UserNotFound);
        }

        if (request.PhoneNumber is not null && request.PhoneNumber != user.PhoneNumber)
        {
            _logger.LogDebug("Checking if phone number {PhoneNumber} is already in use", request.PhoneNumber);
            var isPhoneTaken = await _userRepository.IsPhoneNumberInUseAsync(request.PhoneNumber, ct);
            if (isPhoneTaken)
            {
                _logger.LogWarning("Profile update failed: Phone number {PhoneNumber} already in use", request.PhoneNumber);
                return Result<SuccessApiResponse>.Failure(InternalAuthErrors.PhoneNumberAlreadyExists);
            }
        }

        user.UpdateProfile(request.Address, request.PhoneNumber);

        await _userRepository.UpdateUserAsync(user, ct);
        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

        return Result<SuccessApiResponse>.Success(new SuccessApiResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Profile updated successfully."
        });
    }
}
