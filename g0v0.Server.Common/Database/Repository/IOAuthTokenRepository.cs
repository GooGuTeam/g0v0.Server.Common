// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for OAuth token records.
/// </summary>
public interface IOAuthTokenRepository
{
    /// <summary>
    /// Gets a valid OAuth token record by access token value.
    /// </summary>
    /// <param name="token">The raw access token.</param>
    /// <returns>The matching token record if found and valid; otherwise, <see langword="null"/>.</returns>
    Task<OAuthToken?> GetByAccessTokenAsync(string token);

    /// <summary>
    /// Checks whether an access token exists and is still valid.
    /// </summary>
    /// <param name="token">The raw access token.</param>
    /// <returns><see langword="true"/> if the token exists and is not expired; otherwise, <see langword="false"/>.</returns>
    Task<bool> CheckAccessTokenIsValidAsync(string token);
}