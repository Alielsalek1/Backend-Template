using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.ExternalAuth;

namespace Tests.Auth.ExternalLoginTests;

public static class ExternalLoginTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostGoogleLoginAsync<TResponse>(HttpClient client, GoogleAuthRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/external-auth/google-login", request);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
