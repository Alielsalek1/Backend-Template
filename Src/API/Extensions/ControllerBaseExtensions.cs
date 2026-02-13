using Microsoft.AspNetCore.Mvc;
using Domain.Shared;
using Application.Utils;

namespace API.Extensions;

public static class ControllerBaseExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        int statusCode = 0;
        if (result.IsSuccess)
        {
            // The user says T will be the SuccessApiResponse
            // We retrieve the status code from T using dynamic to accommodate the generic type
            statusCode = (result.Data as dynamic)?.StatusCode ?? 200;
            if (statusCode < 100 || statusCode > 599) statusCode = 200;
            return controller.StatusCode(statusCode, result.Data);
        }

        var error = result.Error;
        statusCode = result.Error.statusCode;
        if (statusCode < 100 || statusCode > 599) statusCode = 500;
        var failResponse = new FailApiResponse
        {
            StatusCode = error.statusCode,
            Message = error.message,
            Errors = error.errors ?? [],
            ErrorCode = error.errorCode,
            TraceId = error.traceId
        };

        return controller.StatusCode(statusCode, failResponse);
    }

    public static void AddRefreshTokenCookie(this ControllerBase controller, string refreshToken)
    {
        controller.Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            }
        );
    }
}