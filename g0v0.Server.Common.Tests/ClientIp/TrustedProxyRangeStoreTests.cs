// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.ClientIp;
using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.ClientIp;

[TestFixture]
public class TrustedProxyRangeStoreTests
{
    [Test]
    public void MappedAddressesAreNormalizedBeforeMatchingRanges()
    {
        TrustedProxyRangeStore store = CreateStore();

        Assert.That(store.IsTrusted(IPAddress.Parse("::ffff:127.0.0.1")), Is.True);
        Assert.That(store.IsCloudflare(IPAddress.Parse("::ffff:173.245.48.5")), Is.True);
    }

    [Test]
    public void DroppingAStaleSourceRemovesItsRanges()
    {
        TrustedProxyRangeStore store = CreateStore();
        store.SetSource("https://old.test/edges", [IPNetwork.Parse("203.0.113.0/24")]);
        Assert.That(store.IsTrusted(IPAddress.Parse("203.0.113.7")), Is.True);

        store.RetainSources(["https://new.test/edges"]);

        Assert.That(store.IsTrusted(IPAddress.Parse("203.0.113.7")), Is.False);
        Assert.That(store.FetchedNetworkCount, Is.Zero);
    }

    private static TrustedProxyRangeStore CreateStore()
    {
        return new TrustedProxyRangeStore(new VerificationConfig(), NullLogger<TrustedProxyRangeStore>.Instance);
    }
}