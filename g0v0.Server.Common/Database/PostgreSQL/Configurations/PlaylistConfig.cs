// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="Playlist"/> to the v2 PostgreSQL <c>playlists</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the mod
/// lists stored as <c>jsonb</c> and the <c>now()</c> defaults for timestamps.
/// </para>
/// </summary>
public class PlaylistConfig : IEntityTypeConfiguration<Playlist>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        // Mods are serialized as JSONB. Empty lists are stored as NULL, so the
        // columns are nullable.
        builder.Property(playlist => playlist.AllowedMods)
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        builder.Property(playlist => playlist.RequiredMods)
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        builder.Property(playlist => playlist.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.Property(playlist => playlist.UpdatedAt)
            .HasDefaultValueSql("now()");
    }
}