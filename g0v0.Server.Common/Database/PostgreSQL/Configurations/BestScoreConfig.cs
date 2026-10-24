// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures the <see cref="BestScore"/> entity mapping for the v2 PostgreSQL schema.
/// </summary>
public class BestScoreConfig : IEntityTypeConfiguration<BestScore>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BestScore> builder)
    {
        SetIndexName(
            builder,
            [nameof(BestScore.UserId), nameof(BestScore.Mode), nameof(BestScore.Pp), nameof(BestScore.ScoreId)],
            "ix_best_scores_user_mode_pp_score");
        SetIndexName(
            builder,
            [nameof(BestScore.Mode), nameof(BestScore.Pp), nameof(BestScore.ScoreId)],
            "ix_best_scores_mode_pp_score");
        SetIndexName(builder, [nameof(BestScore.Mode)], "ix_best_scores_gamemode");
    }

    private static void SetIndexName(
        EntityTypeBuilder<BestScore> builder,
        IReadOnlyCollection<string> propertyNames,
        string databaseName)
    {
        IMutableIndex index = builder.Metadata.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(propertyNames, StringComparer.Ordinal));
        index.SetDatabaseName(databaseName);
    }
}