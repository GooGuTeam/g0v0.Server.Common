// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Database.Models;

/// <summary>
/// Represents the category of a room (g0v0! extended).
/// </summary>
public enum G0V0RoomCategory
{
    /// <summary>
    /// A regular playlist room.
    /// </summary>
    Normal,

    /// <summary>
    /// A room with spotlight beatmaps.
    /// </summary>
    Spotlight,

    /// <summary>
    /// A room with featured artist beatmaps.
    /// </summary>
    FeaturedArtist,

    /// <summary>
    /// The daily challenge room.
    /// </summary>
    DailyChallenge,

    /// <summary>
    /// A realtime multiplayer room.
    /// </summary>
    Realtime
}