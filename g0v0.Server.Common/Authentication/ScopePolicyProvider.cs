// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Dynamic authorization policy provider that resolves shared OAuth/JWT policies.
/// Falls back to the default provider for all other policy names.
/// </summary>
public class ScopePolicyProvider : IAuthorizationPolicyProvider
{
    private readonly ConfigurationManager _config;
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopePolicyProvider"/> class.
    /// </summary>
    /// <param name="options">Authorization options.</param>
    /// <param name="config">The shared general configuration.</param>
    public ScopePolicyProvider(
        IOptions<AuthorizationOptions> options,
        ConfigurationManager config)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _config = config;
    }

    /// <summary>
    /// Gets the default authorization policy.
    /// </summary>
    /// <returns>The default authorization policy.</returns>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <summary>
    /// Gets the fallback authorization policy.
    /// </summary>
    /// <returns>The fallback policy if configured; otherwise, <see langword="null"/>.</returns>
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <summary>
    /// Gets an authorization policy by name, creating OAuth policies when needed.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <returns>The resolved policy, or <see langword="null"/> if not found.</returns>
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.Equals(policyName, AuthorizationPolicyNames.ClientOnly, StringComparison.Ordinal))
        {
            return CreateClientOnlyPolicy();
        }

        if (string.Equals(policyName, AuthorizationPolicyNames.RequireUserId, StringComparison.Ordinal))
        {
            return CreateRequireUserIdPolicy();
        }

        if (!policyName.StartsWith(AuthorizationPolicyNames.ScopePrefix, StringComparison.Ordinal))
        {
            return await _fallback.GetPolicyAsync(policyName);
        }

        string[] scopes = policyName[AuthorizationPolicyNames.ScopePrefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        AuthorizationPolicyBuilder builder = new();
        builder.RequireAuthenticatedUser();
        builder.AddRequirements(new ScopeAuthorizationRequirement(scopes));
        return builder.Build();
    }

    private static AuthorizationPolicy CreateRequireUserIdPolicy()
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(OAuthClaimTypes.Subject)
            .Build();
    }

    private AuthorizationPolicy CreateClientOnlyPolicy()
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(
                OAuthClaimTypes.ClientId,
                _config.Get<GeneralConfiguration>().OsuClientId.ToString(),
                _config.Get<GeneralConfiguration>().OsuWebClientId.ToString())
            .Build();
    }
}