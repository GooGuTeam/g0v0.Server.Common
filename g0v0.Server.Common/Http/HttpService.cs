// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Http;

/// <summary>
/// Default implementation of <see cref="IHttpService"/> using <see cref="HttpClient"/>.
/// </summary>
/// <param name="httpClient">The HTTP client.</param>
public class HttpService(HttpClient httpClient) : IHttpService
{
    /// <inheritdoc/>
    public async Task<T> GetJsonAsync<T>(string url, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        ApplyAuthorization(request, bearerToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<T>(response, url, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T> PostJsonAsync<T>(string url, object payload, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        string json = JsonConvert.SerializeObject(payload);
        HttpRequestMessage request = new(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        using (request)
        {
            ApplyAuthorization(request, bearerToken);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await ReadJsonAsync<T>(response, url, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<string> PostFormAsync(string url, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken = default)
    {
        using FormUrlEncodedContent content = new(form);
        using HttpResponseMessage response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return !response.IsSuccessStatusCode
            ? throw new HttpServiceException($"POST {url} failed with HTTP {(int)response.StatusCode}.")
            : body;
    }

    /// <inheritdoc/>
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return !response.IsSuccessStatusCode
            ? throw new HttpServiceException($"GET {url} failed with HTTP {(int)response.StatusCode}.")
            : body;
    }

    /// <inheritdoc/>
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return httpClient.SendAsync(request, cancellationToken);
    }

    private static void ApplyAuthorization(HttpRequestMessage request, string? bearerToken)
    {
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpServiceException($"Request to {url} failed with HTTP {(int)response.StatusCode}.", (int)response.StatusCode);
        }

        T? value = JsonConvert.DeserializeObject<T>(body);
        return value ?? throw new HttpServiceException($"Request to {url} returned an invalid payload.");
    }
}