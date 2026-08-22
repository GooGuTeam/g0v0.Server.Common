// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Http;

/// <summary>
/// Provides simplified HTTP operations for JSON and form payloads.
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Sends a GET request and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <param name="bearerToken">An optional bearer token for authorization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
    Task<T> GetJsonAsync<T>(string url, string? bearerToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a JSON body and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <param name="payload">The JSON payload to send.</param>
    /// <param name="bearerToken">An optional bearer token for authorization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
    Task<T> PostJsonAsync<T>(string url, object payload, string? bearerToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a form-urlencoded body.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <param name="form">The form fields to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw response body.</returns>
    Task<string> PostFormAsync(string url, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a GET request and returns the raw response body.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw response body.</returns>
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request and returns the raw response message for advanced handling.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response message.</returns>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}