using System.Security.Claims;
using API.ActionFilters;
using API.Extensions;
using Application.DTOs.User;
using Application.Services.Interfaces;
using Application.Utils;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UserController> _logger = logger;

    [HttpPatch("profile")]
    [Idempotent]
    [ProducesResponseType(typeof(SuccessApiResponse<UpdateUserRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("Update profile request received");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Unauthorized attempt to update profile: User ID claim missing or invalid");
            return Unauthorized();
        }

        _logger.LogInformation("Updating profile for user {UserId}", userId);
        var result = await _userService.UpdateProfileAsync(userId, request, ct);
        
        if (result.IsFailure)
        {
            _logger.LogWarning("Profile update failed for user {UserId}: {Message}", userId, result.Error.message);
        }
        else
        {
            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
        }
        
        return this.ToActionResult(result);
    }
}
