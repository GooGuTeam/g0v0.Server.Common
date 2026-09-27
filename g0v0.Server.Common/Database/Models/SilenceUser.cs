// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Records a user silence (mute) in a specific chat channel.
/// </summary>
/// <remarks>
/// A user with an active silence in a channel cannot send messages there until
/// <see cref="Until"/> passes. The record itself is never deleted; the API
/// exposes it through <c>chat/updates</c> and <c>chat/ack</c> responses so
/// clients can keep their silenced state in sync.
/// </remarks>
[Table("chat_silence_users")]
[Index(nameof(UserId))]
[Index(nameof(ChannelId))]
[Index(nameof(Until))]
[Index(nameof(BannedAt))]
public class SilenceUser
{
    /// <summary>
    /// Gets or sets the internal numeric silence identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the silenced user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel the silence applies to.
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp until which the user is silenced; <see langword="null"/> silences indefinitely.
    /// </summary>
    public DateTimeOffset Until { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the silence.
    /// </summary>
    [StringLength(255)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the timestamp at which the silence was issued.
    /// </summary>
    public DateTimeOffset BannedAt { get; set; }
}