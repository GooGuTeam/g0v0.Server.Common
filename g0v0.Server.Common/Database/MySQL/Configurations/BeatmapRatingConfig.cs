// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>Maps ratings to the legacy lazer schema.</summary>
public class BeatmapRatingConfig : IEntityTypeConfiguration<BeatmapRating>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BeatmapRating> builder)
    {
        builder.Property(rating => rating.BeatmapSetId).HasColumnName("beatmapset_id");
        builder.HasOne(rating => rating.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rating => rating.BeatmapSet).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}