// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL;

/// <summary>
/// Entity Framework Core database context for the legacy MySQL schema.
/// </summary>
public class MysqlDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MysqlDbContext"/> class.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    public MysqlDbContext(DbContextOptions<MysqlDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the users table set.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the OAuth tokens table set.
    /// </summary>
    public DbSet<OAuthToken> OAuthTokens { get; set; }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSnakeCaseNamingConvention();
    }
}