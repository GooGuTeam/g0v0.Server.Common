// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="Notification"/> to the v2 <c>notifications</c> table.
/// </summary>
public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.Property(notification => notification.Name)
            .HasMaxLength(64);

        builder.Property(notification => notification.Category)
            .HasMaxLength(255);

        builder.Property(notification => notification.CreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(notification => notification.ObjectType)
            .HasMaxLength(255);

        builder.Property(notification => notification.Details)
            .HasColumnType("jsonb");
    }
}