using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth; 
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
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

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(401, content.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Returns401Unauthorized()
    {
        var (userId, correctPassword, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("PasswordUser", "password@example.com", "CorrectPassword123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = "PasswordUser",
            Password = "WrongPassword123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(401, content.StatusCode);
    }

    [Fact]
    public async Task Login_WithExternalAuthUser_Returns401Unauthorized()
    {
        // Arrange: Create a user with External AuthScheme (1)
        var (_, _, username, _) = await AuthBackdoor.CreateVerifiedUserAsync("ExternalUser", "external@example.com", "TestPassword123", authScheme: 1);

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = username,
            Password = "TestPassword123"
        };

        // Act
        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
    }
}
