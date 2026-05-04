// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IRelationshipRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class RelationshipRepository(PostgreSqlDbContext context) : IRelationshipRepository, IPostgreSqlRepository
{
    /// <inheritdoc/>
    public async Task<int[]> GetAllFriendIds(long userId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId && r.Type == RelationshipType.Follow)
            .Select(r => (int)r.TargetId)
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relationship>> GetByUserIdAsync(long userId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relationship>> GetByTargetIdAsync(long targetId)
    {
        return await context.Relationships
            .Where(r => r.TargetId == targetId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Relationship?> GetRelationshipAsync(long userId, long targetId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId && r.TargetId == targetId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsFollowingAsync(long userId, long targetId)
    {
        return await context.Relationships
            .AnyAsync(r => r.UserId == userId && r.TargetId == targetId && r.Type == RelationshipType.Follow)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsBlockedAsync(long userId, long targetId)
    {
        return await context.Relationships
            .AnyAsync(r => r.UserId == userId && r.TargetId == targetId && r.Type == RelationshipType.Block)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CreateAsync(Relationship relationship)
    {
        await context.Relationships.AddAsync(relationship).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Relationship relationship)
    {
        context.Relationships.Update(relationship);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Relationship relationship)
    {
        context.Relationships.Remove(relationship);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}