using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.InternalAuth;

namespace Tests.Auth;

public static class RegisterationTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PostRegisterAsync<TResponse>(HttpClient client, RegisterRequestDto request, string? idempotencyKey = "AUTO")
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/internal-auth/register")
        {
            Content = JsonContent.Create(request)
        };

        if (idempotencyKey == "AUTO")
        {
            message.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        }
        else if (idempotencyKey != null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        var response = await client.SendAsync(message);
        var json = await response.Content.ReadAsStringAsync();
        var content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        return (response, content, json);
    }
}
