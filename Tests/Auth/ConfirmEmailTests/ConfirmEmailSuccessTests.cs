using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

public class ConfirmEmailSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmEmail_WithValidToken_Returns200OKAndRemovesTokenFromRedis()
    {
        // Arrange: create an unverified user directly in the DB and seed a confirmation token into Redis
        var (userIdGuid, password, username, email) = await AuthBackdoor.CreateUnverifiedUserAsync("ConfirmUser", "confirm@example.com", "TestPassword123");
        var token = Guid.NewGuid().ToString();
        // Seed token into Redis using Redis provider
        await Redis.SetValueAsync($"MyBackendTemplate_{userIdGuid}", token, 900);

        var confirmRequest = new ConfirmEmailRequestDto
        {
            Email = email,
            Token = token
        };

        var (confirmResponse, confirmContent, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<SuccessApiResponse<object>>(Client, confirmRequest);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.True(confirmContent!.Success);
        Assert.Equal("Email confirmation successful.", confirmContent.Message);

        var tokenAfterConfirmation = await Redis.GetValueAsync($"MyBackendTemplate_{userIdGuid}");
        Assert.Null(tokenAfterConfirmation);
    }
}
