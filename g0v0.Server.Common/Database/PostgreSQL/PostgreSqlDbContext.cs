// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.PostgreSQL;

/// <summary>
/// Entity Framework Core database context for the PostgreSQL schema.
/// </summary>
public class PostgreSqlDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDbContext"/> class.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    public PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the users table set.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the OAuth tokens table set.
    /// </summary>
    public DbSet<OAuthToken> OAuthTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the relationships table set.
    /// </summary>
    public DbSet<Relationship> Relationships { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSnakeCaseNamingConvention();
    }
}