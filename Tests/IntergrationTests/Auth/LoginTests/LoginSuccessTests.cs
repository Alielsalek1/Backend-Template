using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class LoginSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_WithValidUsername_Returns200OkWithToken()
    {
        // Arrange: create user directly in DB (backdoor) so test is isolated from registration endpoint
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("LoginUser1", "login1@example.com", "TestPassword123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = "LoginUser1",
            Password = "TestPassword123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);

        AssertLoginSuccess(response, content);
    }

    [Fact]
    public async Task Login_WithValidEmail_Returns200OkWithToken()
    {
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("LoginUser2", "login2@example.com", "TestPassword123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = "login2@example.com",
            Password = "TestPassword123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);

        AssertLoginSuccess(response, content);
    }

    private static void AssertLoginSuccess(HttpResponseMessage response, SuccessApiResponse<LoginResponseDto>? content)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal(200, content.StatusCode);
        Assert.Equal("Login successful.", content.Message);
        Assert.NotEqual(Guid.Empty, content.Data.UserId);
        Assert.False(string.IsNullOrWhiteSpace(content.Data.AccessToken));
    }
}
