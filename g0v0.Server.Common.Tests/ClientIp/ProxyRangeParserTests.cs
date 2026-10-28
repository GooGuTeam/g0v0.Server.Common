// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.ClientIp;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.ClientIp;

[TestFixture]
public class ProxyRangeParserTests
{
    [Test]
    public void PublishedJsonWithBareAddressesYieldsSingleHostRanges()
    {
        IPNetwork[] networks = ProxyRangeParser.ParsePayload("[\"203.0.113.20\",\"2001:db8::1\"]");

        Assert.That(networks, Has.Length.EqualTo(2));
        Assert.That(networks, Does.Contain(IPNetwork.Parse("203.0.113.20/32")));
        Assert.That(networks, Does.Contain(IPNetwork.Parse("2001:db8::1/128")));
    }

    [Test]
    public void PlainTextAndCsvPayloadsYieldEveryRange()
    {
        IPNetwork[] networks = ProxyRangeParser.ParsePayload("173.245.48.0/20\n103.21.244.0/22, 2400:cb00::/32");

        Assert.That(networks, Is.EquivalentTo<IPNetwork>(
        [
            IPNetwork.Parse("173.245.48.0/20"),
            IPNetwork.Parse("103.21.244.0/22"),
            IPNetwork.Parse("2400:cb00::/32"),
        ]));
    }

    [Test]
    public void DuplicateTokensAreCollapsed()
    {
        IPNetwork[] networks = ProxyRangeParser.ParsePayload("203.0.113.20 203.0.113.20/32");

        Assert.That(networks, Has.Length.EqualTo(1));
        Assert.That(networks[0], Is.EqualTo(IPNetwork.Parse("203.0.113.20/32")));
    }

    [Test]
    public void MalformedTokensAreIgnored()
    {
        IPNetwork[] networks = ProxyRangeParser.ParsePayload("{\"ranges\":[],\"note\":\"not-an-address\",\"bad\":\"999.1.1.1\"}");

        Assert.That(networks, Is.Empty);
    }

    [Test]
    public void MappedAddressesAreNormalizedToIpv4()
    {
        Assert.That(ProxyRangeParser.TryParse("::ffff:203.0.113.9", out IPNetwork host), Is.True);
        Assert.That(host, Is.EqualTo(IPNetwork.Parse("203.0.113.9/32")));

        Assert.That(ProxyRangeParser.TryParse("::ffff:203.0.113.0/120", out IPNetwork range), Is.True);
        Assert.That(range, Is.EqualTo(IPNetwork.Parse("203.0.113.0/24")));
    }

    [Test]
    public void OutOfRangePrefixLengthIsRejected()
    {
        Assert.That(ProxyRangeParser.TryParse("0.0.0.0/33", out _), Is.False);
    }
}