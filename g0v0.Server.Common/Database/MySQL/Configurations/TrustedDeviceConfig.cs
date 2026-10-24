// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures <see cref="TrustedDevice"/> for MySQL.
/// </summary>
public class TrustedDeviceConfig : IEntityTypeConfiguration<TrustedDevice>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.Property(record => record.UserAgent).HasColumnType("text");

        builder.HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(record => record.CreatedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));

        builder.Property(record => record.LastUsedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));

        builder.Property(record => record.ExpiresAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));
    }
}