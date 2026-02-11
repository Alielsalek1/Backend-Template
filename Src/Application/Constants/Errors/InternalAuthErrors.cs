using Application.Constants;
using Application.Constants.ApiErrors;
using Application.Constants.ErrorCodes;
using Microsoft.AspNetCore.Http;

namespace Application.Constants.ApiErrors;

public static class InternalAuthErrors
{
    public static readonly Error UserNotFound = new(
        InternalAuthErrorCodes.UserNotFoundErrorCode,
        "user not found.",
        [],
        string.Empty,
        StatusCodes.Status404NotFound
    );

    public static readonly Error EmailAlreadyExists = new(
        InternalAuthErrorCodes.EmailAlreadyExistsCode,
        "A user with the given email already exists.",
        [],
        string.Empty,
        StatusCodes.Status409Conflict
    );

    public static readonly Error PhoneNumberAlreadyExists = new(
        InternalAuthErrorCodes.PhoneNumberAlreadyExistsCode,
        "A user with the given phone number already exists.",
        [],
        string.Empty,
        StatusCodes.Status409Conflict
    );

    public static readonly Error UsernameAlreadyExists = new(
        InternalAuthErrorCodes.UsernameAlreadyExistsCode,
        "A user with the given username already exists.",
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

    public static readonly Error EmailAlreadyConfirmed = new(
        InternalAuthErrorCodes.EmailAlreadyConfirmedCode,
        "Email address is already confirmed.",
        [],
        string.Empty,
        StatusCodes.Status400BadRequest
    );

    public static readonly Error InvalidToken = new(
        InternalAuthErrorCodes.InvalidActivationTokenCode,
        "The provided token is invalid or has expired.",
        [],
        string.Empty,
        StatusCodes.Status400BadRequest
    );
}