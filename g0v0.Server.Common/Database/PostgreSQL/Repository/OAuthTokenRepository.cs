// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IOAuthTokenRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class OAuthTokenRepository(PostgreSqlDbContext context) : IOAuthTokenRepository, IPostgreSqlRepository
{
    /// <inheritdoc />
    public async Task<OAuthToken?> GetByAccessTokenAsync(string token)
    {
        return await context.OAuthTokens.Where(t => t.AccessToken == token && t.ExpiresAt > DateTimeOffset.UtcNow)
            .FirstOrDefaultAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CheckAccessTokenIsValidAsync(string token)
    {
        return await context.OAuthTokens.AnyAsync(t => t.AccessToken == token && t.ExpiresAt > DateTimeOffset.UtcNow).ConfigureAwait(false);
    }
}