// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>Maps screenshot timestamps to the legacy UTC datetime columns.</summary>
public class ScreenshotConfig : IEntityTypeConfiguration<Screenshot>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Screenshot> builder)
    {
        builder.Property(screenshot => screenshot.Url).HasColumnType("varchar(255)");
        builder.Property(screenshot => screenshot.Timestamp).HasColumnType("datetime").HasConversion(
            value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
            value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));
        builder.Property(screenshot => screenshot.LastAccess).HasColumnType("datetime").HasConversion(
            value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
            value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));
    }
}