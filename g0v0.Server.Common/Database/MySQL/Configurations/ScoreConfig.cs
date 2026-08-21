// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

public class ScoreConfig : IEntityTypeConfiguration<Score>
{
    public void Configure(EntityTypeBuilder<Score> builder)
    {
        builder.Ignore(s => s.Statistics);
        builder.Ignore(s => s.ClassicTotalScoreWithoutMods);

        builder.Property(s => s.ClientVersion)
            .HasColumnName("client_version")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(s => s.EndedAt)
            .HasColumnName("ended_at")
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(s => s.Mods)
            .HasColumnName("mods")
            .HasColumnType("json")
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        builder.Property(s => s.Rank)
            .HasColumnName("rank")
            .HasColumnType("enum('X','XH','S','SH','A','B','C','D','F')")
            .HasConversion(
                value => ConfigurationHelper.ConvertRankToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToRank(value))
            .IsRequired();

        builder.Property(s => s.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(s => s.Type)
            .HasColumnName("type")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion(
                s => s,
                value => value == "solo"
                    ? "lazer"
                    : value); // TODO: make g0v0-server v1 change `type` from `solo` to `lazer`

        builder.Property<int>("N300")
            .HasColumnName("n300")
            .IsRequired();

        builder.Property<int>("N100")
            .HasColumnName("n100")
            .IsRequired();

        builder.Property<int>("N50")
            .HasColumnName("n50")
            .IsRequired();

        builder.Property<int>("NMiss")
            .HasColumnName("nmiss")
            .IsRequired();

        builder.Property<int>("NGeki")
            .HasColumnName("ngeki")
            .IsRequired();

        builder.Property<int>("NKatu")
            .HasColumnName("nkatu")
            .IsRequired();

        builder.Property<int?>("NLargeTickMiss")
            .HasColumnName("nlarge_tick_miss");

        builder.Property<int?>("NLargeTickHit")
            .HasColumnName("nlarge_tick_hit");

        builder.Property<int?>("NSliderTailHit")
            .HasColumnName("nslider_tail_hit");

        builder.Property<int?>("NSmallTickHit")
            .HasColumnName("nsmall_tick_hit");

        builder.Property<int?>("NSmallTickMiss").HasColumnName("nsmall_tick_miss");

        builder.Property(s => s.Mode)
            .HasColumnName("gamemode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value))
            .IsRequired();

        builder.Property(s => s.MaximumStatistics)
            .HasColumnName("maximum_statistics")
            .HasColumnType("json")
            .HasConversion(
                value => ConfigurationHelper.SerializeStatistics(value),
                value => ConfigurationHelper.DeserializeStatistics(value))
            .Metadata.SetValueComparer(ConfigurationHelper.StatisticsComparer);

        builder.Property(s => s.BeatmapChecksum).HasColumnName("map_md5");

        builder.HasOne<Beatmap>()
            .WithMany()
            .HasForeignKey(s => s.BeatmapId)
            .HasConstraintName("scores_ibfk_1")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("scores_ibfk_2")
            .OnDelete(DeleteBehavior.Restrict);

        SetIndexName(
            builder,
            [nameof(Score.UserId), nameof(Score.Mode), nameof(Score.EndedAt), nameof(Score.Id)],
            "idx_score_user_mode_date");
        SetIndexName(
            builder,
            [nameof(Score.UserId), nameof(Score.Mode), nameof(Score.Pp), nameof(Score.Id)],
            "idx_score_user_mode_pp");
        SetIndexName(builder, [nameof(Score.BeatmapId)], "ix_scores_beatmap_id");
        SetIndexName(builder, [nameof(Score.Mode)], "ix_scores_gamemode");
        SetIndexName(builder, [nameof(Score.BeatmapChecksum)], "ix_scores_map_md5");
        SetIndexName(builder, [nameof(Score.UserId)], "ix_scores_user_id");
    }

    private static void SetIndexName(EntityTypeBuilder<Score> builder, IReadOnlyCollection<string> propertyNames, string databaseName)
    {
        var index = builder.Metadata.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(propertyNames, StringComparer.Ordinal));
        index.SetDatabaseName(databaseName);
    }
}