// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Defines shared authorization policy names for OAuth/JWT endpoints.
/// </summary>
public static class AuthorizationPolicyNames
{
    /// <summary>
    /// The policy that only allows authenticated client credentials tokens.
    /// </summary>
    public const string ClientOnly = "ClientOnly";

    /// <summary>
    /// The policy that requires an authenticated token with a subject claim.
    /// </summary>
    public const string RequireUserId = "RequireUserId";

    /// <summary>
    /// The prefix used to encode scope-based policies.
    /// </summary>
    public const string ScopePrefix = "Scope:";

    /// <summary>
    /// Builds a scope policy name for the provided scopes.
    /// </summary>
    /// <param name="scopes">The required scopes.</param>
    /// <returns>The encoded policy name.</returns>
    public static string RequireScope(params string[] scopes) => $"{ScopePrefix}{string.Join(",", scopes)}";
}