// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using Room = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>
/// Persists multiplayer room state, including playlist items and participants.
/// </summary>
public interface IRoomRepository
{
    /// <summary>
    /// Creates a room owned by the given host user.
    /// </summary>
    /// <param name="room">The multiplayer room to persist.</param>
    /// <param name="hostUserId">The user ID of the room host.</param>
    /// <param name="category">The room category.</param>
    /// <param name="tournamentMode">Whether the room runs in tournament mode.</param>
    /// <returns>The created room with its playlist items.</returns>
    public Task<Room> CreateRoom(
        MultiplayerRoom room,
        int hostUserId,
        G0V0RoomCategory category = G0V0RoomCategory.Normal,
        bool tournamentMode = false);

    /// <summary>
    /// Gets a room by its ID, including its host and playlist items.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <returns>The room, or <see langword="null"/> when no such room exists.</returns>
    public Task<Room?> GetRoom(long roomId);

    /// <summary>
    /// Adds the user to the room, refreshing an existing participation record or creating a new one.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddUserToRoom(long roomId, int userId);

    /// <summary>
    /// Removes the user from the room and ends the room when no active participants remain.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveUserFromRoom(long roomId, int userId);

    /// <summary>
    /// Updates the host of a room.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="hostUserId">The new host user ID, or <see langword="null"/> to clear it.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateRoomHost(long roomId, int? hostUserId);

    /// <summary>
    /// Updates the mutable settings of a room.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="settings">The new room settings.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateRoomSettings(long roomId, MultiplayerRoomSettings settings);

    /// <summary>
    /// Updates the status of a room based on the given multiplayer room state.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="state">The multiplayer room state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateRoomStatus(long roomId, MultiplayerRoomState state);

    /// <summary>
    /// Sets the end date of a room.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <param name="endDate">The end date, or <see langword="null"/> to clear it.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetRoomEndDate(long roomId, DateTimeOffset? endDate);

    /// <summary>
    /// Adds a playlist item to a room.
    /// </summary>
    /// <param name="item">The playlist item to add.</param>
    /// <param name="roomId">The room ID.</param>
    /// <param name="ownerId">The user ID of the playlist item owner.</param>
    /// <returns>The persisted playlist item.</returns>
    public Task<Playlist> AddItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId);

    /// <summary>
    /// Updates an existing playlist item in a room.
    /// </summary>
    /// <param name="item">The playlist item with updated values.</param>
    /// <param name="roomId">The room ID.</param>
    /// <param name="ownerId">The user ID of the playlist item owner.</param>
    /// <returns>The updated playlist item.</returns>
    public Task<Playlist> EditItemToPlaylist(MultiplayerPlaylistItem item, long roomId, int ownerId);

    /// <summary>
    /// Removes a playlist item from a room.
    /// </summary>
    /// <param name="itemId">The playlist item ID.</param>
    /// <param name="roomId">The room ID.</param>
    /// <param name="ownerId">The user ID of the playlist item owner.</param>
    /// <returns>The removed playlist item.</returns>
    public Task<Playlist> RemoveItemToPlaylist(long itemId, long roomId, int ownerId);

    /// <summary>
    /// Checks whether any score token is associated with the given playlist item.
    /// </summary>
    /// <param name="playlistItemId">The playlist item ID.</param>
    /// <returns><see langword="true"/> when at least one score token exists; otherwise, <see langword="false"/>.</returns>
    public Task<bool> AnyScoreTokenExistsFor(long playlistItemId);

    /// <summary>
    /// Deletes a room and all of its dependent records (playlist items and
    /// participants). Used to clean up orphaned rooms when creation fails.
    /// </summary>
    /// <param name="roomId">The room ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteRoom(long roomId);
}