using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class ConfirmEmailLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400BadRequest()
    {
        // 1. Register a user first
        var registerRequest = new RegisterRequestDto
        {
            Username = "InvalidTokenUser",
            Email = "invalidtoken@example.com",
            Password = "Password123"
        };
        await RegisterationTestHelpers.PostRegisterAsync<object>(Client, registerRequest);

        // 2. Try to confirm with an invalid token
        var request = new ConfirmEmailRequestDto
        {
            Email = registerRequest.Email,
            Token = "InvalidToken"
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WhenAlreadyConfirmed_Returns400BadRequest()
    {
        // 1. Create a user who is ALREADY verified
        var (_, _, _, email) = await AuthBackdoor.CreateVerifiedUserAsync("AlreadyConfirmedUser", "confirmed@example.com");

        // 2. Seed a token in Redis so it passes the token check
        var validToken = "123456";
        await AuthBackdoor.SeedConfirmationTokenAsync(Factory, email, validToken);

        var request = new ConfirmEmailRequestDto
        {
            Email = email,
            Token = validToken
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Contains("already confirmed", content.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmail_WithNonExistentUser_Returns404NotFound()
    {
        var request = new ConfirmEmailRequestDto
        {
            Email = "nonexistent@example.com",
            Token = "SomeToken"
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(404, content.StatusCode);
    }
}
