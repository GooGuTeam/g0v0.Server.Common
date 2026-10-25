// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>Maps favourites to the legacy lazer schema.</summary>
public class FavouriteBeatmapSetConfig : IEntityTypeConfiguration<FavouriteBeatmapSet>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FavouriteBeatmapSet> builder)
    {
        builder.Property(favourite => favourite.BeatmapSetId).HasColumnName("beatmapset_id");
        builder.Property(favourite => favourite.Date).HasColumnType("datetime").HasConversion(
            value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
            value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));
        builder.HasOne(favourite => favourite.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(favourite => favourite.BeatmapSet).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}