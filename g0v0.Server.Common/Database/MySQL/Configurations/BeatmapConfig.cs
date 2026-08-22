// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="BeatmapModel"/> entity mapping for the legacy MySQL schema.
/// </summary>
public class BeatmapConfig : IEntityTypeConfiguration<BeatmapModel>
{
    /// <inheritdoc/>
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
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value))
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
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.HitLength)
            .HasColumnName("hit_length")
            .IsRequired();

        builder.Property(b => b.LastUpdate)
            .HasColumnName("last_updated")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.BeatmapSetId)
            .HasColumnName("beatmapset_id")
            .IsRequired();

        builder.Property(b => b.Status)
            .HasColumnName("beatmap_status")
            .HasColumnType("enum('GRAVEYARD','WIP','PENDING','RANKED','APPROVED','QUALIFIED','LOVED')")
            .HasConversion(
                value => ConfigurationHelper.ConvertBeatmapStatusToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToBeatmapStatus(value))
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
}