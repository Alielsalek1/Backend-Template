using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TestsReusables.Auth;

public static class AuthBackdoor
{
    /// <summary>
    /// Inserts a registered user into the test Postgres database with IsEmailVerified = true.
    /// Returns the created user's Guid and the plain password used.
    /// </summary>
    public static async Task<(Guid UserId, string Password, string Username, string Email)> CreateVerifiedUserAsync(string? username = null, string? email = null, string? password = null)
    {
        return await CreateUserAsync(true, username, email, password);
    }

    /// <summary>
    /// Inserts a registered user into the test Postgres database with IsEmailVerified = false.
    /// Returns the created user's Guid and the plain password used.
    /// </summary>
    public static async Task<(Guid UserId, string Password, string Username, string Email)> CreateUnverifiedUserAsync(string? username = null, string? email = null, string? password = null)
    {
        return await CreateUserAsync(false, username, email, password);
    }

    private static async Task<(Guid UserId, string Password, string Username, string Email)> CreateUserAsync(bool isEmailVerified, string? username, string? email, string? password)
    {
        var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (string.IsNullOrEmpty(connStr)) throw new InvalidOperationException("CONNECTION_STRING environment variable is not set.");

        var userId = Guid.NewGuid();
        var pwd = password ?? "TestPassword123";
        var uname = username ?? ($"user_{userId.ToString().Substring(0, 8)}");
        var mail = email ?? ($"{uname}@example.com");

        // Hash the password using BCrypt (Application project includes BCrypt dependency)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(pwd, BCrypt.Net.BCrypt.GenerateSalt());

        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO users (id, username, password_hash, email, is_email_verified, role, address, phone_number)
                            VALUES (@id, @username, @password_hash, @email, @is_email_verified, @role, @address, @phone_number);";
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@username", uname);
        cmd.Parameters.AddWithValue("@password_hash", passwordHash);
        cmd.Parameters.AddWithValue("@email", mail);
        cmd.Parameters.AddWithValue("@is_email_verified", isEmailVerified);
        cmd.Parameters.AddWithValue("@role", 0);
        cmd.Parameters.AddWithValue("@address", string.Empty);
        cmd.Parameters.AddWithValue("@phone_number", string.Empty);

        await cmd.ExecuteNonQueryAsync();

        return (userId, pwd, uname, mail);
    }

    /// <summary>
    /// Helper to seed a confirmation token into the Redis cache used by the application.
    /// Uses the app's IDistributedCache to ensure correct formatting (avoids WRONGTYPE errors).
    /// </summary>
    public static async Task SeedConfirmationTokenAsync(Tests.Common.CustomWebApplicationFactory factory, string email, string token, int ttlSeconds = 900)
    {
        using var scope = factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        
        await cache.SetStringAsync(email, token, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
        });
    }
}
