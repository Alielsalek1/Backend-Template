using System.Net;
using System.Net.Http.Headers;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class JwtAuthorizationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task HealthAuth_WithValidJwtToken_Returns200Ok()
    {
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("AuthTestUser", "authtest@example.com", "TestPassword123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = "AuthTestUser",
            Password = "TestPassword123"
        };

        var (loginResponse, loginContent, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);
        
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var token = loginContent!.Data!.AccessToken;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var authResponse = await Client.GetAsync("/health/auth");

        Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
        var authContent = await authResponse.Content.ReadAsStringAsync();
        Assert.Contains("Authenticated", authContent);

        Client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task HealthAuth_WithoutToken_Returns401Unauthorized()
    {
        var response = await Client.GetAsync("/health/auth");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}