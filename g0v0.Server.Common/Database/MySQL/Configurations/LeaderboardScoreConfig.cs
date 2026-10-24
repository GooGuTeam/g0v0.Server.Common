// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="LeaderboardScore"/> entity mapping for the legacy MySQL schema.
/// </summary>
public class LeaderboardScoreConfig : IEntityTypeConfiguration<LeaderboardScore>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LeaderboardScore> builder)
    {
        // The legacy schema names the leaderboard table after the original model; the entity keeps
        // the neutral name and the physical name is mapped here.
        builder.ToTable("total_score_best_scores");

        builder.Property(score => score.Mode)
            .HasColumnName("gamemode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value))
            .IsRequired();

        builder.Property(score => score.TotalScore).HasColumnType("bigint");

        builder.Property(score => score.Mods)
            .HasColumnName("mods")
            .HasColumnType("json")
            .HasConversion(
                value => ConfigurationHelper.SerializeModAcronyms(value),
                value => ConfigurationHelper.DeserializeModAcronyms(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModAcronymsComparer);

        builder.Property(score => score.Rank)
            .HasColumnName("rank")
            .HasColumnType("enum('X','XH','S','SH','A','B','C','D','F')")
            .HasConversion(
                value => ConfigurationHelper.ConvertRankToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToRank(value))
            .IsRequired();

        SetIndexName(
            builder,
            [nameof(LeaderboardScore.UserId), nameof(LeaderboardScore.Mode), nameof(LeaderboardScore.ScoreId)],
            "ix_total_score_best_scores_user_mode_score");
        SetIndexName(
            builder,
            [nameof(LeaderboardScore.BeatmapId), nameof(LeaderboardScore.Mode), nameof(LeaderboardScore.TotalScore)],
            "ix_total_score_best_scores_beatmap_mode_score");
        SetIndexName(builder, [nameof(LeaderboardScore.BeatmapId)], "ix_total_score_best_scores_beatmap_id");
        SetIndexName(builder, [nameof(LeaderboardScore.Mode)], "ix_total_score_best_scores_gamemode");
        SetIndexName(builder, [nameof(LeaderboardScore.UserId)], "ix_total_score_best_scores_user_id");
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