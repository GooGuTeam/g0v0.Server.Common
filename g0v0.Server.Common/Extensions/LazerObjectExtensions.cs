// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using osu.Game.Online.Rooms;

namespace g0v0.Server.Common.Extensions;

public static class LazerObjectExtensions
{
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