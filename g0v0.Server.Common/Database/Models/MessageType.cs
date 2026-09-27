// Copyright (c) GooGuTeam. License under MIT License.See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// The type of a chat message.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// A regular chat message.
    /// </summary>
    Plain,

    /// <summary>
    /// A markdown-formatted message (used in announcement channels).
    /// </summary>
    Markdown,

    /// <summary>
    /// An action message (<c>/me</c>).
    /// </summary>
    Action,
}