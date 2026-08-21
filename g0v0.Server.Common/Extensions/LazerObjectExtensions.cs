// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Online.Rooms;

namespace g0v0.Server.Common.Extensions;

/// <summary>
/// Provides conversion helpers for lazer objects.
/// </summary>
public static class LazerObjectExtensions
{
    /// <summary>
    /// Converts a lazer <see cref="RoomCategory"/> into a g0v0 <see cref="G0V0RoomCategory"/>.
    /// </summary>
    /// <param name="category">The lazer room category.</param>
    /// <param name="realtime">Whether the room is a realtime multiplayer room.</param>
    /// <returns>The equivalent g0v0 room category.</returns>
    public static G0V0RoomCategory ToG0V0RoomCategory(this RoomCategory category, bool realtime = false)
    {
        return realtime
            ? G0V0RoomCategory.Realtime
            : category switch
            {
                RoomCategory.Spotlight => G0V0RoomCategory.Spotlight,
                RoomCategory.FeaturedArtist => G0V0RoomCategory.FeaturedArtist,
                RoomCategory.DailyChallenge => G0V0RoomCategory.DailyChallenge,
                _ => G0V0RoomCategory.Normal,
            };
    }
}