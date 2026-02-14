using API.ActionFilters;
using API.Extensions;
using Application.DTOs.User;
using Application.Services.Interfaces;
using Application.Utils;
using Asp.Versioning;
using Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController(IUserFacadeService userFacade) : ControllerBase
{
    private readonly IUserFacadeService _userFacade = userFacade;
    [HttpPatch("profile")]
    [Idempotent]
    [ProducesResponseType(typeof(SuccessApiResponse<UpdateUserRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        var userIdResult = this.GetAuthenticatedUserId();
        if (!userIdResult.IsSuccess)
        {
            return this.ToActionResult(Result<SuccessApiResponse>.Failure(userIdResult.Error));
        }

        var result = await _userFacade.UpdateProfileAsync(userIdResult.Data, request, ct);
        return this.ToActionResult(result);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(SuccessApiResponse<GetUserProfileResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userIdResult = this.GetAuthenticatedUserId();
        if (!userIdResult.IsSuccess)
        {
            return this.ToActionResult(Result<SuccessApiResponse>.Failure(userIdResult.Error));
        }
        
        var result = await _userFacade.GetProfileAsync(userIdResult.Data, ct);
        return this.ToActionResult(result);
    }
}
