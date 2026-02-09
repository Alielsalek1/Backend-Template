using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class ConfirmEmailLogicTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400BadRequest()
    {
        var request = new ConfirmEmailRequestDto
        {
            Email = "nonexistent@example.com",
            Token = "InvalidToken"
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
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
