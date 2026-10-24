// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Handles <see cref="ScopeAuthorizationRequirement"/> by checking the user's
/// OAuth scope claims. Tokens from known clients (osu! client / web client)
/// bypass scope checks, mirroring the Python <c>_validate_token</c> logic.
/// </summary>
public class ScopeAuthorizationHandler(ConfigManager config)
    : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    /// <summary>
    /// Evaluates whether the current user satisfies the required OAuth scopes.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The scope requirement.</param>
    /// <returns>A completed task.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeAuthorizationRequirement requirement)
    {
        GeneralConfig generalConfig = config.Get<GeneralConfig>();

        System.Security.Claims.Claim? clientIdClaim = context.User.FindFirst(OAuthClaimTypes.ClientId);
        if (clientIdClaim != null)
        {
            string clientId = clientIdClaim.Value;
            if (string.Equals(clientId, generalConfig.OsuClientId.ToString(), StringComparison.Ordinal) ||
string.Equals(clientId, generalConfig.OsuWebClientId.ToString(), StringComparison.Ordinal))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        HashSet<string> scopeClaims = context.User.FindAll(OAuthClaimTypes.Scope)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (scopeClaims.Contains("*") ||
            requirement.Scopes.All(s => scopeClaims.Contains(s)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}