// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Creates isolated in-memory <see cref="DbContext"/> instances for the backend under test.
/// </summary>
internal static class RepositoryTestContexts
{
    /// <summary>
    /// Creates a context over a fresh in-memory database so fixtures never share state.
    /// </summary>
    /// <param name="backend">The backend to create a context for.</param>
    /// <returns>The created context, owned and disposed by the caller.</returns>
    internal static DbContext Create(RepositoryBackend backend)
    {
        return backend switch
        {
            RepositoryBackend.MySql => new MysqlDbContext(
                new DbContextOptionsBuilder<MysqlDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options),
            _ => new PostgreSqlDbContext(
                new DbContextOptionsBuilder<PostgreSqlDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options),
        };
    }
}