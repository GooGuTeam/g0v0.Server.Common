// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using osu.Game.Online.Chat;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="ChatChannel"/> to the legacy lazer API <c>chat_channels</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// <c>name</c> column name (which is not the EF convention-derived
/// <c>channel_name</c>) and the channel type stored as a MySQL native enum.
/// Everything else is left to EF Core conventions.
/// </para>
/// </summary>
public class ChatChannelConfig : IEntityTypeConfiguration<ChatChannel>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        builder.ToTable("chat_channels");

        // The name column is NOT the EF convention-derived channel_name;
        // the legacy schema calls it name.
        builder.Property(channel => channel.Name)
            .HasColumnName("name")
            .HasMaxLength(50);

        builder.Property(channel => channel.Description)
            .HasMaxLength(255);

        // Enums are stored as native MySQL enum columns in the legacy schema,
        // using UPPER_SNAKE_CASE values (e.g. PUBLIC, PM, GROUP).
        // Columns added for upstream parity; the legacy table has to be altered
        // before the MySQL backend can use them:
        // ALTER TABLE chat_channels ADD COLUMN last_message_id INT NULL, ADD COLUMN moderated TINYINT(1) NOT NULL DEFAULT 0;
        builder.Property(channel => channel.LastMessageId)
            .HasColumnName("last_message_id");

        builder.Property(channel => channel.Moderated)
            .HasColumnName("moderated");

        builder.Property(channel => channel.Type)
            .HasColumnType("enum('PUBLIC','PRIVATE','MULTIPLAYER','SPECTATOR','TEMPORARY','PM','GROUP','SYSTEM','ANNOUNCE','TEAM')")
            .HasConversion(
                value => ConfigurationHelper.ConvertEnumToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToEnum<ChannelType>(value))
            .IsRequired();
    }
}