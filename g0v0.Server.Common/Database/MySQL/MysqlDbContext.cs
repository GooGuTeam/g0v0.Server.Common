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
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the OAuth tokens table set.
    /// </summary>
    public DbSet<OAuthToken> OAuthTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the relationships table set.
    /// </summary>
    public DbSet<Relationship> Relationships { get; set; } = null!;

    /// <summary>
    /// Gets or sets the beatmaps table set.
    /// </summary>
    public DbSet<Beatmap> Beatmaps { get; set; } = null!;

    /// <summary>
    /// Gets or sets the scores table set.
    /// </summary>
    public DbSet<Score> Scores { get; set; } = null!;

    /// <summary>
    /// Gets or sets the score tokens table set.
    /// </summary>
    public DbSet<ScoreToken> ScoreTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the multiplayer rooms table set.
    /// </summary>
    public DbSet<Room> Rooms { get; set; } = null!;

    /// <summary>
    /// Gets or sets the room playlist items table set.
    /// </summary>
    public DbSet<Playlist> Playlists { get; set; } = null!;

    /// <summary>
    /// Gets or sets the room participation history table set.
    /// </summary>
    public DbSet<RoomParticipatedUser> RoomParticipatedUsers { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSnakeCaseNamingConvention();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MysqlDbContext).Assembly,
            type => type.Namespace == "g0v0.Server.Common.Database.MySQL.Configurations");
    }
}