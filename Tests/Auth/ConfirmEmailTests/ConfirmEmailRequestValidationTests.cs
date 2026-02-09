using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class ConfirmEmailRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmEmail_WithMissingEmail_Returns400BadRequest()
    {
        var request = new ConfirmEmailRequestDto
        {
            Email = "",
            Token = "SomeToken"
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "email");
    }

    [Fact]
    public async Task ConfirmEmail_WithMissingToken_Returns400BadRequest()
    {
        var request = new ConfirmEmailRequestDto
        {
            Email = "test@example.com",
            Token = ""
        };

        var (response, content, _) = await ConfirmEmailTestHelpers.PostConfirmEmailAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "token");
    }

    private static void AssertBadRequestWithFieldError(HttpResponseMessage response, FailApiResponse? content, string fieldName)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.NotEmpty(content.Errors);
        Assert.Contains(content.Errors, e => e.Key.Contains(fieldName, StringComparison.OrdinalIgnoreCase));
    }
}
