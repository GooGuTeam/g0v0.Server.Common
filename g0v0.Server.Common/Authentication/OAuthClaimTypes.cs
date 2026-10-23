// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Authentication;

/// <summary>
/// Defines custom claim types used for OAuth token authorization.
/// </summary>
public static class OAuthClaimTypes
{
    /// <summary>
    /// The database-verified osu! user ID, independent of the JWT subject's grant-specific meaning.
    /// </summary>
    public const string UserId = "osu_user_id";

    /// <summary>
    /// The claim type for the OAuth client ID.
    /// </summary>
    public const string ClientId = "client_id";

    /// <summary>
    /// The claim type for an individual OAuth scope.
    /// </summary>
    public const string Scope = "scope";

    /// <summary>
    /// The raw JWT subject, which may identify a user or an OAuth client depending on the grant.
    /// </summary>
    public const string Subject = "sub";
}