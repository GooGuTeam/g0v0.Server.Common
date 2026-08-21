// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using Room = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Database.PostgreSQL.Repository;

/// <summary>
/// Persists multiplayer room state in the PostgreSQL schema.
/// </summary>
public class RoomRepository(PostgreSqlDbContext context) : IRoomRepository, IPostgreSqlRepository
{
    /// <inheritdoc/>
    public async Task<Room> CreateRoom(
        MultiplayerRoom room,
        int hostUserId,
        G0V0RoomCategory category = G0V0RoomCategory.Normal,
        bool tournamentMode = false)
    {
        bool userExists = await context.Users.AnyAsync(u => u.Id == hostUserId);
        if (!userExists)
        {
            throw new InvalidOperationException($"User with ID {hostUserId} does not exist.");
        }

        Room dbRoom = new()
        {
            Name = room.Settings.Name,
            HostId = hostUserId,
            Category = category,
            Status = RoomStatus.Idle,
            Type = room.Settings.MatchType,
            Password = room.Settings.Password,
            QueueMode = room.Settings.QueueMode,
            ParticipantCount = 1,
            AutoSkip = room.Settings.AutoSkip,
            AutoStartDuration = (int)room.Settings.AutoStartDuration.TotalSeconds,
            MaxParticipants = room.Settings.MaxParticipants,
            TournamentMode = tournamentMode,
        };
        context.Add(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
        await context.Entry(dbRoom).ReloadAsync().ConfigureAwait(false);

        foreach (MultiplayerPlaylistItem playlist in room.Playlist)
        {
            await AddItemToDb(playlist, dbRoom.Id, hostUserId).ConfigureAwait(false);
        }

        return await context.Rooms
            .Include(r => r.Playlists).ThenInclude(p => p.Beatmap)
            .FirstAsync(r => r.Id == dbRoom.Id).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Room?> GetRoom(long roomId)
    {
        return await context.Rooms
            .Include(r => r.Host)
            .Include(r => r.Playlists).ThenInclude(p => p.Beatmap)
            .FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddUserToRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} or Room with ID {roomId} does not exist.");
        }

        // Reject joins to ended rooms.
        Room? room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room?.EndsAt != null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} has ended.");
        }

        // clean up duplicate active records (keep only the earliest),
        // refresh the join time of the survivor, otherwise create a fresh participation record.
        List<RoomParticipatedUser> activeRecords = await context.RoomParticipatedUsers
            .Where(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null)
            .OrderBy(rpu => rpu.JoinedAt)
            .ToListAsync().ConfigureAwait(false);

        if (activeRecords.Count > 0)
        {
            foreach (RoomParticipatedUser? extra in activeRecords.Skip(1))
            {
                extra.LeftAt = DateTimeOffset.UtcNow;
                context.Update(extra);
            }

            activeRecords[0].JoinedAt = DateTimeOffset.UtcNow;
            context.Update(activeRecords[0]);
        }
        else
        {
            RoomParticipatedUser newRoomUser = new()
            {
                RoomId = roomId,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
                LeftAt = null,
            };
            context.Add(newRoomUser);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        await UpdateRoomParticipantCount(roomId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveUserFromRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} or Room with ID {roomId} does not exist.");
        }

        // If the room has already ended, return immediately.
        Room? room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room?.EndsAt != null)
        {
            return;
        }

        // Only the active participation record is relevant; historical records
        // (LeftAt != null) may exist from previous join/leave cycles.
        RoomParticipatedUser? roomUser = await context.RoomParticipatedUsers
            .SingleOrDefaultAsync(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null)
            .ConfigureAwait(false);

        if (roomUser == null || roomUser.LeftAt != null)
        {
            // User is not in the room; still check whether the room should end (idempotent).
            await UpdateRoomParticipantCount(roomId).ConfigureAwait(false);
            await EndRoomIfEmpty(roomId).ConfigureAwait(false);
            return;
        }

        roomUser.LeftAt = DateTimeOffset.UtcNow;
        context.Update(roomUser);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await UpdateRoomParticipantCount(roomId).ConfigureAwait(false);
        await EndRoomIfEmpty(roomId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateRoomHost(long roomId, int? hostUserId)
    {
        Room? dbRoom = await GetRoomForUpdate(roomId) ?? throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        dbRoom.HostId = hostUserId;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateRoomSettings(long roomId, MultiplayerRoomSettings settings)
    {
        Room? dbRoom = await GetRoomForUpdate(roomId) ?? throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        dbRoom.Name = settings.Name;
        dbRoom.Password = settings.Password;
        dbRoom.Type = settings.MatchType;
        dbRoom.QueueMode = settings.QueueMode;
        dbRoom.AutoSkip = settings.AutoSkip;
        dbRoom.AutoStartDuration = (int)settings.AutoStartDuration.TotalSeconds;
        dbRoom.MaxParticipants = settings.MaxParticipants;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateRoomStatus(long roomId, MultiplayerRoomState state)
    {
        Room? dbRoom = await GetRoomForUpdate(roomId) ?? throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        dbRoom.Status = state is MultiplayerRoomState.Playing or MultiplayerRoomState.WaitingForLoad
            ? RoomStatus.Playing
            : RoomStatus.Idle;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetRoomEndDate(long roomId, DateTimeOffset? endDate)
    {
        Room? dbRoom = await GetRoomForUpdate(roomId) ?? throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        dbRoom.EndsAt = endDate;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Playlist> AddItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        return !userAndRoomExists
            ? throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.")
            : await AddItemToDb(item, roomId, ownerId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Playlist> EditItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.");
        }

        Playlist? dbPlaylist = await context.Playlists
            .SingleOrDefaultAsync(p => p.Id == item.ID && p.RoomId == roomId).ConfigureAwait(false) ?? throw new InvalidOperationException($"Playlist item with ID {item.ID} does not exist in room {roomId}.");
        dbPlaylist.BeatmapId = item.BeatmapID;
        dbPlaylist.RulesetId = item.RulesetID;
        dbPlaylist.OwnerId = item.OwnerID;
        dbPlaylist.AllowedMods = item.AllowedMods.ToList();
        dbPlaylist.RequiredMods = item.RequiredMods.ToList();
        dbPlaylist.Freestyle = item.Freestyle;
        dbPlaylist.Expired = item.Expired;
        dbPlaylist.PlaylistOrder = item.PlaylistOrder;
        dbPlaylist.PlayedAt = item.PlayedAt;
        dbPlaylist.WinCondition = item.WinCondition;
        dbPlaylist.UpdatedAt = DateTimeOffset.UtcNow;
        context.Update(dbPlaylist);
        await context.SaveChangesAsync().ConfigureAwait(false);
        await context.Entry(dbPlaylist).ReloadAsync().ConfigureAwait(false);
        return dbPlaylist;
    }

    /// <inheritdoc/>
    public async Task<Playlist> RemoveItemToPlaylist(long itemId, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.");
        }

        Playlist? dbPlaylist = await context.Playlists
            .SingleOrDefaultAsync(p => p.Id == itemId && p.RoomId == roomId).ConfigureAwait(false) ?? throw new InvalidOperationException($"Playlist item with ID {itemId} does not exist in room {roomId}.");
        context.Playlists.Remove(dbPlaylist);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return dbPlaylist;
    }

    /// <summary>
    /// Checks whether the user has an active participation record in the room.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns><see langword="true"/> when the user is currently in the room; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> CheckUserInRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        return !userAndRoomExists
            ? false
            : await context.RoomParticipatedUsers
                .AnyAsync(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyScoreTokenExistsFor(long playlistItemId)
    {
        return await context.ScoreTokens
            .AnyAsync(t => t.PlaylistItemId == playlistItemId).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteRoom(long roomId)
    {
        List<RoomParticipatedUser> participants = await context.RoomParticipatedUsers
            .Where(rpu => rpu.RoomId == roomId).ToListAsync().ConfigureAwait(false);
        context.RoomParticipatedUsers.RemoveRange(participants);

        List<Playlist> playlists = await context.Playlists
            .Where(p => p.RoomId == roomId).ToListAsync().ConfigureAwait(false);
        context.Playlists.RemoveRange(playlists);

        Room? room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room != null)
        {
            context.Rooms.Remove(room);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<Playlist> AddItemToDb(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        Playlist dbPlaylist = new()
        {
            RoomId = roomId,
            BeatmapId = item.BeatmapID,
            RulesetId = item.RulesetID,
            AllowedMods = item.AllowedMods.ToList(),
            RequiredMods = item.RequiredMods.ToList(),
            Freestyle = item.Freestyle,
            Expired = item.Expired,
            OwnerId = ownerId,
            PlaylistOrder = item.PlaylistOrder,
            WinCondition = item.WinCondition,
        };
        context.Add(dbPlaylist);
        await context.SaveChangesAsync().ConfigureAwait(false);
        await context.Entry(dbPlaylist).ReloadAsync().ConfigureAwait(false);
        return dbPlaylist;
    }

    private async Task<Room?> GetRoomForUpdate(long roomId)
    {
        return await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
    }

    private async Task<bool> CheckUserAndRoomExists(long roomId, int userId)
    {
        return await context.Users.AnyAsync(u => u.Id == userId).ConfigureAwait(false) &&
               await context.Rooms.AnyAsync(r => r.Id == roomId).ConfigureAwait(false);
    }

    /// <summary>
    /// Recomputes and persists the active participant count.
    /// </summary>
    private async Task UpdateRoomParticipantCount(long roomId)
    {
        int count = await context.RoomParticipatedUsers
            .CountAsync(rpu => rpu.RoomId == roomId && rpu.LeftAt == null).ConfigureAwait(false);

        Room? room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room != null)
        {
            room.ParticipantCount = count;
            context.Update(room);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ends the room when no active participants remain.
    /// </summary>
    private async Task EndRoomIfEmpty(long roomId)
    {
        int count = await context.RoomParticipatedUsers
            .CountAsync(rpu => rpu.RoomId == roomId && rpu.LeftAt == null).ConfigureAwait(false);

        if (count != 0)
        {
            return;
        }

        Room? room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room is { EndsAt: null })
        {
            room.EndsAt = DateTimeOffset.UtcNow;
            room.ParticipantCount = 0;
            context.Update(room);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}