using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.User;

namespace Tests.User;

public static class UserTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<(HttpResponseMessage Response, TResponse? Content, string Json)>
        PatchProfileAsync<TResponse>(HttpClient client, UpdateUserRequestDto request, string? idempotencyKey = null)
    {
        using var message = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/users/profile")
        {
            Content = JsonContent.Create(request)
        };

        if (idempotencyKey != null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        else
        {
            message.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        }

        var response = await client.SendAsync(message);
        var json = await response.Content.ReadAsStringAsync();
        TResponse? content = default;
        if (!string.IsNullOrWhiteSpace(json))
        {
            content = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
        }
        return (response, content, json);
    }
}
