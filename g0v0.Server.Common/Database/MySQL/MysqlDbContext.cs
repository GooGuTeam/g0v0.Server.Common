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
    /// Gets or sets the trusted devices table set.
    /// </summary>
    public DbSet<TrustedDevice> TrustedDevices { get; set; } = null!;

    /// <summary>
    /// Gets or sets the login sessions table set.
    /// </summary>
    public DbSet<LoginSession> LoginSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the login attempt logs table set.
    /// </summary>
    public DbSet<LoginLogSession> LoginLogSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the relationships table set.
    /// </summary>
    public DbSet<Relationship> Relationships { get; set; } = null!;

    /// <summary>
    /// Gets or sets the beatmaps table set.
    /// </summary>
    public DbSet<Beatmap> Beatmaps { get; set; } = null!;

    /// <summary>
    /// Gets or sets the beatmap sets table set.
    /// </summary>
    public DbSet<BeatmapSet> BeatmapSets { get; set; } = null!;

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

    /// <summary>
    /// Gets or sets the chat channels table set.
    /// </summary>
    public DbSet<ChatChannel> ChatChannels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the chat messages table set.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    /// <summary>
    /// Gets or sets the chat silence users table set.
    /// </summary>
    public DbSet<SilenceUser> ChatSilenceUsers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the notifications.
    /// </summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>
    /// Gets or sets the per-user notification delivery records.
    /// </summary>
    public DbSet<UserNotification> UserNotifications { get; set; } = null!;

    /// <summary>
    /// Gets or sets the chat channel membership table set.
    /// </summary>
    public DbSet<ChatUserChannel> ChatUserChannels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the per-mode user performance statistics table set.
    /// </summary>
    public DbSet<UserStatistics> UserStatistics { get; set; } = null!;

    /// <summary>
    /// Gets or sets the highest-performance score per user, beatmap and mode table set.
    /// </summary>
    public DbSet<BestScore> BestScores { get; set; } = null!;

    /// <summary>
    /// Gets or sets the highest-scoring entry per user, beatmap, mode and mod set table set.
    /// </summary>
    public DbSet<LeaderboardScore> LeaderboardScores { get; set; } = null!;

    /// <summary>
    /// Gets or sets the per-user beatmap play count table set.
    /// </summary>
    public DbSet<BeatmapPlaycount> BeatmapPlaycounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the per-user monthly play count table set.
    /// </summary>
    public DbSet<MonthlyPlaycount> MonthlyPlaycounts { get; set; } = null!;

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
            type => string.Equals(type.Namespace, "g0v0.Server.Common.Database.MySQL.Configurations", StringComparison.Ordinal));
    }
}