// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osu.Game.Online.Multiplayer;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="Playlist"/> to the legacy lazer API <c>room_playlists</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the table
/// name, the JSON-mod columns, the <c>win_condition</c> enum column, and the
/// <see cref="DateTimeOffset"/> columns stored as <c>datetime</c>.
/// </para>
/// </summary>
public class PlaylistConfig : IEntityTypeConfiguration<Playlist>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("room_playlists");

        // Mods are serialized as JSON in the legacy schema. Empty lists are stored
        // as NULL, so the columns are nullable.
        builder.Property(playlist => playlist.AllowedMods)
            .HasColumnType("json")
            .IsRequired(false)
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        builder.Property(playlist => playlist.RequiredMods)
            .HasColumnType("json")
            .IsRequired(false)
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        // The legacy schema stores the win condition as a MySQL enum.
        builder.Property(playlist => playlist.WinCondition)
            .HasColumnType("enum('SCORE','ACCURACY','COMBO','PP')")
            .HasConversion(
                value => value.HasValue ? value.Value.ToString().ToUpperInvariant() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<WinCondition>(value, true));

        // The legacy schema stores UTC timestamps in `datetime` columns.
        builder.Property(playlist => playlist.CreatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(playlist => playlist.PlayedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(playlist => playlist.UpdatedAt)
            .HasColumnType("datetime")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));
    }
}