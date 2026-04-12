using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResX.IntegrationTests.Common.Helpers;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Sets a Bearer JWT token on the client's default headers.</summary>
    public static HttpClient WithBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Clears the Authorization header.</summary>
    public static HttpClient WithoutAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

    /// <summary>Reads the HTTP response body as the specified type. Throws if deserialization fails.</summary>
    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)
               ?? throw new InvalidOperationException(
                   $"Could not deserialize response to {typeof(T).Name}. Body: {content}");
    }

    /// <summary>
    /// Posts JSON and returns the response. Convenience wrapper over PostAsJsonAsync
    /// that uses the shared camelCase options.
    /// </summary>
    public static Task<HttpResponseMessage> PostJsonAsync<T>(
        this HttpClient client, string url, T payload)
        => client.PostAsJsonAsync(url, payload, JsonOptions);

    /// <summary>Puts JSON and returns the response.</summary>
    public static Task<HttpResponseMessage> PutJsonAsync<T>(
        this HttpClient client, string url, T payload)
        => client.PutAsJsonAsync(url, payload, JsonOptions);

    /// <summary>Patches JSON and returns the response.</summary>
    public static Task<HttpResponseMessage> PatchJsonAsync<T>(
        this HttpClient client, string url, T payload)
    {
        var content = JsonContent.Create(payload, options: JsonOptions);
        return client.PatchAsync(url, content);
    }

    /// <summary>
    /// Extracts a cookie value from the Set-Cookie response headers.
    /// </summary>
    public static string? GetCookieValue(this HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{cookieName}=";
        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return cookie[prefix.Length..].Split(';')[0];
            }
        }

        return null;
    }
}
