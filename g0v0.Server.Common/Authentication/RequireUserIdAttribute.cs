// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Requires the current request to be authenticated with a token containing a user id claim.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireUserIdAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireUserIdAttribute"/> class.
    /// </summary>
    public RequireUserIdAttribute()
    {
        Policy = AuthorizationPolicyNames.RequireUserId;
    }
}