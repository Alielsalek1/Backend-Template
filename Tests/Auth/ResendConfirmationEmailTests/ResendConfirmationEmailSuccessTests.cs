using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

public class ResendConfirmationEmailSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ResendConfirmationEmail_WithValidUnverifiedUser_Returns200OKAndSendsEmail()
    {
        // 1. Create an unverified user
        var (_, _, _, email) = await AuthBackdoor.CreateUnverifiedUserAsync("ResendSuccessUser", "resend@example.com");

        var request = new ResendConfirmationEmailRequestDto
        {
            Email = email
        };

        var (response, content, _) = await ResendConfirmationEmailTestHelpers.PostResendConfirmationEmailAsync<SuccessApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);

        // 2. Verify email sent via Mailhog
        var mailClient = Mailhog.CreateClient();
        await Task.Delay(500); // Wait for email to be processed
        
        var messages = await mailClient.SearchMessagesByRecipientAsync(email);
        Assert.Single(messages.Items);
        
        var sentEmail = messages.Items[0];
        Assert.Equal(email, sentEmail.To[0].Email);
        Assert.Contains("Activate", sentEmail.Content.Headers["Subject"][0], StringComparison.OrdinalIgnoreCase);
    }
}
