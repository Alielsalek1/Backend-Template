using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;

namespace Tests.Auth;

public static class ResetPasswordTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostResetPasswordAsync<TResponse>(HttpClient client, ResetPasswordRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/internal-auth/reset-password", request);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
