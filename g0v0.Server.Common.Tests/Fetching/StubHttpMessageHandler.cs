// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;

namespace g0v0.Server.Common.Tests.Fetching;

/// <summary>
/// In-memory <see cref="HttpMessageHandler"/> used by Fetcher tests.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, StubResponse> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, int, StubResponse> responder)
    {
        _responder = responder;
    }

    public StubHttpMessageHandler(params (string PathAndQuery, StubResponse Response)[] stubs)
    {
        _responder = (request, _) =>
        {
            StubResponse? match = stubs.FirstOrDefault(s => request.RequestUri!.PathAndQuery.StartsWith(s.PathAndQuery, StringComparison.Ordinal)).Response;
            return match ?? new StubResponse(HttpStatusCode.NotFound, "{}");
        };
    }

    public int RequestCount { get; private set; }

    public List<string> RequestUris { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        RequestUris.Add(request.RequestUri!.ToString());
        StubResponse response = _responder(request, RequestCount);

        HttpResponseMessage result = new(response.StatusCode)
        {
            Content = new StringContent(response.Content, Encoding.UTF8, response.ContentType),
        };

        if (request.Headers.Authorization != null)
        {
            result.Headers.Add("X-Auth-Header", request.Headers.Authorization.Parameter ?? string.Empty);
        }

        return Task.FromResult(result);
    }
}