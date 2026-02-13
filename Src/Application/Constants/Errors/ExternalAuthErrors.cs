using Application.Constants.ApiErrors;
using Application.Constants.ErrorCodes;
using Application.Utils;
using Microsoft.AspNetCore.Http;

namespace Application.Constants.Errors;

public static class ExternalAuthErrors
{
    public static readonly Error InvalidCredentials = new(
        ExternalAuthErrorCodes.InvalidCredentials,
        "Invalid external authentication credentials.",
        [],
        string.Empty,
        StatusCodes.Status401Unauthorized
    );

    public static readonly Error EmailAlreadyInUse = new(
        ExternalAuthErrorCodes.EmailAlreadyInUse,
        "A user with this email already exists and is not registered via external authentication.",
        [],
        string.Empty,
        StatusCodes.Status409Conflict
    );
}