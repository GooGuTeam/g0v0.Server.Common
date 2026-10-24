// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures the <see cref="LeaderboardScore"/> entity mapping for the v2 PostgreSQL schema.
/// </summary>
public class LeaderboardScoreConfig : IEntityTypeConfiguration<LeaderboardScore>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LeaderboardScore> builder)
    {
        builder.Property(score => score.Mods)
            .HasColumnType("jsonb")
            .HasConversion(
                value => ConfigurationHelper.SerializeModAcronyms(value),
                value => ConfigurationHelper.DeserializeModAcronyms(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModAcronymsComparer);

        SetIndexName(
            builder,
            [nameof(LeaderboardScore.UserId), nameof(LeaderboardScore.Mode), nameof(LeaderboardScore.ScoreId)],
            "ix_leaderboard_scores_user_mode_score");
        SetIndexName(
            builder,
            [nameof(LeaderboardScore.BeatmapId), nameof(LeaderboardScore.Mode), nameof(LeaderboardScore.TotalScore)],
            "ix_leaderboard_scores_beatmap_mode_score");
        SetIndexName(builder, [nameof(LeaderboardScore.Mode)], "ix_leaderboard_scores_gamemode");
    }

    private static void SetIndexName(
        EntityTypeBuilder<LeaderboardScore> builder,
        IReadOnlyCollection<string> propertyNames,
        string databaseName)
    {
        IMutableIndex index = builder.Metadata.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(propertyNames, StringComparer.Ordinal));
        index.SetDatabaseName(databaseName);
    }
}