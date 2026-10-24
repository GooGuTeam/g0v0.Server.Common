// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="UserStatistics"/> entity mapping for the legacy MySQL schema.
/// </summary>
public class UserStatisticsConfig : IEntityTypeConfiguration<UserStatistics>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserStatistics> builder)
    {
        builder.ToTable("user_statistics");

        // The table identifies a statistics record by (user_id, mode).
        builder.HasKey(statistics => new { statistics.UserId, statistics.Mode });

        // The snake-case naming convention would map Count100 to `count100`;
        // the legacy schema uses `count_100`.
        builder.Property(statistics => statistics.Count100).HasColumnName("count_100");
        builder.Property(statistics => statistics.Count300).HasColumnName("count_300");
        builder.Property(statistics => statistics.Count50).HasColumnName("count_50");
        builder.Property(statistics => statistics.CountMiss).HasColumnName("count_miss");

        builder.Property(statistics => statistics.Mode)
            .HasColumnName("mode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value))
            .IsRequired();
    }
}