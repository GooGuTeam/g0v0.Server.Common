// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures <see cref="LoginSession"/> for PostgreSQL.
/// </summary>
public class LoginSessionConfig : IEntityTypeConfiguration<LoginSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LoginSession> builder)
    {
        builder.Property(record => record.UserAgent).HasColumnType("text");

        builder.HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(session => session.Device)
            .WithMany(device => device.Sessions)
            .HasForeignKey(session => session.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(session => session.Token)
            .WithMany()
            .HasForeignKey(session => session.TokenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}