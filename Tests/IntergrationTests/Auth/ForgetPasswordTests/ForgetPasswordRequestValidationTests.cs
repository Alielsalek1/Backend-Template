using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ForgetPasswordRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ForgetPassword_WithEmptyEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new ForgetPasswordRequestDto
        {
            Email = ""
        };

        // Act
        var (response, content, _) = await ForgetPasswordTestHelpers.PostForgetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgetPassword_WithInvalidEmailFormat_Returns400BadRequest()
    {
        // Arrange
        var request = new ForgetPasswordRequestDto
        {
            Email = "invalid-email"
        };

        // Act
        var (response, content, _) = await ForgetPasswordTestHelpers.PostForgetPasswordAsync<FailApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
