// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Online.Chat;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Provides persistence operations for the chat system (channels, messages and silences).
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Gets a chat channel by its numeric ID.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The channel, or <see langword="null"/> when no such channel exists.</returns>
    Task<ChatChannel?> GetChannelAsync(int channelId);

    /// <summary>
    /// Gets a chat channel by its name.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <returns>The channel, or <see langword="null"/> when no such channel exists.</returns>
    Task<ChatChannel?> GetChannelByNameAsync(string name);

    /// <summary>
    /// Gets a chat channel by ID or name, mirroring the lazer API <c>ChatChannel.get</c> helper.
    /// </summary>
    /// <param name="channel">The channel ID (numeric string) or name.</param>
    /// <returns>The channel, or <see langword="null"/> when no such channel exists.</returns>
    Task<ChatChannel?> GetChannelAsync(string channel);

    /// <summary>
    /// Gets the PM channel between two users, checking both <c>pm_{user1}_{user2}</c> orderings.
    /// </summary>
    /// <param name="user1Id">The first user ID.</param>
    /// <param name="user2Id">The second user ID.</param>
    /// <returns>The PM channel, or <see langword="null"/> when none exists.</returns>
    Task<ChatChannel?> GetPmChannelAsync(int user1Id, int user2Id);

    /// <summary>
    /// Gets the membership row of a user in a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The membership row, or <see langword="null"/> when the user is not in the channel.</returns>
    Task<ChatUserChannel?> GetUserChannelAsync(int channelId, int userId);

    /// <summary>
    /// Gets the channel membership rows of a user, ordered by ascending channel ID.
    /// </summary>
    /// <param name="userId">The user whose memberships are listed.</param>
    /// <param name="includeHidden">Whether hidden channels (left PM and announcement channels) are included.</param>
    /// <param name="limit">The maximum number of rows to return; <see langword="null"/> returns all of them.</param>
    /// <returns>The user's membership rows.</returns>
    Task<IReadOnlyList<ChatUserChannel>> GetUserChannelsAsync(int userId, bool includeHidden = false, int? limit = null);

    /// <summary>
    /// Gets the IDs of every user with a membership row in a channel, hidden rows included.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The member user IDs.</returns>
    Task<IReadOnlyList<int>> GetChannelUserIdsAsync(int channelId);

    /// <summary>
    /// Adds a membership row, or unhides an existing one.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns><see langword="true"/> when the join has to be announced to the user.</returns>
    Task<bool> AddUserChannelAsync(int channelId, int userId);

    /// <summary>
    /// Removes a membership row, or hides it for hideable (PM and announcement)
    /// channels.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="hide">Whether the row should be hidden instead of deleted.</param>
    /// <returns><see langword="true"/> when the part has to be announced to the user.</returns>
    Task<bool> RemoveUserChannelAsync(int channelId, int userId, bool hide);

    /// <summary>
    /// Moves the read marker of a membership row forward (the marker never
    /// moves backwards).
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="messageId">The message ID to mark as read up to.</param>
    /// <returns><see langword="false"/> when the user has no membership row in the channel.</returns>
    Task<bool> MarkUserChannelAsReadAsync(int channelId, int userId, int messageId);

    /// <summary>
    /// Unhides every membership row of a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The number of rows that were hidden.</returns>
    Task<int> UnhideChannelAsync(int channelId);

    /// <summary>
    /// Gets channels by their IDs.
    /// </summary>
    /// <param name="channelIds">The channel IDs.</param>
    /// <returns>The matching channels, ordered by ascending channel ID.</returns>
    Task<IReadOnlyList<ChatChannel>> GetChannelsAsync(IReadOnlyList<int> channelIds);

    /// <summary>
    /// Updates the ID of the last message posted in a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateLastMessageIdAsync(int channelId, int messageId);

    /// <summary>
    /// Gets all joinable public channels.
    /// </summary>
    /// <returns>The public channels.</returns>
    Task<IReadOnlyList<ChatChannel>> GetPublicChannelsAsync();

    /// <summary>
    /// Creates a new chat channel.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <param name="description">The channel description.</param>
    /// <param name="type">The channel type.</param>
    /// <returns>The created channel.</returns>
    Task<ChatChannel> CreateChannelAsync(string name, string description, ChannelType type);

    /// <summary>
    /// Gets the existing PM channel between two users, or creates one.
    /// </summary>
    /// <param name="user1Id">The first user ID.</param>
    /// <param name="user2Id">The second user ID.</param>
    /// <returns>The existing or newly created PM channel.</returns>
    Task<ChatChannel> GetOrCreatePmChannelAsync(int user1Id, int user2Id);

    /// <summary>
    /// Creates a PM channel between two users.
    /// </summary>
    /// <param name="user1Id">The first user ID.</param>
    /// <param name="user2Id">The second user ID.</param>
    /// <returns>The created PM channel.</returns>
    Task<ChatChannel> CreatePmChannelAsync(int user1Id, int user2Id);

    /// <summary>
    /// Gets the existing multiplayer channel for a room, or creates one with the
    /// name <c>mp_{roomId}</c>.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <returns>The existing or newly created multiplayer channel.</returns>
    Task<ChatChannel> GetOrCreateMultiplayerChannelAsync(long roomId);

    /// <summary>
    /// Gets a chat message by its numeric ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>The message, or <see langword="null"/> when no such message exists.</returns>
    Task<ChatMessage?> GetMessageAsync(int messageId);

    /// <summary>
    /// Gets messages from a channel in chronological order.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="limit">The maximum number of messages to return (1-50).</param>
    /// <param name="since">When set, returns messages with an ID greater than this value.</param>
    /// <param name="until">When set, returns messages with an ID less than this value.</param>
    /// <returns>The messages in ascending ID order.</returns>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(int channelId, int limit, int? since = null, int? until = null);

    /// <summary>
    /// Gets the newest messages of several channels in a single query.
    /// </summary>
    /// <param name="channelIds">The channel IDs to load messages for.</param>
    /// <param name="limitPerChannel">The maximum number of messages returned per channel.</param>
    /// <returns>The messages, ordered by channel and ascending message ID.</returns>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(IReadOnlyList<int> channelIds, int limitPerChannel);

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="senderId">The sender user ID.</param>
    /// <param name="content">The message content.</param>
    /// <param name="type">The message type.</param>
    /// <param name="uuid">The optional client-generated UUID for deduplication.</param>
    /// <returns>The created message.</returns>
    Task<ChatMessage> CreateMessageAsync(int channelId, int senderId, string content, MessageType type, string? uuid = null);

    /// <summary>
    /// Gets the silence record for a user in a channel, if any.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The silence record, or <see langword="null"/> when the user is not silenced.</returns>
    Task<SilenceUser?> GetSilenceAsync(int channelId, int userId);

    /// <summary>
    /// Checks whether a user is silenced in a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns><see langword="true"/> when the user has a silence record in the channel.</returns>
    Task<bool> IsUserSilencedAsync(int channelId, int userId);

    /// <summary>
    /// Gets silence records matching the update criteria used by <c>chat/updates</c> and <c>chat/ack</c>.
    /// </summary>
    /// <param name="historySince">When set, returns silences with an ID greater than this value.</param>
    /// <param name="bannedSince">When set, returns silences issued after this timestamp.</param>
    /// <returns>The matching silence records.</returns>
    Task<IReadOnlyList<SilenceUser>> GetSilencesAsync(int? historySince = null, DateTimeOffset? bannedSince = null);

    /// <summary>
    /// Creates a new silence record.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="userId">The silenced user ID.</param>
    /// <param name="bannedAt">The timestamp at which the silence was issued.</param>
    /// <param name="until">The timestamp until which the user is silenced; <see langword="null"/> silences indefinitely.</param>
    /// <param name="reason">The optional silence reason.</param>
    /// <returns>The created silence record.</returns>
    Task<SilenceUser> CreateSilenceAsync(int channelId, int userId, DateTimeOffset bannedAt, DateTimeOffset? until = null, string? reason = null);
}