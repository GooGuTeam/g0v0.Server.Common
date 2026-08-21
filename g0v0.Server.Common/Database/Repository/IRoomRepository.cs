// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using Room = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Database.Repository;

public interface IRoomRepository
{
    public Task<Room> CreateRoom(
        MultiplayerRoom room,
        int hostUserId,
        G0V0RoomCategory category = G0V0RoomCategory.Normal,
        bool tournamentMode = false);

    public Task<Room?> GetRoom(long roomId);

    public Task AddUserToRoom(long roomId, int userId);

    public Task RemoveUserFromRoom(long roomId, int userId);

    public Task UpdateRoomHost(long roomId, int? hostUserId);

    public Task UpdateRoomSettings(long roomId, MultiplayerRoomSettings settings);

    public Task UpdateRoomStatus(long roomId, MultiplayerRoomState state);

    public Task SetRoomEndDate(long roomId, DateTimeOffset? endDate);

    public Task<Playlist> AddItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId);

    public Task<Playlist> EditItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId);

    public Task<Playlist> RemoveItemToPlaylist(long itemId, long roomId, int ownerId);

    public Task<bool> AnyScoreTokenExistsFor(long playlistItemId);

    /// <summary>
    /// Deletes a room and all of its dependent records (playlist items and
    /// participants). Used to clean up orphaned rooms when creation fails.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteRoom(long roomId);
}