using Application.Services.Interfaces;
using Google.Apis.Auth;
using System.Collections.Concurrent;

namespace Tests.Common;

/// <summary>
/// Test implementation of IGoogleAuthValidator that allows per-test configuration
/// </summary>
public class TestGoogleAuthValidator : IGoogleAuthValidator
{
    private static readonly ConcurrentDictionary<string, GoogleJsonWebSignature.Payload> _configuredPayloads = new();
    private static readonly ConcurrentDictionary<string, Exception> _configuredExceptions = new();

    public static void ConfigureValidToken(string idToken, string email, string name)
    {
        var payload = new GoogleJsonWebSignature.Payload
        {
            Email = email,
            Name = name,
            EmailVerified = true,
            Subject = Guid.NewGuid().ToString()
        };
        _configuredPayloads[idToken] = payload;
        _configuredExceptions.TryRemove(idToken, out _);
    }

    public static void ConfigureInvalidToken(string idToken, string errorMessage = "Invalid token")
    {
        _configuredExceptions[idToken] = new InvalidJwtException(errorMessage);
        _configuredPayloads.TryRemove(idToken, out _);
    }

    public static void Clear()
    {
        _configuredPayloads.Clear();
        _configuredExceptions.Clear();
    }

    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, GoogleJsonWebSignature.ValidationSettings validationSettings)
    {
        if (_configuredExceptions.TryGetValue(idToken, out var exception))
        {
            throw exception;
        }

        if (_configuredPayloads.TryGetValue(idToken, out var payload))
        {
            return Task.FromResult(payload);
        }

        // Default: throw invalid token exception
        throw new InvalidJwtException("Token not configured in test validator");
    }
}
