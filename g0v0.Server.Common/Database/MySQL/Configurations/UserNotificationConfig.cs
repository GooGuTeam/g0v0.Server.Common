// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="UserNotification"/> to the legacy lazer API
/// <c>user_notifications</c> table.
/// </summary>
public class UserNotificationConfig : IEntityTypeConfiguration<UserNotification>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("user_notifications");

        builder.HasOne(userNotification => userNotification.Notification)
            .WithMany()
            .HasForeignKey(userNotification => userNotification.NotificationId);
    }
}