// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="RoomParticipatedUser"/> to the legacy lazer API <c>room_participated_users</c> table.
///
/// <para>
/// The legacy table's primary key is the composite (<c>room_id</c>, <c>user_id</c>) and it has no
/// surrogate <c>id</c> column. The legacy schema stores UTC timestamps in <c>datetime</c> columns.
/// </para>
/// </summary>
public class RoomParticipatedUserConfig : IEntityTypeConfiguration<RoomParticipatedUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoomParticipatedUser> builder)
    {
        builder.ToTable("room_participated_users");

        // The legacy table identifies a participation record by (room_id, user_id).
        builder.HasKey(participant => new { participant.RoomId, participant.UserId });

        // The legacy schema stores UTC timestamps in `datetime` columns.
        builder.Property(participant => participant.JoinedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertDateTimeToDateTimeOffset(value));

        builder.Property(participant => participant.LeftAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => ConfigurationHelper.ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConfigurationHelper.ConvertNullableDateTimeToDateTimeOffset(value));
    }
}