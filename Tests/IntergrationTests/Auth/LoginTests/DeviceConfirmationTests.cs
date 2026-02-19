using System.Net;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.Utils;
using Microsoft.Extensions.Caching.Distributed;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class DeviceConfirmationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Login_FromNewDevice_Returns202Accepted_AndSendsConfirmationEmail()
    {
        // Arrange: Create a verified user
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceUser1", "device1@example.com", "Password123");

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = email,
            Password = password
        };

        // Act: Login from new device (using a new client to ensure no default deviceId cookie)
        var freshClient = Factory.CreateClient();
        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(freshClient, loginRequest);

        // Assert: Should return 202 Accepted and ask for device confirmation
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Contains("New device detected", content.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confirmation email", content.Message, StringComparison.OrdinalIgnoreCase);

        // Verify email was sent
        await Task.Delay(500); // Give email time to arrive
        var mailhogClient = Mailhog.CreateClient();
        var messages = await mailhogClient.GetMessagesAsync();
        Assert.NotEmpty(messages.Items);
        
        var lastEmail = messages.Items[^1];
        Assert.Contains(email, lastEmail.Content.Headers["To"][0]);
        Assert.Contains("New Device", lastEmail.Content.Headers["Subject"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_FromKnownDevice_Returns200OK_AndSkipsDeviceConfirmation()
    {
        // Arrange: Create a verified user and add their device to the database
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceUser2", "device2@example.com", "Password123");
        var deviceId = Guid.NewGuid();
        
        // Add device to database using raw SQL
        var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO user_devices (user_id, device_id) VALUES (@userId, @deviceId)";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@deviceId", deviceId);
        await cmd.ExecuteNonQueryAsync();

        // Set the deviceId cookie
        Client.DefaultRequestHeaders.Add("X-Test-Device-ID", deviceId.ToString());

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = email,
            Password = password
        };

        // Act: Login from known device
        var (response, content, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);

        // Assert: Should return 200 OK and complete login
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.NotNull(content.Data);
        Assert.Equal("Login successful.", content.Message);
        Assert.NotNull(content.Data.AccessToken);
        Assert.Equal(userId, content.Data.UserId);

        // Assert deviceId cookie is set
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.Contains($"deviceId={deviceId}"));
    }

    [Fact]
    public async Task ConfirmLogin_WithValidToken_Returns200OK_AndAddsDeviceToDatabase()
    {
        // Arrange: Create a verified user and simulate new device login
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("DeviceUser3", "device3@example.com", "Password123");
        var deviceId = Guid.NewGuid();
        
        // Seed new_device token in Redis
        var token = "123456";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(userId, deviceId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        // Act: Confirm the new device login
        var (response, content, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, confirmRequest);

        // Assert: Should return 200 OK with login tokens
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
        Assert.Equal("Login confirmed successfully.", content.Message);
        Assert.NotNull(content.Data.AccessToken);
        Assert.Equal(userId, content.Data.UserId);

        // Assert deviceId cookie is set
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.Contains($"deviceId={deviceId}"));

        // Verify device was added to database
        var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        await using var conn = new Npgsql.NpgsqlConnection(connStr);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM user_devices WHERE user_id = @userId AND device_id = @deviceId";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@deviceId", deviceId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConfirmLogin_WithInvalidToken_Returns400BadRequest()
    {
        // Arrange
        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = "invalid-token"
        };

        // Act
        var (response, content, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<FailApiResponse>(Client, confirmRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(content);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task ConfirmLogin_WithExpiredToken_Returns400BadRequest()
    {
        // Arrange: Create token with very short expiry
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var token = "expired-token";
        var storedValue = JsonSerializer.Serialize(new Application.DTOs.Auth.InternalAuth.NewDeviceOtpPayload(userId, deviceId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        
        await Cache.SetStringAsync($"new_device:{token}", storedValue, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1)
        });

        // Wait for expiration
        await Task.Delay(100);

        var confirmRequest = new ConfirmLoginRequestDto
        {
            Otp = token
        };

        // Act
        var (response, content, _) = await ConfirmLoginTestHelpers.PostConfirmLoginAsync<FailApiResponse>(Client, confirmRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
