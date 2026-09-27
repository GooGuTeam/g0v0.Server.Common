// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// MySQL-backed implementation of <see cref="IRelationshipRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class RelationshipRepository(MysqlDbContext context) : IRelationshipRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<int[]> GetAllFriendIds(int userId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId && r.Type == RelationshipType.Follow)
            .Select(r => r.TargetId)
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relationship>> GetByUserIdAsync(int userId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relationship>> GetByTargetIdAsync(int targetId)
    {
        return await context.Relationships
            .Where(r => r.TargetId == targetId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Relationship?> GetRelationshipAsync(int userId, int targetId)
    {
        return await context.Relationships
            .Where(r => r.UserId == userId && r.TargetId == targetId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsFollowingAsync(int userId, int targetId)
    {
        return await context.Relationships
            .AnyAsync(r => r.UserId == userId && r.TargetId == targetId && r.Type == RelationshipType.Follow)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsBlockedAsync(int userId, int targetId)
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
    /// <remarks>
    /// The legacy table is keyed by (<see cref="Relationship.UserId"/>,
    /// <see cref="Relationship.TargetId"/>, <see cref="Relationship.Type"/>), so a
    /// type change cannot be updated in place: the stored rows of the pair are
    /// replaced with the requested state instead. Pass an untracked instance
    /// holding the desired state.
    /// </remarks>
    public async Task UpdateAsync(Relationship relationship)
    {
        List<Relationship> existing = await context.Relationships
            .Where(r => r.UserId == relationship.UserId && r.TargetId == relationship.TargetId)
            .ToListAsync()
            .ConfigureAwait(false);
        context.Relationships.RemoveRange(existing);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await context.Relationships.AddAsync(relationship).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Relationship relationship)
    {
        context.Relationships.Remove(relationship);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}