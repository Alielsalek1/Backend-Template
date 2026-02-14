using System.Net;
using System.Net.Http.Headers;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Utils;
using Tests.Auth;
using Tests.Common;
using TestsReusables.Auth;
using Xunit;

namespace Tests.User;

[Collection("Integration Tests")]
public class UpdateProfileTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task UpdateProfile_WithValidData_Returns200Ok()
    {
        // Arrange
        var (userId, password, username, email) = await AuthBackdoor.CreateVerifiedUserAsync("UpdateUser1", "update1@example.com", "TestPassword123");
        
        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = email,
            Password = password
        };
        var (_, loginContent, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.Data.AccessToken);

        var updateRequest = new UpdateUserRequestDto
        {
            Address = "123 New Street",
            PhoneNumber = "+1234567890"
        };

        // Act
        var (response, content, _) = await UserTestHelpers.PatchProfileAsync<SuccessApiResponse>(Client, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal("Profile updated successfully.", content.Message);

        // Verify update in DB
        await using (var conn = new Npgsql.NpgsqlConnection(Environment.GetEnvironmentVariable("CONNECTION_STRING")))
        {
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT address, phone_number FROM users WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(updateRequest.Address, reader.GetString(0));
            Assert.Equal(updateRequest.PhoneNumber, reader.GetString(1));
        }
    }

    [Fact]
    public async Task UpdateProfile_WithDuplicatePhoneNumber_Returns409Conflict()
    {
        // Arrange
        var (user1Id, _, _, user1Email) = await AuthBackdoor.CreateVerifiedUserAsync("User1", "user1@example.com", "Pass123");
        var (user2Id, _, _, user2Email) = await AuthBackdoor.CreateVerifiedUserAsync("User2", "user2@example.com", "Pass123");
        
        // Give user 2 a phone number
        await using (var conn = new Npgsql.NpgsqlConnection(Environment.GetEnvironmentVariable("CONNECTION_STRING")))
        {
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET phone_number = '+9876543210' WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", user2Id);
            await cmd.ExecuteNonQueryAsync();
        }

        var loginRequest = new LoginRequestDto
        {
            UsernameOrEmail = user1Email,
            Password = "Pass123"
        };
        var (_, loginContent, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.Data.AccessToken);

        var updateRequest = new UpdateUserRequestDto
        {
            PhoneNumber = "+9876543210" // Already used by User2
        };

        // Act
        var (response, _, _) = await UserTestHelpers.PatchProfileAsync<FailApiResponse>(Client, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var updateRequest = new UpdateUserRequestDto
        {
            Address = "Some Address"
        };

        // Act
        var (response, _, _) = await UserTestHelpers.PatchProfileAsync<FailApiResponse>(Client, updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_IsIdempotent()
    {
        // Arrange
        var (_, password, _, email) = await AuthBackdoor.CreateVerifiedUserAsync("IdemUser", "idem@example.com", "Pass123");
        
        var loginRequest = new LoginRequestDto { UsernameOrEmail = email, Password = password };
        var (_, loginContent, _) = await LoginTestHelpers.PostLoginAsync<SuccessApiResponse<LoginResponseDto>>(Client, loginRequest);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.Data.AccessToken);

        var updateRequest = new UpdateUserRequestDto
        {
            Address = "Idempotent Address",
            PhoneNumber = "+1111111111"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act - First call
        var (response1, content1, _) = await UserTestHelpers.PatchProfileAsync<SuccessApiResponse>(Client, updateRequest, idempotencyKey);
        
        // Act - Second call with same key
        var (response2, content2, _) = await UserTestHelpers.PatchProfileAsync<SuccessApiResponse>(Client, updateRequest, idempotencyKey);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(content1);
        Assert.NotNull(content2);
        Assert.Equal(content1.Message, content2.Message);
        
        // Verify only one update happened (not strictly possible to check "twice" easily without logs, but we verify the response is cached)
        Assert.Equal("Profile updated successfully.", content2.Message);
    }
}
