using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Services.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Implementations;

public class JwtTokenProvider(IConfiguration config, ILogger<JwtTokenProvider> logger) : ITokenProvider
{
    private readonly IConfiguration _config = config;
    private readonly ILogger<JwtTokenProvider> _logger = logger;

    public string GenerateAccessToken(User user)
    {
        _logger.LogInformation("Generating access token for user: {UserId}, Email: {Email}", user.Id, user.Email);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID
        };

        var keyString = _config["JWT_KEY"] ?? throw new InvalidOperationException("JWT Key is not configured in environment variables.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["JWT_ISSUER"],
            audience: _config["JWT_AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["JWT_DURATION_IN_MINUTES"] ?? "15")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}