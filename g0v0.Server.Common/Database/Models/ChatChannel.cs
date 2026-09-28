// Copyright (c) GooGuTeam. License under MIT License.See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.Chat;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a chat channel record in the <c>chat_channels</c> table.
/// </summary>
/// <remarks>
/// Channels cover public rooms, private messages (PM), multiplayer rooms,
/// spectator rooms, temporary rooms, group chats, system messages,
/// announcements and team chats. The channel type is stored as a native
/// MySQL enum on the legacy schema and as a string on PostgreSQL.
/// </remarks>
[Table("chat_channels")]
[Index(nameof(Description))]
[Index(nameof(Type))]
[Index(nameof(Name))]
public class ChatChannel
{
    /// <summary>
    /// Gets or sets the internal numeric channel identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the channel name (for PM channels this is <c>pm_{user1}_{user2}</c>).
    /// </summary>
    [StringLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the channel topic/description.
    /// </summary>
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel type.
    /// </summary>
    public ChannelType Type { get; set; }

    /// <summary>
    /// Gets or sets the ID of the last message posted in the channel, if any.
    /// </summary>
    /// <remarks>
    /// Refreshed every time a message is received.
    /// </remarks>
    public int? LastMessageId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the channel is moderated, that
    /// is whether posting is restricted to moderators.
    /// </summary>
    public bool Moderated { get; set; }
}