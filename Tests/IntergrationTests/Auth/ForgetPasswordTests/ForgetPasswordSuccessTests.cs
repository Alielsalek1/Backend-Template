using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ForgetPasswordSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ForgetPassword_WithValidEmail_Returns200Ok_AndSendsEmail()
    {
        // Arrange
        var (_, _, _, email) = await AuthBackdoor.CreateVerifiedUserAsync("ForgetPassUser", "forget@example.com", "TestPassword123");

        var request = new ForgetPasswordRequestDto
        {
            Email = email
        };

        // Act
        var (response, content, _) = await ForgetPasswordTestHelpers.PostForgetPasswordAsync<SuccessApiResponse>(Client, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);

        // Verify email was sent
        var mailhogClient = Factory.MailhogClient!;
        var messagesResponse = await mailhogClient.GetMessagesAsync();
        Assert.Contains(messagesResponse.Items, m => 
            m.To.Any(t => $"{t.Mailbox}@{t.Domain}" == email) && 
            m.Content.Headers.TryGetValue("Subject", out var subject) && 
            subject.Any(s => s.Contains("Reset Your Password")));
    }
}
