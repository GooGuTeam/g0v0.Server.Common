// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents a chat message record in the <c>chat_messages</c> table.
/// </summary>
/// <remarks>
/// Message identifiers are monotonically increasing per server; clients use
/// them for pagination (<c>since</c>/<c>until</c>) and read tracking.
/// </remarks>
[Table("chat_messages")]
[Index(nameof(ChannelId))]
[Index(nameof(SenderId))]
[Index(nameof(Timestamp))]
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the internal numeric message identifier.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MessageId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the channel the message belongs to.
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the message.
    /// </summary>
    public int SenderId { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp at which the message was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the message type (<see cref="MessageType.Plain"/>, <see cref="MessageType.Markdown"/> or <see cref="MessageType.Action"/>).
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Plain;

    /// <summary>
    /// Gets or sets the client-generated UUID of the message, when the client
    /// supplied one. It is relayed back to the client so it can match the
    /// message against the local echo.
    /// </summary>
    public string? Uuid { get; set; }

    /// <summary>
    /// Gets or sets the channel this message belongs to.
    /// </summary>
    [ForeignKey(nameof(ChannelId))]
    public ChatChannel Channel { get; set; } = null!;
}