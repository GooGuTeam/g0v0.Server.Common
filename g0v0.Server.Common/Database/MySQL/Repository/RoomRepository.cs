// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using Room = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Database.MySQL.Repository;

public class RoomRepository(MysqlDbContext context) : IRoomRepository, IMySqlRepository
{
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

        var dbRoom = new Room
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

        foreach (var playlist in room.Playlist)
        {
            await AddItemToDb(playlist, dbRoom.Id, hostUserId).ConfigureAwait(false);
        }

        return await context.Rooms
            .Include(r => r.Playlists).ThenInclude(p => p.Beatmap)
            .FirstAsync(r => r.Id == dbRoom.Id).ConfigureAwait(false);
    }

    public async Task<Room?> GetRoom(long roomId)
    {
        return await context.Rooms
            .Include(r => r.Host)
            .Include(r => r.Playlists).ThenInclude(p => p.Beatmap)
            .FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
    }

    public async Task AddUserToRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} or Room with ID {roomId} does not exist.");
        }

        // Reject joins to ended rooms.
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room?.EndsAt != null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} has ended.");
        }

        // clean up duplicate active records (keep only the earliest),
        // refresh the join time of the survivor, otherwise create a fresh participation record.
        var activeRecords = await context.RoomParticipatedUsers
            .Where(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null)
            .OrderBy(rpu => rpu.JoinedAt)
            .ToListAsync().ConfigureAwait(false);

        if (activeRecords.Count > 0)
        {
            foreach (var extra in activeRecords.Skip(1))
            {
                extra.LeftAt = DateTimeOffset.UtcNow;
                context.Update(extra);
            }

            activeRecords[0].JoinedAt = DateTimeOffset.UtcNow;
            context.Update(activeRecords[0]);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        else
        {
            var newRoomUser = new RoomParticipatedUser
            {
                RoomId = roomId,
                UserId = userId,
                JoinedAt = DateTimeOffset.UtcNow,
                LeftAt = null,
            };
            context.Add(newRoomUser);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        await UpdateRoomParticipantCount(roomId).ConfigureAwait(false);
    }

    public async Task RemoveUserFromRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} or Room with ID {roomId} does not exist.");
        }

        // If the room has already ended, return immediately.
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room?.EndsAt != null)
        {
            return;
        }

        // Only the active participation record is relevant; historical records
        // (LeftAt != null) may exist from previous join/leave cycles.
        var roomUser = await context.RoomParticipatedUsers
            .SingleOrDefaultAsync(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null).ConfigureAwait(false);

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

    public async Task UpdateRoomHost(long roomId, int? hostUserId)
    {
        var dbRoom = await GetRoomForUpdate(roomId);
        if (dbRoom == null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        }

        dbRoom.HostId = hostUserId;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateRoomSettings(long roomId, MultiplayerRoomSettings settings)
    {
        var dbRoom = await GetRoomForUpdate(roomId);
        if (dbRoom == null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        }

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

    public async Task UpdateRoomStatus(long roomId, MultiplayerRoomState state)
    {
        var dbRoom = await GetRoomForUpdate(roomId);
        if (dbRoom == null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        }

        dbRoom.Status = state == MultiplayerRoomState.Playing || state == MultiplayerRoomState.WaitingForLoad
            ? RoomStatus.Playing
            : RoomStatus.Idle;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SetRoomEndDate(long roomId, DateTimeOffset? endDate)
    {
        var dbRoom = await GetRoomForUpdate(roomId);
        if (dbRoom == null)
        {
            throw new InvalidOperationException($"Room with ID {roomId} does not exist.");
        }

        dbRoom.EndsAt = endDate;
        context.Update(dbRoom);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<Playlist> AddItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        return !userAndRoomExists
            ? throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.")
            : await AddItemToDb(item, roomId, ownerId).ConfigureAwait(false);
    }

    public async Task<Playlist> EditItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.");
        }

        var dbPlaylist = await context.Playlists
            .SingleOrDefaultAsync(p => p.Id == item.ID && p.RoomId == roomId).ConfigureAwait(false);
        if (dbPlaylist == null)
        {
            throw new InvalidOperationException($"Playlist item with ID {item.ID} does not exist in room {roomId}.");
        }

        dbPlaylist.BeatmapId = item.BeatmapID;
        dbPlaylist.RulesetId = item.RulesetID;
        dbPlaylist.OwnerId = item.OwnerID;
        dbPlaylist.AllowedMods = item.AllowedMods?.ToList() ?? new List<osu.Game.Online.API.APIMod>();
        dbPlaylist.RequiredMods = item.RequiredMods?.ToList() ?? new List<osu.Game.Online.API.APIMod>();
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

    public async Task<Playlist> RemoveItemToPlaylist(long itemId, long roomId, int ownerId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, ownerId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            throw new InvalidOperationException($"User with ID {ownerId} or Room with ID {roomId} does not exist.");
        }

        var dbPlaylist = await context.Playlists
            .SingleOrDefaultAsync(p => p.Id == itemId && p.RoomId == roomId).ConfigureAwait(false);
        if (dbPlaylist == null)
        {
            throw new InvalidOperationException($"Playlist item with ID {itemId} does not exist in room {roomId}.");
        }

        context.Playlists.Remove(dbPlaylist);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return dbPlaylist;
    }

    public async Task<bool> CheckUserInRoom(long roomId, int userId)
    {
        bool userAndRoomExists = await CheckUserAndRoomExists(roomId, userId).ConfigureAwait(false);
        if (!userAndRoomExists)
        {
            return false;
        }

        return await context.RoomParticipatedUsers
            .AnyAsync(rpu => rpu.RoomId == roomId && rpu.UserId == userId && rpu.LeftAt == null).ConfigureAwait(false);
    }

    public async Task<bool> AnyScoreTokenExistsFor(long playlistItemId)
    {
        return await context.ScoreTokens
            .AnyAsync(t => t.PlaylistItemId == playlistItemId).ConfigureAwait(false);
    }

    public async Task DeleteRoom(long roomId)
    {
        var participants = await context.RoomParticipatedUsers
            .Where(rpu => rpu.RoomId == roomId).ToListAsync().ConfigureAwait(false);
        context.RoomParticipatedUsers.RemoveRange(participants);

        var playlists = await context.Playlists
            .Where(p => p.RoomId == roomId).ToListAsync().ConfigureAwait(false);
        context.Playlists.RemoveRange(playlists);

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room != null)
        {
            context.Rooms.Remove(room);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<Playlist> AddItemToDb(MultiplayerPlaylistItem item, long roomId, int ownerId)
    {
        var dbPlaylist = new Playlist
        {
            RoomId = roomId,
            BeatmapId = item.BeatmapID,
            RulesetId = item.RulesetID,
            AllowedMods = item.AllowedMods?.ToList() ?? new List<osu.Game.Online.API.APIMod>(),
            RequiredMods = item.RequiredMods?.ToList() ?? new List<osu.Game.Online.API.APIMod>(),
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

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
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

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId).ConfigureAwait(false);
        if (room != null && room.EndsAt == null)
        {
            room.EndsAt = DateTimeOffset.UtcNow;
            room.ParticipantCount = 0;
            context.Update(room);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}