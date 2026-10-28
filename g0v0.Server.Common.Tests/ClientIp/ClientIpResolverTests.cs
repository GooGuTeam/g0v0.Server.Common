// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.ClientIp;
using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.ClientIp;

[TestFixture]
public class ClientIpResolverTests
{
    [Test]
    public void ForwardingHeadersFromAnUntrustedPeerAreIgnored()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("81.2.69.142", forwardedFor: "1.1.1.1", realIp: "2.2.2.2");

        Assert.That(resolver.Resolve(context), Is.EqualTo("81.2.69.142"));
    }

    [Test]
    public void TrustedPeerReturnsTheRightmostUntrustedHop()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "1.1.1.1, 203.0.113.9, 10.0.0.5");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void TrustedPeerWithoutHeadersReturnsThePeer()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("192.168.1.10");

        Assert.That(resolver.Resolve(context), Is.EqualTo("192.168.1.10"));
    }

    [Test]
    public void ForwardedForWinsOverRealIpWhenBothArePresent()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "203.0.113.9", realIp: "198.51.100.7");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void RealIpIsUsedWhenForwardedForIsAbsent()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", realIp: "203.0.113.9");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void TrustedRealIpIsIgnored()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", realIp: "10.0.0.7");

        Assert.That(resolver.Resolve(context), Is.EqualTo("127.0.0.1"));
    }

    [Test]
    public void MalformedForwardedHopsAreSkipped()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "unknown, 203.0.113.9");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void AllTrustedHopsFallBackToTheLeftmost()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "10.0.0.1, 172.16.0.2");

        Assert.That(resolver.Resolve(context), Is.EqualTo("10.0.0.1"));
    }

    [Test]
    public void Ipv4MappedPeerIsNormalizedBeforeTheTrustCheck()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("::ffff:127.0.0.1", forwardedFor: "203.0.113.9");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void Ipv4MappedHopIsNormalized()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "::ffff:203.0.113.9");

        Assert.That(resolver.Resolve(context), Is.EqualTo("203.0.113.9"));
    }

    [Test]
    public void Ipv6ClientIsReturnedInCompressedForm()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("::1", forwardedFor: "2001:db8:0:0::1");

        Assert.That(resolver.Resolve(context), Is.EqualTo("2001:db8::1"));
    }

    [Test]
    public void CloudflareEdgeUsesConnectingIp()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request(
            "127.0.0.1",
            forwardedFor: "10.10.10.10, 203.0.113.9, 173.245.48.5",
            cloudflareConnectingIp: "8.8.4.4");

        Assert.That(resolver.Resolve(context), Is.EqualTo("8.8.4.4"));
    }

    [Test]
    public void CloudflareEdgeWithoutConnectingIpFallsBackToTheEdge()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "10.10.10.10, 203.0.113.9, 173.245.48.5");

        Assert.That(resolver.Resolve(context), Is.EqualTo("173.245.48.5"));
    }

    [Test]
    public void FetchedSourceRangeResolvesTheClientBehindTheCdn()
    {
        VerificationConfig config = new();
        TrustedProxyRangeStore store = new(config, NullLogger<TrustedProxyRangeStore>.Instance);
        store.SetSource("https://cdn.test/edges", [IPNetwork.Parse("203.0.113.0/24")]);
        ClientIpResolver resolver = new(store, NullLogger<ClientIpResolver>.Instance);
        DefaultHttpContext context = Request("127.0.0.1", forwardedFor: "198.51.100.7, 203.0.113.20");

        Assert.That(resolver.Resolve(context), Is.EqualTo("198.51.100.7"));
    }

    [Test]
    public void MissingPeerFallsBackToLoopback()
    {
        ClientIpResolver resolver = CreateResolver();
        DefaultHttpContext context = Request(peer: null, forwardedFor: "203.0.113.9");

        Assert.That(resolver.Resolve(context), Is.EqualTo("127.0.0.1"));
    }

    private static ClientIpResolver CreateResolver()
    {
        return new ClientIpResolver(
            new TrustedProxyRangeStore(new VerificationConfig(), NullLogger<TrustedProxyRangeStore>.Instance),
            NullLogger<ClientIpResolver>.Instance);
    }

    private static DefaultHttpContext Request(
        string? peer,
        string? forwardedFor = null,
        string? realIp = null,
        string? cloudflareConnectingIp = null)
    {
        DefaultHttpContext context = new();
        if (peer is not null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        }

        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        if (realIp is not null)
        {
            context.Request.Headers["X-Real-IP"] = realIp;
        }

        if (cloudflareConnectingIp is not null)
        {
            context.Request.Headers["CF-Connecting-IP"] = cloudflareConnectingIp;
        }

        return context;
    }
}