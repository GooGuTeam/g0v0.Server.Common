// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using BeatmapSetModel = g0v0.Server.Common.Database.Models.BeatmapSet;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures the <see cref="BeatmapSetModel"/> entity mapping for the PostgreSQL schema.
/// </summary>
public class BeatmapSetConfig : IEntityTypeConfiguration<BeatmapSetModel>
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BeatmapSetModel> builder)
    {
        builder.ToTable("beatmapsets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Artist).HasColumnName("artist").HasMaxLength(255).IsRequired();
        builder.Property(b => b.ArtistUnicode).HasColumnName("artist_unicode").HasMaxLength(255).IsRequired();
        builder.Property(b => b.Creator).HasColumnName("creator").HasMaxLength(255).IsRequired();
        builder.Property(b => b.PreviewUrl).HasColumnName("preview_url").HasMaxLength(255).IsRequired();
        builder.Property(b => b.Source).HasColumnName("source").HasMaxLength(255).IsRequired();
        builder.Property(b => b.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(b => b.TitleUnicode).HasColumnName("title_unicode").HasMaxLength(255).IsRequired();
        builder.Property(b => b.CreatorId).HasColumnName("user_id").IsRequired();
        builder.Property(b => b.FeatureArtistTrackId).HasColumnName("track_id");

        builder.Property(b => b.Covers)
            .HasColumnName("covers")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineCovers>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineCovers>.Create());

        builder.Property(b => b.CurrentNominations)
            .HasColumnName("current_nominations")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineNomination[]>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineNomination[]?>.Create());

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapDescription>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapDescription?>.Create());

        builder.Property(b => b.Genre)
            .HasColumnName("genre")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineGenre>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineGenre>.Create());

        builder.Property(b => b.Language)
            .HasColumnName("language")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapSetOnlineLanguage>(value, JsonSettings))
            .Metadata.SetValueComparer(ComparerFor<BeatmapSetOnlineLanguage>.Create());

        builder.Property(b => b.Status).HasColumnName("status").IsRequired();
        builder.Property(b => b.HasExplicitContent).HasColumnName("nsfw");
        builder.Property(b => b.Spotlight).HasColumnName("spotlight");
        builder.Property(b => b.HasVideo).HasColumnName("video");
        builder.Property(b => b.HasStoryboard).HasColumnName("storyboard");
        builder.Property(b => b.Bpm).HasColumnName("bpm");
        builder.Property(b => b.SubmittedDate).HasColumnName("submitted_date");
        builder.Property(b => b.RankedDate).HasColumnName("ranked_date");
        builder.Property(b => b.LastUpdatedDate).HasColumnName("last_updated");
        builder.Property(b => b.Tags).HasColumnName("tags");

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