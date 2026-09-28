// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the per-user delivery record of a notification in the
/// <c>user_notifications</c> table.
/// </summary>
[Table("user_notifications")]
[Index(nameof(NotificationId))]
[Index(nameof(UserId))]
[Index(nameof(IsRead))]
public class UserNotification
{
    /// <summary>
    /// Gets or sets the delivery record ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the notification.
    /// </summary>
    [ForeignKey(nameof(NotificationId))]
    public Notification? Notification { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user receiving the notification.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has read the notification.
    /// </summary>
    public bool IsRead { get; set; }
}