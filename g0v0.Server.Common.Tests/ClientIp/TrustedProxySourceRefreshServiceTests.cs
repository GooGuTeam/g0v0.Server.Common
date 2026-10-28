// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.ClientIp;
using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.ClientIp;

[TestFixture]
public class TrustedProxySourceRefreshServiceTests
{
    [Test]
    public async Task SuccessfulRefreshMakesPublishedEdgesTrusted()
    {
        VerificationConfig config = new() { TrustedProxySources = ["https://cdn.test/edges"] };
        TrustedProxyRangeStore store = new(config, NullLogger<TrustedProxyRangeStore>.Instance);
        using StubHttpClientFactory factory = new(_ => Response(HttpStatusCode.OK, "[\"203.0.113.20\"]"));
        using TrustedProxySourceRefreshService service = CreateService(config, store, factory);

        await service.RefreshAsync(CancellationToken.None);

        Assert.That(store.IsTrusted(IPAddress.Parse("203.0.113.20")), Is.True);
        Assert.That(store.FetchedNetworkCount, Is.EqualTo(1));
    }

    [Test]
    public async Task FailedRefreshKeepsThePreviousSnapshot()
    {
        Flag fail = new();
        VerificationConfig config = new() { TrustedProxySources = ["https://cdn.test/edges"] };
        TrustedProxyRangeStore store = new(config, NullLogger<TrustedProxyRangeStore>.Instance);
        using StubHttpClientFactory factory = new(_ => fail.Value
            ? Response(HttpStatusCode.InternalServerError, string.Empty)
            : Response(HttpStatusCode.OK, "[\"203.0.113.20\"]"));
        using TrustedProxySourceRefreshService service = CreateService(config, store, factory);

        await service.RefreshAsync(CancellationToken.None);
        fail.Value = true;
        await service.RefreshAsync(CancellationToken.None);

        Assert.That(store.IsTrusted(IPAddress.Parse("203.0.113.20")), Is.True);
    }

    [Test]
    public async Task EmptyRefreshKeepsThePreviousSnapshot()
    {
        Flag empty = new();
        VerificationConfig config = new() { TrustedProxySources = ["https://cdn.test/edges"] };
        TrustedProxyRangeStore store = new(config, NullLogger<TrustedProxyRangeStore>.Instance);
        using StubHttpClientFactory factory = new(_ => Response(HttpStatusCode.OK, empty.Value ? "[]" : "[\"203.0.113.20\"]"));
        using TrustedProxySourceRefreshService service = CreateService(config, store, factory);

        await service.RefreshAsync(CancellationToken.None);
        empty.Value = true;
        await service.RefreshAsync(CancellationToken.None);

        Assert.That(store.IsTrusted(IPAddress.Parse("203.0.113.20")), Is.True);
    }

    [Test]
    public async Task CatchAllRangeIsRejected()
    {
        VerificationConfig config = new() { TrustedProxySources = ["https://cdn.test/edges"] };
        TrustedProxyRangeStore store = new(config, NullLogger<TrustedProxyRangeStore>.Instance);
        using StubHttpClientFactory factory = new(_ => Response(HttpStatusCode.OK, "0.0.0.0/0\n198.51.100.0/24"));
        using TrustedProxySourceRefreshService service = CreateService(config, store, factory);

        await service.RefreshAsync(CancellationToken.None);

        Assert.That(store.IsTrusted(IPAddress.Parse("198.51.100.7")), Is.True);
        Assert.That(store.IsTrusted(IPAddress.Parse("8.8.8.8")), Is.False);
        Assert.That(store.TrustsEveryAddress, Is.False);
    }

    private static TrustedProxySourceRefreshService CreateService(
        VerificationConfig config,
        TrustedProxyRangeStore store,
        IHttpClientFactory factory)
    {
        return new TrustedProxySourceRefreshService(config, store, factory, NullLogger<TrustedProxySourceRefreshService>.Instance);
    }

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode) { Content = new StringContent(body) };
    }

    private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> respond) : IHttpClientFactory, IDisposable
    {
        private readonly HttpMessageHandler _handler = new StubHandler(respond);

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }

        public void Dispose()
        {
            _handler.Dispose();
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }

    private sealed class Flag
    {
        public bool Value { get; set; }
    }
}