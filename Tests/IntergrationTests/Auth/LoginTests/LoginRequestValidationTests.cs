using System.Net;
using Application.DTOs.Auth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class LoginRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_WithEmptyUsernameOrEmail_Returns400BadRequest()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "",
            Password = "ValidPassword123"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "usernameoremail");
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns400BadRequest()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "ValidUser",
            Password = ""
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "password");
    }

    [Fact]
    public async Task Login_WithTooShortPassword_Returns400BadRequest()
    {
        var request = new LoginRequestDto
        {
            UsernameOrEmail = "ValidUser",
            Password = "abc"
        };

        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "password");
    }

    private static void AssertBadRequestWithFieldError(HttpResponseMessage response, FailApiResponse? content, string fieldName)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.Equal(400, content.StatusCode);
        Assert.Contains(fieldName, content.Message.ToLower());
    }
}
