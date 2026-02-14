using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ResetPasswordRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ResetPassword_WithMissingFields_Returns400BadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "",
            Token = "",
            NewPassword = ""
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithShortPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "123456",
            NewPassword = "short"
        };

        // Act
        var (response, content, _) = await ResetPasswordTestHelpers.PostResetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
