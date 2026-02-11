using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

public class ResendConfirmationEmailLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ResendConfirmationEmail_WithNonExistentEmail_Returns404NotFound()
    {
        var request = new ResendConfirmationEmailRequestDto
        {
            Email = "nonexistent@example.com"
        };

        var (response, content, _) = await ResendConfirmationEmailTestHelpers.PostResendConfirmationEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(404, content.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WhenUserAlreadyVerified_Returns400BadRequest()
    {
        // 1. Create a user who is ALREADY verified
        var (_, _, _, email) = await AuthBackdoor.CreateVerifiedUserAsync("VerifiedUser", "verified@example.com");

        var request = new ResendConfirmationEmailRequestDto
        {
            Email = email
        };

        var (response, content, _) = await ResendConfirmationEmailTestHelpers.PostResendConfirmationEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
    }
}
