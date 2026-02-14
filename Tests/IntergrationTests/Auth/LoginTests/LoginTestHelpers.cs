using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;

namespace Tests.Auth;

public static class LoginTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostLoginAsync<TResponse>(HttpClient client, LoginRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/internal-auth/login", request);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
