// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="ChatMessage"/> to the v2 PostgreSQL <c>chat_messages</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// message type stored as a string and the foreign keys to <c>users</c> and
/// <c>chat_channels</c>. Everything else is left to EF Core conventions.
/// </para>
/// </summary>
public class ChatMessageConfig : IEntityTypeConfiguration<ChatMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.Property(message => message.Uuid)
            .HasMaxLength(36);

        // Enums are stored as human-readable strings in the v2 schema.
        builder.Property(message => message.Type)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(message => message.Channel)
            .WithMany()
            .HasForeignKey(message => message.ChannelId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderId);
    }
}