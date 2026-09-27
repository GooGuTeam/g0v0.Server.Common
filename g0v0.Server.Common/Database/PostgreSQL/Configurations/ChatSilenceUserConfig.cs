// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="SilenceUser"/> to the v2 PostgreSQL <c>chat_silence_users</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// foreign keys to <c>users</c> and <c>chat_channels</c>. Everything else is
/// left to EF Core conventions.
/// </para>
/// </summary>
public class ChatSilenceUserConfig : IEntityTypeConfiguration<SilenceUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SilenceUser> builder)
    {
        builder.ToTable("chat_silence_users");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(silence => silence.UserId);

        builder.HasOne<ChatChannel>()
            .WithMany()
            .HasForeignKey(silence => silence.ChannelId);
    }
}