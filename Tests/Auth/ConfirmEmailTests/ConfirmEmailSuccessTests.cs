using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
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
        // Seed token into Redis using IDistributedCache via Backdoor
        await AuthBackdoor.SeedConfirmationTokenAsync(Factory, email, token);

        var confirmRequest = new ConfirmEmailRequestDto
        {
            Email = email,
            Token = token
        };

        var (confirmResponse, confirmContent, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<SuccessApiResponse<object>>(Client, confirmRequest);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.True(confirmContent!.Success);
        Assert.Equal("Email confirmation successful.", confirmContent.Message);

        // Verify token is removed from Redis
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var tokenAfterConfirmation = await cache.GetStringAsync(email);
        Assert.Null(tokenAfterConfirmation);
    }
}
