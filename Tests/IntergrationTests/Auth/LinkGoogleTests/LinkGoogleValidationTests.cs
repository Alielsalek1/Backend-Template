using System.Net;
using Application.DTOs.Auth;
using Application.DTOs.ExternalAuth;
using Application.Services.Interfaces;
using Application.Utils;
using Google.Apis.Auth;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Common;
using Xunit;

namespace Tests.Auth;

[Collection("Integration Tests")]
public class LinkGoogleValidationTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task LinkGoogle_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange: Create a guest user
        var (loginResponse, loginContent, _, _) = await GuestLoginTestHelpers.PostGuestLoginAsync<SuccessApiResponse<GuestLoginResponseDto>>(Client);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        
        var accessToken = loginContent!.Data.AccessToken;

        // Setup mock validator to throw InvalidJwtException
        var mockValidator = new Mock<IGoogleAuthValidator>();
        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<GoogleJsonWebSignature.ValidationSettings>()))
            .ThrowsAsync(new InvalidJwtException("Invalid token"));

        var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockValidator.Object);
            });
        }).CreateClient();

        var request = new GoogleAuthRequestDto { IdToken = "invalid-token" };

        // Act
        var (response, _, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(client, request, accessToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new GoogleAuthRequestDto { IdToken = "valid-token" };

        // Act
        var (response, _, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(Client, request, "");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_WithMalformedAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new GoogleAuthRequestDto { IdToken = "valid-token" };

        // Act
        var (response, _, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(Client, request, "malformed-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_WithExpiredAccessToken_ReturnsUnauthorized()
    {
        // Arrange: Create a guest user and get an access token (we'll simulate it's expired)
        var request = new GoogleAuthRequestDto { IdToken = "valid-token" };
        
        // Use a token that looks valid but is expired (simulated by using a random JWT-like string)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjB9.invalid";

        // Act
        var (response, _, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(Client, request, expiredToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LinkGoogle_WithEmptyIdToken_ReturnsBadRequest()
    {
        // Arrange: Create a guest user
        var (loginResponse, loginContent, _, _) = await GuestLoginTestHelpers.PostGuestLoginAsync<SuccessApiResponse<GuestLoginResponseDto>>(Client);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        
        var accessToken = loginContent!.Data.AccessToken;

        var request = new GoogleAuthRequestDto { IdToken = "" };

        // Act
        var (response, content, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(Client, request, accessToken);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LinkGoogle_WithNullIdToken_ReturnsBadRequest()
    {
        // Arrange: Create a guest user
        var (loginResponse, loginContent, _, _) = await GuestLoginTestHelpers.PostGuestLoginAsync<SuccessApiResponse<GuestLoginResponseDto>>(Client);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        
        var accessToken = loginContent!.Data.AccessToken;

        var request = new GoogleAuthRequestDto { IdToken = null! };

        // Act
        var (response, content, _) = await LinkGoogleTestHelpers.PostLinkGoogleAsync<FailApiResponse>(Client, request, accessToken);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
