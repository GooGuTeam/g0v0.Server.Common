// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace g0v0.Server.Common.ClientIp;

/// <summary>
/// Resolves the address of the client that opened a request, ignoring forwarding headers unless the
/// immediate peer is a trusted proxy.
/// </summary>
public interface IClientIpResolver
{
    /// <summary>
    /// Resolves the client address for <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The request to resolve.</param>
    /// <returns>The normalized client address, or <c>127.0.0.1</c> when the peer is unavailable.</returns>
    /// <remarks>
    /// The returned value is always a parseable IPv4 or IPv6 address. IPv4-mapped IPv6 addresses
    /// are returned in their IPv4 form.
    /// </remarks>
    string Resolve(HttpContext context);
}