// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a notification in the <c>notifications</c> table, mirroring the
/// notification model of the reference implementations (osu-web's
/// App\Models\Notification and the lazer API's app/database/notification.py).
/// </summary>
[Table("notifications")]
[Index(nameof(Category))]
[Index(nameof(Name))]
[Index(nameof(ObjectType))]
[Index(nameof(ObjectId))]
[Index(nameof(SourceUserId))]
public class Notification
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the notification name (for example <c>channel_message</c>).
    /// </summary>
    /// <remarks>
    /// The legacy MySQL column is a native enum whose values are stored in
    /// UPPER_SNAKE_CASE; MySQL's case-insensitive collation accepts the lower
    /// case value on write and normalises it, so readers must compare names
    /// case-insensitively.
    /// </remarks>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification category (for example <c>channel</c>).
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the type of the object the notification is about (the
    /// notifiable type, for example <c>channel</c>).
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ObjectType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the object the notification is about.
    /// </summary>
    public long ObjectId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user that triggered the notification.
    /// </summary>
    public int SourceUserId { get; set; }

    /// <summary>
    /// Gets or sets the JSON serialised details payload.
    /// </summary>
    public string? Details { get; set; }
}