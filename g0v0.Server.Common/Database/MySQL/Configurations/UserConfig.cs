// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Applies legacy MySQL table naming for user records.
/// </summary>
public class UserConfig : IEntityTypeConfiguration<User>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("lazer_users");

        builder.Property(user => user.LastVisit)
            .HasColumnName("last_visit")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConvertNullableDateTimeToDateTimeOffset(value));
    }

    private static DateTime? ConvertNullableDateTimeOffsetToDateTime(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.UtcDateTime : null;
    }

    private static DateTimeOffset? ConvertNullableDateTimeToDateTimeOffset(DateTime? value)
    {
        return value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
}