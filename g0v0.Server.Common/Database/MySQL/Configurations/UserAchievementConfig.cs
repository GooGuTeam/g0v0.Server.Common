// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>Reuses lazer's existing user achievement table and IDs.</summary>
public class UserAchievementConfig : IEntityTypeConfiguration<UserAchievement>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("lazer_user_achievements");
        builder.Property(item => item.AchievedAt).HasColumnType("datetime").HasConversion(
            value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
            value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));
    }
}