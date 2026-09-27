// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.Chat;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// Persists chat channels, messages and silences in the v2 PostgreSQL schema.
/// </summary>
/// <param name="context">The PostgreSQL database context.</param>
public class ChatRepository(PostgreSqlDbContext context) : IChatRepository, IPostgreSqlRepository
{
    /// <inheritdoc />
    public Task<ChatChannel?> GetChannelAsync(int channelId)
    {
        return context.ChatChannels.FirstOrDefaultAsync(channel => channel.ChannelId == channelId);
    }

    /// <inheritdoc />
    public Task<ChatChannel?> GetChannelByNameAsync(string name)
    {
        return context.ChatChannels.FirstOrDefaultAsync(channel => channel.Name == name);
    }

    /// <inheritdoc />
    public async Task<ChatChannel?> GetChannelAsync(string channel)
    {
        return int.TryParse(channel, out int channelId)
            ? await GetChannelAsync(channelId) ?? await GetChannelByNameAsync(channel)
            : await GetChannelByNameAsync(channel);
    }

    /// <inheritdoc />
    public async Task<ChatChannel?> GetPmChannelAsync(int user1Id, int user2Id)
    {
        return await GetChannelByNameAsync($"pm_{user1Id}_{user2Id}")
               ?? await GetChannelByNameAsync($"pm_{user2Id}_{user1Id}");
    }

    /// <inheritdoc />
    public Task<ChatUserChannel?> GetUserChannelAsync(int channelId, int userId)
    {
        return context.ChatUserChannels.FirstOrDefaultAsync(
            userChannel => userChannel.ChannelId == channelId && userChannel.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatUserChannel>> GetUserChannelsAsync(int userId, bool includeHidden = false, int? limit = null)
    {
        IQueryable<ChatUserChannel> query = context.ChatUserChannels
            .Where(userChannel => userChannel.UserId == userId);

        if (!includeHidden)
        {
            query = query.Where(userChannel => !userChannel.Hidden);
        }

        query = query.OrderBy(userChannel => userChannel.ChannelId);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetChannelUserIdsAsync(int channelId)
    {
        return await context.ChatUserChannels
            .Where(userChannel => userChannel.ChannelId == channelId)
            .Select(userChannel => userChannel.UserId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AddUserChannelAsync(int channelId, int userId)
    {
        ChatUserChannel? existing = await GetUserChannelAsync(channelId, userId);

        if (existing == null)
        {
            context.ChatUserChannels.Add(new ChatUserChannel { ChannelId = channelId, UserId = userId });
        }
        else if (existing.Hidden)
        {
            existing.Hidden = false;
        }
        else
        {
            return false;
        }

        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveUserChannelAsync(int channelId, int userId, bool hide)
    {
        ChatUserChannel? existing = await GetUserChannelAsync(channelId, userId);

        if (existing == null || (hide && existing.Hidden))
        {
            return false;
        }

        if (hide)
        {
            existing.Hidden = true;
        }
        else
        {
            context.ChatUserChannels.Remove(existing);
        }

        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkUserChannelAsReadAsync(int channelId, int userId, int messageId)
    {
        ChatUserChannel? existing = await GetUserChannelAsync(channelId, userId);

        if (existing == null)
        {
            return false;
        }

        // The read marker never moves backwards.
        int lastReadId = Math.Max(existing.LastReadId ?? 0, messageId);
        if (existing.LastReadId == lastReadId)
        {
            return true;
        }

        existing.LastReadId = lastReadId;
        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<int> UnhideChannelAsync(int channelId)
    {
        List<ChatUserChannel> hidden = await context.ChatUserChannels
            .Where(userChannel => userChannel.ChannelId == channelId && userChannel.Hidden)
            .ToListAsync();

        if (hidden.Count == 0)
        {
            return 0;
        }

        foreach (ChatUserChannel userChannel in hidden)
        {
            userChannel.Hidden = false;
        }

        await context.SaveChangesAsync();
        return hidden.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatChannel>> GetChannelsAsync(IReadOnlyList<int> channelIds)
    {
        return channelIds.Count == 0
            ? []
            : await context.ChatChannels
                .Where(channel => channelIds.Contains(channel.ChannelId))
                .OrderBy(channel => channel.ChannelId)
                .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateLastMessageIdAsync(int channelId, int messageId)
    {
        ChatChannel? channel = await context.ChatChannels
            .FirstOrDefaultAsync(candidate => candidate.ChannelId == channelId);

        if (channel == null)
        {
            return;
        }

        channel.LastMessageId = messageId;
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatChannel>> GetPublicChannelsAsync()
    {
        return await context.ChatChannels
            .Where(channel => channel.Type == ChannelType.Public)
            .OrderBy(channel => channel.ChannelId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ChatChannel> CreateChannelAsync(string name, string description, ChannelType type)
    {
        ChatChannel channel = new()
        {
            Name = name,
            Description = description,
            Type = type,
        };
        context.ChatChannels.Add(channel);
        await context.SaveChangesAsync();
        await context.Entry(channel).ReloadAsync();
        return channel;
    }

    /// <inheritdoc />
    public async Task<ChatChannel> GetOrCreatePmChannelAsync(int user1Id, int user2Id)
    {
        return await GetPmChannelAsync(user1Id, user2Id)
               ?? await CreatePmChannelAsync(user1Id, user2Id);
    }

    /// <inheritdoc />
    public Task<ChatChannel> CreatePmChannelAsync(int user1Id, int user2Id)
    {
        return CreateChannelAsync($"pm_{user1Id}_{user2Id}", "Private message channel", ChannelType.PM);
    }

    /// <inheritdoc />
    public async Task<ChatChannel> GetOrCreateMultiplayerChannelAsync(long roomId)
    {
        ChatChannel? existing = await GetChannelByNameAsync($"mp_{roomId}");
        return existing ?? await CreateChannelAsync(
            $"mp_{roomId}",
            $"Multiplayer room {roomId} chat",
            ChannelType.Multiplayer);
    }

    /// <inheritdoc />
    public Task<ChatMessage?> GetMessageAsync(int messageId)
    {
        return context.ChatMessages
            .Include(message => message.Channel)
            .FirstOrDefaultAsync(message => message.MessageId == messageId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(int channelId, int limit, int? since = null, int? until = null)
    {
        IQueryable<ChatMessage> query = context.ChatMessages
            .Where(message => message.ChannelId == channelId)
            .Include(message => message.Channel);

        if (since.HasValue)
        {
            return await query
                .Where(message => message.MessageId > since.Value)
                .OrderBy(message => message.MessageId)
                .Take(limit)
                .ToListAsync();
        }

        if (until.HasValue)
        {
            List<ChatMessage> messages = await query
                .Where(message => message.MessageId < until.Value)
                .OrderByDescending(message => message.MessageId)
                .Take(limit)
                .ToListAsync();
            messages.Reverse();
            return messages;
        }

        List<ChatMessage> recent = await query
            .OrderByDescending(message => message.MessageId)
            .Take(limit)
            .ToListAsync();
        recent.Reverse();
        return recent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(IReadOnlyList<int> channelIds, int limitPerChannel)
    {
        if (channelIds.Count == 0)
        {
            return [];
        }

        // A correlated count selects the newest messages of every requested
        // channel in a single round trip on both providers.
        return await context.ChatMessages
            .Where(message => channelIds.Contains(message.ChannelId)
                && context.ChatMessages.Count(other =>
                    other.ChannelId == message.ChannelId && other.MessageId > message.MessageId) < limitPerChannel)
            .OrderBy(message => message.ChannelId)
            .ThenBy(message => message.MessageId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ChatMessage> CreateMessageAsync(int channelId, int senderId, string content, MessageType type, string? uuid = null)
    {
        ChatMessage message = new()
        {
            ChannelId = channelId,
            SenderId = senderId,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow,
            Type = type,
            Uuid = uuid,
        };
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();
        await context.Entry(message).ReloadAsync();
        return message;
    }

    /// <inheritdoc />
    public Task<SilenceUser?> GetSilenceAsync(int channelId, int userId)
    {
        return context.ChatSilenceUsers.FirstOrDefaultAsync(silence =>
            silence.ChannelId == channelId && silence.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> IsUserSilencedAsync(int channelId, int userId)
    {
        return await context.ChatSilenceUsers.AnyAsync(silence =>
            silence.ChannelId == channelId && silence.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SilenceUser>> GetSilencesAsync(int? historySince = null, DateTimeOffset? bannedSince = null)
    {
        IQueryable<SilenceUser> query = context.ChatSilenceUsers.AsQueryable();

        if (historySince.HasValue)
        {
            query = query.Where(silence => silence.Id > historySince.Value);
        }
        else if (bannedSince.HasValue)
        {
            query = query.Where(silence => silence.BannedAt > bannedSince.Value);
        }

        return await query.OrderBy(silence => silence.Id).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SilenceUser> CreateSilenceAsync(int channelId, int userId, DateTimeOffset bannedAt, DateTimeOffset? until = null, string? reason = null)
    {
        SilenceUser silence = new()
        {
            ChannelId = channelId,
            UserId = userId,
            BannedAt = bannedAt,
            Until = until ?? DateTimeOffset.MaxValue,
            Reason = reason,
        };
        context.ChatSilenceUsers.Add(silence);
        await context.SaveChangesAsync();
        await context.Entry(silence).ReloadAsync();
        return silence;
    }
}