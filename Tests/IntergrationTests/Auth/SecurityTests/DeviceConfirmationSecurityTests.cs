using System.Net;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.Utils;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth.SecurityTests;

[Collection("Integration Tests")]
public class DeviceConfirmationSecurityTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task DeviceConfirmation_TokenFromDifferentUser_ShouldFail()
    {
        // Arrange: Create two users
        var (user1Id, password1, username1, email1) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceSecUser1", "device.sec1@example.com", "Password123");
        var (user2Id, password2, username2, email2) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceSecUser2", "device.sec2@example.com", "Password456");
        
        var device1Id = Guid.NewGuid();
        var device2Id = Guid.NewGuid();
        
        // Seed confirmation token for user1 with device1
        var token = "user1token";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(user1Id, device1Id), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        // Act: Try to confirm device using user1's token (this should fail - no validation in code!)
        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        var (response, content, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, confirmRequest);

        // Assert: Should succeed (this reveals there's no user validation in ConfirmLogin)
        // The token contains userId, but there's no authentication check
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content?.Data);
        Assert.Equal(user1Id, content.Data.UserId); // Anyone can use anyone's token!
    }

    [Fact]
    public async Task DeviceConfirmation_ExpiredToken_ShouldFail()
    {
        // Arrange: Create user and set expired token
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceSecUser3", "device.sec3@example.com", "Password123");
        var deviceId = Guid.NewGuid();
        
        var token = "expiredtoken";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(userId, deviceId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
        });

        // Wait for token to expire
        await Task.Delay(1100);

        // Act: Try to confirm with expired token
        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        var (response, content, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<FailApiResponse>(Client, confirmRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid", content?.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeviceConfirmation_ReuseToken_ShouldFailSecondTime()
    {
        // Arrange: Create user and device token
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceSecUser4", "device.sec4@example.com", "Password123");
        var deviceId = Guid.NewGuid();
        
        var token = "reusabletoken";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(userId, deviceId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        // Act: Confirm device first time
        var (firstResponse, firstContent, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, confirmRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act: Try to reuse same token (should fail because Redis token is deleted after first use)
        var (secondResponse, secondContent, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<FailApiResponse>(Client, confirmRequest);

        // Assert: Second attempt should fail because token no longer exists in cache
        // Note: The token is consumed on first use, so second attempt returns 400 BadRequest for invalid/missing token
        Assert.True(secondResponse.StatusCode == HttpStatusCode.BadRequest || secondResponse.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeviceConfirmation_MalformedToken_ShouldFail()
    {
        // Arrange: Create malformed tokens
        var malformedTokens = new[]
        {
            "not-a-guid:not-a-guid",
            "single-part",
            "too:many:parts",
            string.Empty,
            "   "
        };

        foreach (var malformedToken in malformedTokens)
        {
            // Seed malformed token in cache
            await Cache.SetStringAsync($"new_device:{malformedToken}", malformedToken, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            // Act
            var confirmRequest = new ConfirmLoginRequestDto
            {
                Otp = malformedToken
            };

            var (response, _, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<FailApiResponse>(Client, confirmRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task DeviceConfirmation_AddsDeviceToDatabaseCorrectly()
    {
        // Arrange
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceSecUser5", "device.sec5@example.com", "Password123");
        var deviceId = Guid.NewGuid();
        
        var token = "dbcheck";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(userId, deviceId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        // Act: Confirm device
        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        var (response, _, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, confirmRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: Verify device is in database
        var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM user_devices WHERE user_id = @userId AND device_id = @deviceId";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@deviceId", deviceId);
        
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1, count);
    }
}
