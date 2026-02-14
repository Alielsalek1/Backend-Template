using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ResetPasswordLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400BadRequest()
    {
        // Arrange
        var (_, _, _, email) = await AuthBackdoor.CreateVerifiedUserAsync("InvalidTokenReset", "invalidtoken@example.com", "TestPassword123");

        // Set token in Redis
        var cache = Factory.Services.GetRequiredService<IDistributedCache>();
        await cache.SetStringAsync(email, "111111", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var request = new ResetPasswordRequestDto
        {
            Email = email,
            Token = "wrong-token",
            NewPassword = "NewPassword123"
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistentUser_Returns404NotFound()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "nonexistent@example.com",
            Token = "123456",
            NewPassword = "NewPassword123"
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithUnverifiedUser_Returns403Forbidden()
    {
        // Arrange
        var email = "unverified-reset@example.com";
        await AuthBackdoor.CreateUnverifiedUserAsync("UnverifiedReset", email, "TestPassword123");

        var request = new ResetPasswordRequestDto
        {
            Email = email,
            Token = "123456",
            NewPassword = "NewPassword123"
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
