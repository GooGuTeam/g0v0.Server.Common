// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="ChatUserChannel"/> to the v2 PostgreSQL <c>chat_user_channels</c> table.
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