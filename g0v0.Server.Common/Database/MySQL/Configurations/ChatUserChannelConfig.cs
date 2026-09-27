// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="ChatUserChannel"/> to the <c>chat_user_channels</c> table.
///
/// <para>
/// The legacy schema has no equivalent table; it has to be created before the
/// MySQL backend can serve chat listings:
/// </para>
/// <code>
/// CREATE TABLE chat_user_channels (
///   user_id INT NOT NULL,
///   channel_id INT NOT NULL,
///   hidden TINYINT(1) NOT NULL DEFAULT 0,
///   last_read_id INT NULL,
///   PRIMARY KEY (user_id, channel_id),
///   INDEX (channel_id),
///   INDEX (hidden)
/// );
/// </code>
/// </summary>
public class ChatUserChannelConfig : IEntityTypeConfiguration<ChatUserChannel>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ChatUserChannel> builder)
    {
        builder.ToTable("chat_user_channels");

        builder.HasKey(userChannel => new { userChannel.UserId, userChannel.ChannelId });
    }
}