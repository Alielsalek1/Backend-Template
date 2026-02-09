using Application.Constants;
using Application.Constants.ApiErrors;
using Microsoft.AspNetCore.Http;

namespace ENTER_NAMESPACE;

public static class InternalAuthErrors
{
    public static readonly Error UserNotFound = new(
        InternalAuthErrorCodes.UserNotFoundErrorCode,
        "No user found with the given email.",
        [],
        string.Empty,
        StatusCodes.Status404NotFound
    );

    public static readonly Error UserAlreadyExists = new(
        InternalAuthErrorCodes.UserAlreadyExistsCode,
        "A user with the given email already exists.",
        [],
        string.Empty,
        StatusCodes.Status409Conflict
    );

    public static readonly Error InvalidCredentials = new(
        InternalAuthErrorCodes.InvalidCredentialsCode,
        "Invalid email or password.",
        [],
        string.Empty,
        StatusCodes.Status401Unauthorized
    );

    public static readonly Error EmailNotConfirmed = new(
        InternalAuthErrorCodes.EmailNotConfirmedCode,
        "Email address has not been confirmed.",
        [],
        string.Empty,
        StatusCodes.Status403Forbidden
    );
}