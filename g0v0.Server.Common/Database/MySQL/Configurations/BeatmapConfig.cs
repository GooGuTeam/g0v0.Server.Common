// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osu.Game.Beatmaps;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

public class BeatmapConfig : IEntityTypeConfiguration<BeatmapModel>
{
    private static readonly Dictionary<int, string> ModeToDatabaseValue = new()
    {
        [0] = "OSU",
        [1] = "TAIKO",
        [2] = "FRUITS",
        [3] = "MANIA",
        [10] = "SENTAKKI",
        [11] = "TAU",
        [12] = "RUSH",
        [13] = "HISHIGATA",
        [14] = "SOYOKAZE",
    };

    private static readonly Dictionary<string, int> DatabaseValueToMode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OSU"] = 0,
        ["OSURX"] = 0,
        ["OSUAP"] = 0,
        ["TAIKO"] = 1,
        ["TAIKORX"] = 1,
        ["FRUITS"] = 2,
        ["FRUITSRX"] = 2,
        ["MANIA"] = 3,
        ["SENTAKKI"] = 10,
        ["TAU"] = 11,
        ["RUSH"] = 12,
        ["HISHIGATA"] = 13,
        ["SOYOKAZE"] = 14,
    };

    private static readonly Dictionary<BeatmapOnlineStatus, string> StatusToDatabaseValue = new()
    {
        [BeatmapOnlineStatus.Graveyard] = "GRAVEYARD",
        [BeatmapOnlineStatus.WIP] = "WIP",
        [BeatmapOnlineStatus.Pending] = "PENDING",
        [BeatmapOnlineStatus.Ranked] = "RANKED",
        [BeatmapOnlineStatus.Approved] = "APPROVED",
        [BeatmapOnlineStatus.Qualified] = "QUALIFIED",
        [BeatmapOnlineStatus.Loved] = "LOVED",
    };

    private static readonly Dictionary<string, BeatmapOnlineStatus> DatabaseValueToStatus =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GRAVEYARD"] = BeatmapOnlineStatus.Graveyard,
            ["WIP"] = BeatmapOnlineStatus.WIP,
            ["PENDING"] = BeatmapOnlineStatus.Pending,
            ["RANKED"] = BeatmapOnlineStatus.Ranked,
            ["APPROVED"] = BeatmapOnlineStatus.Approved,
            ["QUALIFIED"] = BeatmapOnlineStatus.Qualified,
            ["LOVED"] = BeatmapOnlineStatus.Loved,
        };

    public void Configure(EntityTypeBuilder<BeatmapModel> builder)
    {
        builder.ToTable("beatmaps");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Url)
            .HasColumnName("url")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.Mode)
            .HasColumnName("mode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConvertModeToDatabaseValue(value),
                value => ConvertDatabaseValueToMode(value))
            .IsRequired();

        builder.Property(b => b.DifficultyRating)
            .HasColumnName("difficulty_rating")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.TotalLength)
            .HasColumnName("total_length")
            .IsRequired();

        builder.Property(b => b.MapperId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.Version)
            .HasColumnName("version")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(32);

        builder.Property(b => b.MaxCombo)
            .HasColumnName("max_combo");

        builder.Property(b => b.ApproachRate)
            .HasColumnName("ar")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.CircleSize)
            .HasColumnName("cs")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.HpDrainRate)
            .HasColumnName("drain")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.OverallDifficulty)
            .HasColumnName("accuracy")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.Bpm)
            .HasColumnName("bpm")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(b => b.CirclesCount)
            .HasColumnName("count_circles")
            .IsRequired();

        builder.Property(b => b.SlidersCount)
            .HasColumnName("count_sliders")
            .IsRequired();

        builder.Property(b => b.SpinnersCount)
            .HasColumnName("count_spinners")
            .IsRequired();

        builder.Property(b => b.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.HitLength)
            .HasColumnName("hit_length")
            .IsRequired();

        builder.Property(b => b.LastUpdate)
            .HasColumnName("last_updated")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.BeatmapSetId)
            .HasColumnName("beatmapset_id")
            .IsRequired();

        builder.Property(b => b.Status)
            .HasColumnName("beatmap_status")
            .HasColumnType("enum('GRAVEYARD','WIP','PENDING','RANKED','APPROVED','QUALIFIED','LOVED')")
            .HasConversion(
                value => ConvertStatusToDatabaseValue(value),
                value => ConvertDatabaseValueToStatus(value))
            .IsRequired();

        builder.Ignore(b => b.BeatmapVersion);

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("ix_beatmaps_beatmap_status");

        builder.HasIndex(b => b.BeatmapSetId)
            .HasDatabaseName("ix_beatmaps_beatmapset_id");

        builder.HasIndex(b => b.Checksum)
            .HasDatabaseName("ix_beatmaps_checksum");

        builder.HasIndex(b => b.DifficultyRating)
            .HasDatabaseName("ix_beatmaps_difficulty_rating");

        builder.HasIndex(b => b.Id)
            .HasDatabaseName("ix_beatmaps_id");

        builder.HasIndex(b => b.LastUpdate)
            .HasDatabaseName("ix_beatmaps_last_updated");

        builder.HasIndex(b => b.MapperId)
            .HasDatabaseName("ix_beatmaps_user_id");

        builder.HasIndex(b => b.Version)
            .HasDatabaseName("ix_beatmaps_version");
    }

    private static string ConvertModeToDatabaseValue(int value)
    {
        return ModeToDatabaseValue.TryGetValue(value, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported legacy beatmap mode value '{value}'.");
    }

    private static int ConvertDatabaseValueToMode(string value)
    {
        return DatabaseValueToMode.TryGetValue(value, out int mode)
            ? mode
            : throw new InvalidOperationException($"Unsupported legacy beatmap mode '{value}'.");
    }

    private static string ConvertStatusToDatabaseValue(BeatmapOnlineStatus value)
    {
        return StatusToDatabaseValue.TryGetValue(value, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported beatmap status '{value}'.");
    }

    private static BeatmapOnlineStatus ConvertDatabaseValueToStatus(string value)
    {
        return DatabaseValueToStatus.TryGetValue(value, out BeatmapOnlineStatus status)
            ? status
            : throw new InvalidOperationException($"Unsupported beatmap status '{value}'.");
    }

    private static DateTime? ConvertNullableDateTimeOffsetToDateTime(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.UtcDateTime : null;
    }

    private static DateTimeOffset? ConvertNullableDateTimeToDateTimeOffset(DateTime? value)
    {
        return value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
}