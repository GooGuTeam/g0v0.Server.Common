// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using DbRoom = g0v0.Server.Common.Database.Models.Room;
using G0V0RoomCategory = g0v0.Server.Common.Database.Models.G0V0RoomCategory;
using OsuMatchType = osu.Game.Online.Rooms.MatchType;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="Room"/> to the legacy lazer API <c>rooms</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here:
/// the snake_case table name, enum columns stored as MySQL native enums,
/// <see cref="DateTimeOffset"/> columns stored as <c>datetime</c>, the
/// <c>host_id</c> column name, and the ignored legacy columns.
/// </para>
/// </summary>
public class RoomConfig : IEntityTypeConfiguration<DbRoom>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbRoom> builder)
    {
        builder.ToTable("rooms");

        // Enums are stored as native MySQL enum columns in the legacy schema,
        // using UPPER_SNAKE_CASE values (e.g. `HOST_ONLY`, `HEAD_TO_HEAD`).
        builder.Property(room => room.Category)
            .HasColumnType("enum('NORMAL','SPOTLIGHT','FEATURED_ARTIST','DAILY_CHALLENGE','REALTIME')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<G0V0RoomCategory>(value));

        builder.Property(room => room.Type)
            .HasColumnType("enum('PLAYLISTS','HEAD_TO_HEAD','TEAM_VERSUS','MATCHMAKING')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<OsuMatchType>(value));

        builder.Property(room => room.QueueMode)
            .HasColumnType("enum('HOST_ONLY','ALL_PLAYERS','ALL_PLAYERS_ROUND_ROBIN')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<QueueMode>(value));

        builder.Property(room => room.Status)
            .HasColumnType("enum('IDLE','PLAYING')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<RoomStatus>(value));

        // The legacy schema stores UTC timestamps in `datetime` columns.
        builder.Property(room => room.StartsAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(room => room.EndsAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));

        // The legacy lazer API names the host FK column `host_id` instead of the
        // EF convention `host_id` is already correct, but keep this explicit for
        // documentation purposes. `auto_start_duration` is already the EF
        // convention-derived snake_case name, so no explicit column name is needed.
        builder.Property(room => room.HostId)
            .HasColumnName("host_id");

        // The legacy `rooms` table has no `max_participants` or `tournament_mode`
        // columns; both are v2-only fields.
        builder.Ignore(room => room.MaxParticipants);
        builder.Ignore(room => room.TournamentMode);
    }
}