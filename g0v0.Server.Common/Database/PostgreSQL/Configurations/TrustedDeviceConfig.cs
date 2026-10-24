// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures <see cref="TrustedDevice"/> for PostgreSQL.
/// </summary>
public class TrustedDeviceConfig : IEntityTypeConfiguration<TrustedDevice>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.Property(record => record.UserAgent).HasColumnType("text");

        builder.HasOne(record => record.User)
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}