using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth; 
using Xunit;

namespace Tests.Auth;

public class LoginLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_WithNonExistentUser_Returns404NotFound()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "NonExistentUser",
            Password = "Password123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(404, content.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Returns400BadRequest()
    {
        var (userId, correctPassword, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("PasswordUser", "password@example.com", "CorrectPassword123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = "PasswordUser",
            Password = "WrongPassword123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, loginRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
    }
}
