// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.ClientIp;

/// <summary>
/// Resolves the client address from the connection peer and the forwarding headers, honouring the
/// headers only when the peer is a trusted proxy.
/// </summary>
/// <remarks>
/// <para>
/// The peer is read from <see cref="HttpContext.Connection"/>, so forwarded-header processing must
/// stay disabled: if middleware rewrites <c>RemoteIpAddress</c> to the value in
/// <c>X-Forwarded-For</c>, this resolver can no longer tell whether the headers came from a proxy
/// or from the client itself.
/// </para>
/// <para>
/// When the peer is trusted, <c>X-Forwarded-For</c> is walked from right to left and the first
/// address outside the trusted ranges wins. The rightmost entries were appended by this proxy, so a
/// client cannot forge them by pre-filling the header. Cloudflare is handled separately because its
/// edge appends to a client-controlled list; when the last hop is a Cloudflare edge,
/// <c>CF-Connecting-IP</c> is authoritative.
/// </para>
/// </remarks>
/// <param name="ranges">The trusted proxy ranges.</param>
/// <param name="logger">The logger for identity misconfiguration warnings.</param>
public sealed partial class ClientIpResolver(TrustedProxyRangeStore ranges, ILogger<ClientIpResolver> logger) : IClientIpResolver
{
    private const int MaxIdentityWarnings = 8;
    private const string FallbackAddress = "127.0.0.1";
    private readonly Lock _warningLock = new();
    private readonly HashSet<string> _warnedIdentityPeers = new(StringComparer.Ordinal);
    private bool _warnedRewrittenByServer;

    /// <inheritdoc />
    public string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        IPAddress? peer = Normalize(context.Connection.RemoteIpAddress);
        if (peer is null)
        {
            return FallbackAddress;
        }

        IPAddress[] hops = ParseHops(Header(context, "X-Forwarded-For"));
        if (!ranges.IsTrusted(peer))
        {
            // Forwarding headers from an untrusted peer are attacker-controlled.
            if (Array.IndexOf(hops, peer) >= 0)
            {
                WarnRewrittenByServer();
            }

            return Format(peer);
        }

        if (hops.Length > 0)
        {
            if (ranges.IsCloudflare(hops[^1]))
            {
                IPAddress? cloudflare = ParseAddress(Header(context, "CF-Connecting-IP"));
                if (cloudflare is not null)
                {
                    return Format(cloudflare);
                }
            }

            for (int i = hops.Length - 1; i >= 0; i--)
            {
                if (!ranges.IsTrusted(hops[i]))
                {
                    return Format(hops[i]);
                }
            }

            return Format(hops[0]);
        }

        IPAddress? realIp = ParseAddress(Header(context, "X-Real-IP"));
        if (realIp is not null)
        {
            if (ranges.IsCloudflare(realIp))
            {
                IPAddress? cloudflare = ParseAddress(Header(context, "CF-Connecting-IP"));
                if (cloudflare is not null)
                {
                    return Format(cloudflare);
                }
            }
            else if (!ranges.IsTrusted(realIp))
            {
                return Format(realIp);
            }
        }

        if (!IPAddress.IsLoopback(peer))
        {
            WarnIdentityCollapse(peer);
        }

        return Format(peer);
    }

    private static string? Header(HttpContext context, string name)
    {
        return context.Request.Headers[name].ToString();
    }

    private static IPAddress[] ParseHops(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',')
                .Select(ParseAddress)
                .OfType<IPAddress>()
                .ToArray();
    }

    private static IPAddress? ParseAddress(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value.Trim(), out IPAddress? address)
            ? null
            : Normalize(address);
    }

    private static IPAddress? Normalize(IPAddress? address)
    {
        // Proxies on dual-stack sockets report IPv4 peers as ::ffff:a.b.c.d.
        return address is { IsIPv4MappedToIPv6: true } ? address.MapToIPv4() : address;
    }

    private static string Format(IPAddress address)
    {
        return address.ToString();
    }

    private void WarnIdentityCollapse(IPAddress peer)
    {
        string key = Format(peer);
        lock (_warningLock)
        {
            if (_warnedIdentityPeers.Count >= MaxIdentityWarnings || !_warnedIdentityPeers.Add(key))
            {
                return;
            }
        }

        Log.IdentityCollapse(logger, key);
    }

    private void WarnRewrittenByServer()
    {
        lock (_warningLock)
        {
            if (_warnedRewrittenByServer)
            {
                return;
            }

            _warnedRewrittenByServer = true;
        }

        Log.RewrittenByServer(logger);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Client identity falls back to proxy address {Peer} without forwarding headers; rate limits and trusted devices are shared between its clients. Point TrustedProxies at the proxy and have it send X-Forwarded-For or X-Real-IP.")]
        internal static partial void IdentityCollapse(ILogger logger, string peer);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "The forwarded headers already contain the peer address, so an upstream server rewrote Connection.RemoteIpAddress; disable forwarded-header processing and let ClientIpResolver validate the peer instead.")]
        internal static partial void RewrittenByServer(ILogger logger);
    }
}