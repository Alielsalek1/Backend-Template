using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class RegisterationRequestValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_WithInvalidUsername_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "abc",
            Email = "valid@example.com",
            Password = "ValidPassword123"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "username");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "not-an-email",
            Password = "ValidPassword123"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "email");
    }

    [Fact]
    public async Task Register_WithInvalidPassword_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "valid@example.com",
            Password = "abc"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "password");
    }

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "valid@example.com",
            Password = "ValidPassword123",
            PhoneNumber = "invalid-phone"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "phone");
    }

    [Fact]
    public async Task Register_WithInvalidAddress_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "valid@example.com",
            Password = "ValidPassword123",
            Address = "abc"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "address");
    }

    [Fact]
    public async Task Register_WithoutIdempotencyKey_Returns400BadRequest()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "valid@example.com",
            Password = "ValidPassword123"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<FailApiResponse>(Client, request);

        AssertBadRequestWithFieldError(response, content, "idempotency");
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
