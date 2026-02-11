using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class ResendConfirmationEmailRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    public async Task ResendConfirmationEmail_WithInvalidEmail_Returns400BadRequest(string? email)
    {
        var request = new ResendConfirmationEmailRequestDto
        {
            Email = email!
        };

        var (response, content, _) = await ResendConfirmationEmailTestHelpers.PostResendConfirmationEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
        Assert.NotEmpty(content.Errors);
    }
}
