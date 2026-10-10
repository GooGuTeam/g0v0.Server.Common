// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="RoomParticipatedUser"/> to the v2 PostgreSQL <c>room_participated_users</c> table.
///
/// <para>
/// Participation records are keyed by the composite (<c>room_id</c>, <c>user_id</c>) primary
/// key on both backends; there is no surrogate <c>id</c> column.
/// </para>
/// </summary>
public class RoomParticipatedUserConfig : IEntityTypeConfiguration<RoomParticipatedUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoomParticipatedUser> builder)
    {
        builder.ToTable("room_participated_users");

        builder.HasKey(participant => new { participant.RoomId, participant.UserId });
    }
}