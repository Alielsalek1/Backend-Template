using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;

namespace Tests.Auth;

public static class ResendConfirmationEmailTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostResendConfirmationEmailAsync<TResponse>(HttpClient client, ResendConfirmationEmailRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/internal-auth/resend-confirmation-email", request);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
