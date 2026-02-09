using System.Net;
using Application.DTOs.InternalAuth;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class RegisterationEmailTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_SendsConfirmationEmail()
    {
        var request = new RegisterRequestDto
        {
            Username = "EmailTestUser",
            Email = "emailtest@example.com",
            Password = "TestPassword123",
            PhoneNumber = "+1234567890",
            Address = "123 Test Street"
        };
        var (response, _, _) = await RegisterationTestHelpers.PostRegisterAsync<object>(Client, request);

        await Task.Delay(500);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var messages = await Factory.MailhogClient!.GetMessagesAsync();
        Assert.NotNull(messages);
        Assert.True(messages.Total > 0, "No emails were sent");

        var emailMessages = await Factory.MailhogClient!.SearchMessagesByRecipientAsync(request.Email);
        Assert.Single(emailMessages.Items);

        var sentEmail = emailMessages.Items[0];
        Assert.Equal(request.Email, sentEmail.To[0].Email);
        Assert.Contains("confirm", sentEmail.Content.Headers["Subject"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_EmailContainsCorrectSubject()
    {
        var request = new RegisterRequestDto
        {
            Username = "SubjectTestUser",
            Email = "subjecttest@example.com",
            Password = "TestPassword123"
        };

        await RegisterationTestHelpers.PostRegisterAsync<object>(Client, request);
        await Task.Delay(500);

        var messages = await Factory.MailhogClient!.SearchMessagesByRecipientAsync(request.Email);
        Assert.Single(messages.Items);

        var email = messages.Items[0];
        var subject = email.Content.Headers["Subject"][0];
        
        Assert.NotNull(subject);
        Assert.Contains("email", subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confirm", subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_EmailContainsUsername()
    {
        var request = new RegisterRequestDto
        {
            Username = "UsernameTestUser",
            Email = "usernametest@example.com",
            Password = "TestPassword123"
        };

        await RegisterationTestHelpers.PostRegisterAsync<object>(Client, request);
        await Task.Delay(500);

        var messages = await Factory.MailhogClient!.SearchMessagesByRecipientAsync(request.Email);
        Assert.Single(messages.Items);

        var email = messages.Items[0];
        var body = email.Content.Body;
        Assert.Contains(request.Username, body);
    }

    [Fact]
    public async Task Register_EmailContainsConfirmationToken()
    {
        var request = new RegisterRequestDto
        {
            Username = "LinkTestUser",
            Email = "linktest@example.com",
            Password = "TestPassword123"
        };

        await RegisterationTestHelpers.PostRegisterAsync<object>(Client, request);
        await Task.Delay(500);

        var messages = await Factory.MailhogClient!.SearchMessagesByRecipientAsync(request.Email);
        Assert.Single(messages.Items);

        var email = messages.Items[0];
        var body = email.Content.Body;
        Assert.Contains("/api/v1/internal-auth/confirm-email", body);
        Assert.Contains("token=", body);
    }
}