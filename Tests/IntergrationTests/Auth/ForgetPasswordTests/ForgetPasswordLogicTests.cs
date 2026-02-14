using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ForgetPasswordLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ForgetPassword_WithNonExistentEmail_Returns404NotFound()
    {
        // Arrange
        var request = new ForgetPasswordRequestDto
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var (response, content, _) = await ForgetPasswordTestHelpers.PostForgetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task ForgetPassword_WithUnverifiedEmail_Returns403Forbidden()
    {
        // Arrange: Create a user but don't verify them
        var email = "unverified@example.com";
        await AuthBackdoor.CreateUnverifiedUserAsync("UnverifiedUser", email, "TestPassword123");

        var request = new ForgetPasswordRequestDto
        {
            Email = email
        };

        // Act
        var (response, content, _) = await ForgetPasswordTestHelpers.PostForgetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
    }
}
