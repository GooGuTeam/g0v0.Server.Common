// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="ChatMessage"/> to the legacy lazer API <c>chat_messages</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// message type stored as a MySQL native enum, the <c>datetime</c> timestamp
/// column, and the foreign keys to <c>chat_channels</c>/<c>lazer_users</c>.
/// Everything else is left to EF Core conventions.
/// </para>
/// </summary>
public class ChatMessageConfig : IEntityTypeConfiguration<ChatMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.Property(message => message.Content)
            .HasMaxLength(1000);

        // Column added for upstream parity; the legacy table has to be altered
        // before the MySQL backend can use it:
        // ALTER TABLE chat_messages ADD COLUMN uuid VARCHAR(36) NULL;
        builder.Property(message => message.Uuid)
            .HasColumnName("uuid")
            .HasMaxLength(36);

        builder.Property(message => message.Timestamp)
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        // Enums are stored as native MySQL enum columns in the legacy schema,
        // using UPPER_SNAKE_CASE values (e.g. PLAIN, ACTION, MARKDOWN).
        builder.Property(message => message.Type)
            .HasColumnType("enum('ACTION','MARKDOWN','PLAIN')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<MessageType>(value))
            .IsRequired();

        // The legacy schema's FK column names are channel_id and sender_id,
        // both already matching EF conventions, but the sender FK targets
        // lazer_users instead of users.
        builder.HasOne(message => message.Channel)
            .WithMany()
            .HasForeignKey(message => message.ChannelId);

        builder.HasIndex(message => message.ChannelId);
        builder.HasIndex(message => message.SenderId);
        builder.HasIndex(message => message.Timestamp);
    }
}