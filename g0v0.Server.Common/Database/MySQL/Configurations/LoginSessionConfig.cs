// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures <see cref="LoginSession"/> for MySQL.
/// </summary>
public class LoginSessionConfig : IEntityTypeConfiguration<LoginSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LoginSession> builder)
    {
        builder.Property(record => record.UserAgent).HasColumnType("text");

        builder.HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(session => session.Device)
            .WithMany(device => device.Sessions)
            .HasForeignKey(session => session.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(session => session.Token)
            .WithMany()
            .HasForeignKey(session => session.TokenId)
            .OnDelete(DeleteBehavior.SetNull);

        // The legacy device_id column is BIGINT, while trusted_devices.id is INT.
        builder.Property(session => session.DeviceId)
            .HasConversion<long>()
            .HasColumnType("bigint");

        builder.Property(record => record.CreatedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));

        builder.Property(record => record.VerifiedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(record => record.ExpiresAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));
    }
}