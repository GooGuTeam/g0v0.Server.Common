// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Maps <see cref="SilenceUser"/> to the legacy lazer API <c>chat_silence_users</c> table.
///
/// <para>
/// Only explicit mappings that EF Core cannot infer are declared here: the
/// <c>datetime</c> timestamp columns. Everything else is left to EF Core
/// conventions.
/// </para>
/// </summary>
public class ChatSilenceUserConfig : IEntityTypeConfiguration<SilenceUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SilenceUser> builder)
    {
        builder.ToTable("chat_silence_users");

        builder.Property(silence => silence.Until)
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(silence => silence.BannedAt)
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(silence => silence.Reason)
            .HasMaxLength(255);
    }
}