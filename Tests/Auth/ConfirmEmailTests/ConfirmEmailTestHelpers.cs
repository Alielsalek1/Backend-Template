using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.InternalAuth;

namespace Tests.Auth;

public static class ConfirmEmailTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostConfirmEmailAsync<TResponse>(HttpClient client, ConfirmEmailRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/internal-auth/confirm-email", request);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
