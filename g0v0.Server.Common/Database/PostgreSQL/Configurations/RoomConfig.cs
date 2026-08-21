// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DbRoom = g0v0.Server.Common.Database.Models.Room;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="DbRoom"/> to the v2 PostgreSQL <c>rooms</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: enum
/// columns stored as strings and the <c>now()</c> default for <c>starts_at</c>.
/// </para>
/// </summary>
public class RoomConfig : IEntityTypeConfiguration<DbRoom>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbRoom> builder)
    {
        builder.ToTable("rooms");

        // Enums are stored as human-readable strings in the v2 schema.
        builder.Property(room => room.Category)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(room => room.Type)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(room => room.QueueMode)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(room => room.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(room => room.StartsAt)
            .HasDefaultValueSql("now()");

        builder.Property(room => room.Password)
            .HasMaxLength(40);
    }
}