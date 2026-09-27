// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="ChatChannel"/> to the v2 PostgreSQL <c>chat_channels</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// channel type stored as a string. Everything else is left to EF Core
/// conventions.
/// </para>
/// </summary>
public class ChatChannelConfig : IEntityTypeConfiguration<ChatChannel>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        builder.ToTable("chat_channels");

        // Enums are stored as human-readable strings in the v2 schema.
        builder.Property(channel => channel.Type)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}