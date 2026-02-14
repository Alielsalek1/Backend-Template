using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ResetPasswordSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ResetPassword_WithValidToken_Returns200Ok_AndUpdatesPassword()
    {
        // Arrange
        var (_, oldPassword, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("ResetUser", "reset@example.com", "OldPassword123");

        // Generate token and store in Redis
        var token = "123456";
        var cache = Factory.Services.GetRequiredService<IDistributedCache>();
        await cache.SetStringAsync(email, token, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var newPassword = "NewPassword123";
        var request = new ResetPasswordRequestDto
        {
            Email = email,
            Token = token,
            NewPassword = newPassword
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<SuccessApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);

        // Verify login with new password works
        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = email,
            Password = newPassword
        };
        var (loginResponse, loginContent, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}
