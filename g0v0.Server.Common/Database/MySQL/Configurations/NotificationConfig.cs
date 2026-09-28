// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="Notification"/> to the legacy lazer API <c>notifications</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// JSON details column and the <c>datetime</c> creation timestamp. The name
/// column is a native MySQL enum whose values are stored in UPPER_SNAKE_CASE;
/// the case-insensitive collation normalises the lower case values written by
/// this backend.
/// </para>
/// </summary>
public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.Property(notification => notification.Category)
            .HasMaxLength(255);

        builder.Property(notification => notification.CreatedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(notification => notification.ObjectType)
            .HasMaxLength(255);

        builder.Property(notification => notification.Details)
            .HasColumnType("json");

        builder.Property(notification => notification.Name)
            .HasConversion(
                value => value.ToUpperInvariant(),
                value => value)
            .IsRequired();
    }
}