// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using BeatmapSetModel = g0v0.Server.Common.Database.Models.BeatmapSet;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="BeatmapSetModel"/> entity mapping for the legacy MySQL schema.
/// </summary>
public class BeatmapSetConfig : IEntityTypeConfiguration<BeatmapSetModel>
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    private static readonly Dictionary<string, int> GenreToDatabaseValue = new(StringComparer.Ordinal)
    {
        ["ANY"] = 0,
        ["UNSPECIFIED"] = 1,
        ["VIDEO_GAME"] = 2,
        ["ANIME"] = 3,
        ["ROCK"] = 4,
        ["POP"] = 5,
        ["OTHER"] = 6,
        ["NOVELTY"] = 7,
        ["HIP_HOP"] = 9,
        ["ELECTRONIC"] = 10,
        ["METAL"] = 11,
        ["CLASSICAL"] = 12,
        ["FOLK"] = 13,
        ["JAZZ"] = 14,
    };

    private static readonly Dictionary<string, int> LanguageToDatabaseValue = new(StringComparer.Ordinal)
    {
        ["ANY"] = 0,
        ["UNSPECIFIED"] = 1,
        ["ENGLISH"] = 2,
        ["JAPANESE"] = 3,
        ["CHINESE"] = 4,
        ["INSTRUMENTAL"] = 5,
        ["KOREAN"] = 6,
        ["FRENCH"] = 7,
        ["GERMAN"] = 8,
        ["SWEDISH"] = 9,
        ["ITALIAN"] = 10,
        ["SPANISH"] = 11,
        ["RUSSIAN"] = 12,
        ["POLISH"] = 13,
        ["OTHER"] = 14,
    };

    private static readonly Dictionary<int, string> DatabaseValueToGenre = new()
    {
        [0] = "ANY",
        [1] = "UNSPECIFIED",
        [2] = "VIDEO_GAME",
        [3] = "ANIME",
        [4] = "ROCK",
        [5] = "POP",
        [6] = "OTHER",
        [7] = "NOVELTY",
        [9] = "HIP_HOP",
        [10] = "ELECTRONIC",
        [11] = "METAL",
        [12] = "CLASSICAL",
        [13] = "FOLK",
        [14] = "JAZZ",
    };

    private static readonly Dictionary<int, string> DatabaseValueToLanguage = new()
    {
        [0] = "ANY",
        [1] = "UNSPECIFIED",
        [2] = "ENGLISH",
        [3] = "JAPANESE",
        [4] = "CHINESE",
        [5] = "INSTRUMENTAL",
        [6] = "KOREAN",
        [7] = "FRENCH",
        [8] = "GERMAN",
        [9] = "SWEDISH",
        [10] = "ITALIAN",
        [11] = "SPANISH",
        [12] = "RUSSIAN",
        [13] = "POLISH",
        [14] = "OTHER",
    };

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BeatmapSetModel> builder)
    {
        builder.ToTable("beatmapsets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Covers)
            .HasColumnName("covers")
            .HasColumnType("json")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineCovers>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineCovers>.Create());

        builder.Property(b => b.HasExplicitContent)
            .HasColumnName("nsfw")
            .HasColumnType("tinyint(1)");

        builder.Property(b => b.CreatorId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.HasVideo)
            .HasColumnName("video")
            .HasColumnType("tinyint(1)");

        builder.Property(b => b.FeatureArtistTrackId)
            .HasColumnName("track_id");

        builder.Property(b => b.CurrentNominations)
            .HasColumnName("current_nominations")
            .HasColumnType("json")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineNomination[]>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineNomination[]?>.Create());

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasColumnType("json")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapDescription>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapDescription?>.Create());

        builder.Property(b => b.HasStoryboard)
            .HasColumnName("storyboard")
            .HasColumnType("tinyint(1)");

        builder.Property(b => b.LastUpdatedDate)
            .HasColumnName("last_updated")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.RankedDate)
            .HasColumnName("ranked_date")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(b => b.SubmittedDate)
            .HasColumnName("submitted_date")
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(b => b.Status)
            .HasColumnName("beatmap_status")
            .HasColumnType("enum('GRAVEYARD','WIP','PENDING','RANKED','APPROVED','QUALIFIED','LOVED')")
            .HasConversion(
                value => ConfigurationHelper.ConvertBeatmapStatusToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToBeatmapStatus(value))
            .IsRequired();

        builder.Property(b => b.Genre)
            .HasColumnName("beatmap_genre")
            .HasColumnType("enum('ANY','UNSPECIFIED','VIDEO_GAME','ANIME','ROCK','POP','OTHER','NOVELTY','HIP_HOP','ELECTRONIC','METAL','CLASSICAL','FOLK','JAZZ')")
            .HasConversion(
                value => ConvertGenreToDatabaseValue(value),
                value => ConvertDatabaseValueToGenre(value))
            .IsRequired();

        builder.Property(b => b.Language)
            .HasColumnName("beatmap_language")
            .HasColumnType("enum('ANY','UNSPECIFIED','ENGLISH','JAPANESE','CHINESE','INSTRUMENTAL','KOREAN','FRENCH','GERMAN','SWEDISH','ITALIAN','SPANISH','RUSSIAN','POLISH','OTHER')")
            .HasConversion(
                value => ConvertLanguageToDatabaseValue(value),
                value => ConvertDatabaseValueToLanguage(value))
            .IsRequired();

        builder.Property<int>("NominationsRequired")
            .HasColumnName("nominations_required")
            .IsRequired();

        builder.Property<int>("NominationsCurrent")
            .HasColumnName("nominations_current")
            .IsRequired();

        builder.Property<int>("HypeCurrent")
            .HasColumnName("hype_current")
            .IsRequired();

        builder.Property<int>("HypeRequired")
            .HasColumnName("hype_required")
            .IsRequired();

        builder.Property<string?>("AvailabilityInfo")
            .HasColumnName("availability_info")
            .HasMaxLength(255);

        builder.Property<bool?>("DownloadDisabled")
            .HasColumnName("download_disabled")
            .HasColumnType("tinyint(1)");

        builder.Property<bool?>("CanBeHyped")
            .HasColumnName("can_be_hyped")
            .HasColumnType("tinyint(1)");

        builder.Property<bool?>("DiscussionLocked")
            .HasColumnName("discussion_locked")
            .HasColumnType("tinyint(1)");

        builder.Property<string?>("PackTags")
            .HasColumnName("pack_tags")
            .HasColumnType("json");

        builder.HasMany(b => b.Beatmaps)
            .WithOne(b => b.BeatmapSet)
            .HasForeignKey(b => b.BeatmapSetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.Artist).HasDatabaseName("ix_beatmapsets_artist");
        builder.HasIndex(b => b.ArtistUnicode).HasDatabaseName("ix_beatmapsets_artist_unicode");
        builder.HasIndex(b => b.Id).HasDatabaseName("ix_beatmapsets_id");
        builder.HasIndex(b => b.Genre).HasDatabaseName("ix_beatmapsets_beatmap_genre");
        builder.HasIndex(b => b.Language).HasDatabaseName("ix_beatmapsets_beatmap_language");
        builder.HasIndex(b => b.Status).HasDatabaseName("ix_beatmapsets_beatmap_status");
        builder.HasIndex(b => b.Creator).HasDatabaseName("ix_beatmapsets_creator");
        builder.HasIndex(b => b.LastUpdatedDate).HasDatabaseName("ix_beatmapsets_last_updated");
        builder.HasIndex(b => b.RankedDate).HasDatabaseName("ix_beatmapsets_ranked_date");
        builder.HasIndex(b => b.HasStoryboard).HasDatabaseName("ix_beatmapsets_storyboard");
        builder.HasIndex(b => b.SubmittedDate).HasDatabaseName("ix_beatmapsets_submitted_date");
        builder.HasIndex(b => b.Title).HasDatabaseName("ix_beatmapsets_title");
        builder.HasIndex(b => b.TitleUnicode).HasDatabaseName("ix_beatmapsets_title_unicode");
        builder.HasIndex(b => b.FeatureArtistTrackId).HasDatabaseName("ix_beatmapsets_track_id");
        builder.HasIndex(b => b.CreatorId).HasDatabaseName("ix_beatmapsets_user_id");
        builder.HasIndex(b => b.HasVideo).HasDatabaseName("ix_beatmapsets_video");
    }

    private static string ConvertGenreToDatabaseValue(BeatmapSetOnlineGenre value)
    {
        return DatabaseValueToGenre.TryGetValue(value.Id, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported beatmap set genre '{value.Id}'.");
    }

    private static BeatmapSetOnlineGenre ConvertDatabaseValueToGenre(string value)
    {
        return !GenreToDatabaseValue.TryGetValue(value, out int id)
            ? throw new InvalidOperationException($"Unsupported beatmap set genre '{value}'.")
            : new BeatmapSetOnlineGenre { Id = id, Name = value };
    }

    private static string ConvertLanguageToDatabaseValue(BeatmapSetOnlineLanguage value)
    {
        return DatabaseValueToLanguage.TryGetValue(value.Id, out string? databaseValue)
            ? databaseValue
            : throw new InvalidOperationException($"Unsupported beatmap set language '{value.Id}'.");
    }

    private static BeatmapSetOnlineLanguage ConvertDatabaseValueToLanguage(string value)
    {
        return !LanguageToDatabaseValue.TryGetValue(value, out int id)
            ? throw new InvalidOperationException($"Unsupported beatmap set language '{value}'.")
            : new BeatmapSetOnlineLanguage { Id = id, Name = value };
    }

    private static class ComparerFor<T>
    {
        public static ValueComparer<T> Create()
        {
            return new ValueComparer<T>(
                (left, right) => JsonConvert.SerializeObject(left, JsonSettings) == JsonConvert.SerializeObject(right, JsonSettings),
                value => JsonConvert.SerializeObject(value, JsonSettings).GetHashCode(StringComparison.Ordinal),
                value => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, JsonSettings), JsonSettings)!);
        }
    }
}