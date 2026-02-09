using System.Net;
using Application.DTOs.InternalAuth;
using Application.Utils;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

public class RegisterationSuccessTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_WithAddresss_Returns201CreatedWithUserId()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "user@example.com",
            Password = "TestPassword123",
            Address = "123 Main Street"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request);

        AssertRegistrationSuccess(response, content);
    }

    [Fact]
    public async Task Register_WithPhoneNumber_Returns201CreatedWithUserId()
    {
        var request = new RegisterRequestDto
        {
            Username = "ValidUser123",
            Email = "user@example.com",
            Password = "TestPassword123",
            PhoneNumber = "+1234567890",
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request);

        AssertRegistrationSuccess(response, content);
    }

    [Fact]
    public async Task Register_WithMinimalValidData_Returns201CreatedWithUserIdAsync()
    {
        var request = new RegisterRequestDto
        {
            Username = "MinimalUser",
            Email = "minimal@example.com",
            Password = "MinimalPass123"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request);

        AssertRegistrationSuccess(response, content);
    }

    [Fact]
    public async Task Register_PutsTokenInRedisCacheWithCorrectExpiration()
    {
        var request = new RegisterRequestDto
        {
            Username = "RedisTestUser",
            Email = "redis@example.com",
            Password = "TestPassword123"
        };

        var (response, content, _) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Actual key in Redis will be prefixed with the InstanceName from Program.cs
        // User requested that the key be the user Guid
        var redisKey = $"MyBackendTemplate_{content!.Data.UserId}";
        var token = await Redis.GetValueAsync(redisKey);
        var ttl = await Redis.GetTTLAsync(redisKey);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // Expiration should be around 15 minutes (900 seconds)
        Assert.True(ttl > 0 && ttl <= 900, $"Expected TTL to be between 0 and 900 seconds, but was {ttl}");
    }

    [Fact]
    public async Task Register_IsIdempotent_ReturnsSameSuccessResponseForSameKey()
    {
        var request = new RegisterRequestDto
        {
            Username = "IdempotentUser",
            Email = "idempotent@example.com",
            Password = "IdempotentPass123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        // First request
        var (response1, content1, json1) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Second request with same key
        var (response2, content2, json2) = await RegisterationTestHelpers.PostRegisterAsync<SuccessApiResponse<RegisterResponseDto>>(Client, request, idempotencyKey);

        // Assertions
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(json1, json2);
    }

    private static void AssertRegistrationSuccess(HttpResponseMessage response, SuccessApiResponse<RegisterResponseDto>? content)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal(201, content.StatusCode);
        Assert.Equal("Registration successful.", content.Message);
        Assert.NotEqual(Guid.Empty, content.Data.UserId);
        Assert.NotNull(content.TraceId);
    }
}
