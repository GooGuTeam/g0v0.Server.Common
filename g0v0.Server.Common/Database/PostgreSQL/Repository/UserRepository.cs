// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IUserRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public class UserRepository(PostgreSqlDbContext context) : IUserRepository, IPostgreSqlRepository
{
    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(int userId)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyList<int> userIds)
    {
        return userIds.Count == 0
            ? []
            : await context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync()
                .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string?> GetUsernameByIdAsync(int userId)
    {
        return await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Username == username)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Email == email)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await context.Users
            .AnyAsync(u => u.Username == username)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(User user)
    {
        await context.Users.AddAsync(user).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(User user, IReadOnlyList<int> statisticsModes, CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        await context.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (statisticsModes.Count != 0)
        {
            await context.UserStatistics.AddRangeAsync(
                statisticsModes.Select(mode => new UserStatistics { UserId = user.Id, Mode = mode }),
                cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}