// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace g0v0.Server.Common.ClientIp;

/// <summary>
/// Parses the proxy ranges supplied by configuration and by published CDN edge lists.
/// </summary>
public static partial class ProxyRangeParser
{
    /// <summary>
    /// Parses one CIDR range or bare address.
    /// </summary>
    /// <param name="value">The configured or published value.</param>
    /// <param name="network">The parsed range.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is an address or CIDR range.</returns>
    public static bool TryParse(string? value, out IPNetwork network)
    {
        network = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string text = value.Trim();
        if (IPAddress.TryParse(text, out IPAddress? address))
        {
            network = Normalize(new IPNetwork(address, address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32));
            return true;
        }

        if (!IPNetwork.TryParse(text, out network))
        {
            return false;
        }

        network = Normalize(network);
        return true;
    }

    /// <summary>
    /// Extracts every address and CIDR range from a published edge list.
    /// </summary>
    /// <param name="payload">The response body, in plain text, JSON or CSV form.</param>
    /// <returns>The distinct ranges found in <paramref name="payload"/>.</returns>
    /// <remarks>
    /// Publishers disagree about the format: Cloudflare serves plain text, AWS serves JSON and
    /// Bunny serves a JSON array of bare addresses. Pulling out every address-shaped token handles
    /// all three without assuming one shape.
    /// </remarks>
    public static IPNetwork[] ParsePayload(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return [];
        }

        HashSet<IPNetwork> networks = [];
        foreach (Match match in SourceTokenPattern().Matches(payload))
        {
            if (TryParse(match.Value, out IPNetwork network))
            {
                networks.Add(network);
            }
        }

        return [.. networks];
    }

    private static IPNetwork Normalize(IPNetwork network)
    {
        return network.BaseAddress.IsIPv4MappedToIPv6 && network.PrefixLength >= 96
            ? new IPNetwork(network.BaseAddress.MapToIPv4(), network.PrefixLength - 96)
            : network;
    }

    [GeneratedRegex(@"[0-9A-Fa-f:.]{2,45}(?:/\d{1,3})?", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex SourceTokenPattern();
}