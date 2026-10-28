// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using g0v0.Server.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.ClientIp;

/// <summary>
/// Holds the proxy ranges whose forwarding headers are trusted, including CDN ranges refreshed at
/// runtime.
/// </summary>
/// <remarks>
/// The static ranges come from <see cref="VerificationConfig.TrustedProxies"/>; the fetched ranges come
/// from <see cref="VerificationConfig.TrustedProxySources"/> and are replaced atomically so a refresh
/// never exposes a partially updated set. Publishers rotate edge addresses, so the fetched set is
/// deliberately kept after a failed refresh instead of being cleared.
/// </remarks>
public sealed partial class TrustedProxyRangeStore
{
    private static readonly string[] CloudflareRanges =
    [
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];

    private readonly IPNetwork[] _cloudflare;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, IPNetwork[]> _sources = new(StringComparer.Ordinal);
    private readonly IPNetwork[] _static;
    private IPNetwork[] _fetched = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustedProxyRangeStore"/> class.
    /// </summary>
    /// <param name="config">The client IP configuration.</param>
    /// <param name="logger">The logger used for malformed configuration values.</param>
    public TrustedProxyRangeStore(VerificationConfig config, ILogger<TrustedProxyRangeStore> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        _static = Parse(config.TrustedProxies, logger);
        _cloudflare = ProxyRangeParser.ParsePayload(string.Join('\n', CloudflareRanges));
    }

    /// <summary>
    /// Gets the ranges configured through <see cref="VerificationConfig.TrustedProxies"/>.
    /// </summary>
    public IReadOnlyList<IPNetwork> StaticNetworks => _static;

    /// <summary>
    /// Gets the number of currently loaded ranges from published sources.
    /// </summary>
    public int FetchedNetworkCount => Volatile.Read(ref _fetched).Length;

    /// <summary>
    /// Gets a value indicating whether any trusted range covers every address.
    /// </summary>
    public bool TrustsEveryAddress => _static.Concat(Volatile.Read(ref _fetched)).Any(network => network.PrefixLength == 0);

    /// <summary>
    /// Determines whether <paramref name="address"/> is a trusted proxy.
    /// </summary>
    /// <param name="address">The peer address.</param>
    /// <returns><see langword="true"/> when the peer may supply forwarding headers.</returns>
    public bool IsTrusted(IPAddress address)
    {
        IPAddress normalized = Normalize(address);
        return Contains(_static, normalized) || Contains(Volatile.Read(ref _fetched), normalized);
    }

    /// <summary>
    /// Determines whether <paramref name="address"/> belongs to a published Cloudflare edge range.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns><see langword="true"/> when the address is a Cloudflare edge.</returns>
    public bool IsCloudflare(IPAddress address)
    {
        return Contains(_cloudflare, Normalize(address));
    }

    /// <summary>
    /// Replaces the ranges published by one source.
    /// </summary>
    /// <param name="source">The absolute source URL used as the cache key.</param>
    /// <param name="networks">The ranges parsed from the source.</param>
    public void SetSource(string source, IPNetwork[] networks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(networks);
        lock (_lock)
        {
            _sources[source] = networks;
            Rebuild();
        }
    }

    /// <summary>
    /// Drops ranges published by sources that are no longer configured.
    /// </summary>
    /// <param name="sources">The currently configured source URLs.</param>
    public void RetainSources(IEnumerable<string> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        HashSet<string> configured = new(sources, StringComparer.Ordinal);
        lock (_lock)
        {
            string[] stale = _sources.Keys.Where(source => !configured.Contains(source)).ToArray();
            foreach (string source in stale)
            {
                _sources.Remove(source);
            }

            if (stale.Length > 0)
            {
                Rebuild();
            }
        }
    }

    private static IPNetwork[] Parse(string[] values, ILogger logger)
    {
        List<IPNetwork> networks = [];
        foreach (string value in values)
        {
            if (ProxyRangeParser.TryParse(value, out IPNetwork network))
            {
                networks.Add(network);
            }
            else
            {
                Log.MalformedRange(logger, value);
            }
        }

        return [.. networks.Distinct()];
    }

    private static bool Contains(IPNetwork[] networks, IPAddress address)
    {
        return networks.Any(network => network.Contains(address));
    }

    private static IPAddress Normalize(IPAddress address)
    {
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    private void Rebuild()
    {
        IPNetwork[] merged = _sources.Values
            .SelectMany(networks => networks)
            .Distinct()
            .ToArray();
        Volatile.Write(ref _fetched, merged);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Ignoring malformed trusted proxy range: {Value}")]
        internal static partial void MalformedRange(ILogger logger, string value);
    }
}