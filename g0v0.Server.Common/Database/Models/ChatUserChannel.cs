// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a chat channel membership row in the <c>chat_user_channels</c> table.
/// </summary>
/// <remarks>
/// One row per (user, channel) pair holding the user's read marker and whether
/// the channel is hidden from the user's channel list. PM and announcement
/// channels keep their row when the user leaves - the row is only hidden - so
/// that the conversation history stays reachable and reappears on the next
/// message.
/// </remarks>
[Table("chat_user_channels")]
[Index(nameof(ChannelId))]
[Index(nameof(Hidden))]
public class ChatUserChannel
{
    /// <summary>
    /// Gets or sets the ID of the user; part of the composite primary key.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel; part of the composite primary key.
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the channel is hidden from the
    /// user's channel list.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets the ID of the last message the user read in the channel,
    /// or <see langword="null"/> when the user never read it.
    /// </summary>
    public int? LastReadId { get; set; }
}