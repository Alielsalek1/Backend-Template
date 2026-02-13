namespace Application.Constants.ErrorCodes;

public static class InternalAuthErrorCodes
{
    public const string UserNotFoundErrorCode = "USER_NOT_FOUND";
    public const string EmailAlreadyExistsCode = "EMAIL_ALREADY_EXISTS";
    public const string PhoneNumberAlreadyExistsCode = "PHONE_NUMBER_ALREADY_EXISTS";
    public const string UsernameAlreadyExistsCode = "USERNAME_ALREADY_EXISTS";
    public const string InvalidCredentialsCode = "INVALID_CREDENTIALS";
    public const string EmailNotConfirmedCode = "EMAIL_NOT_CONFIRMED";
    public const string EmailAlreadyConfirmedCode = "EMAIL_ALREADY_CONFIRMED";
    public const string InvalidActivationTokenCode = "INVALID_ACTIVATION_TOKEN";
    public const string InvalidRefreshTokenCode = "INVALID_REFRESH_TOKEN";
    public const string WrongAuthSchemeCode = "WRONG_AUTH_SCHEME";
}