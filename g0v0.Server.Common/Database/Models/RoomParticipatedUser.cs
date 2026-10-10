// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a user's participation history in a multiplayer room.
/// </summary>
/// <remarks>
/// Rows are keyed by the composite (<c>room_id</c>, <c>user_id</c>) primary key on both
/// backends; there is no surrogate <c>id</c> column. Rejoining reopens the existing record.
/// </remarks>
[Table("room_participated_users")]
[Index(nameof(RoomId))]
[Index(nameof(UserId))]
public class RoomParticipatedUser
{
    /// <summary>
    /// Gets or sets the room ID.
    /// </summary>
    public long RoomId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the join timestamp.
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the leave timestamp, when the user is no longer in the room.
    /// </summary>
    public DateTimeOffset? LeftAt { get; set; }

    /// <summary>
    /// Gets or sets the room this participation record belongs to.
    /// </summary>
    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    /// <summary>
    /// Gets or sets the participating user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}