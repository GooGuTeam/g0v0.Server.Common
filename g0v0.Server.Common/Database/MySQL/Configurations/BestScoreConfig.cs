// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="BestScore"/> entity mapping for the legacy MySQL schema.
/// </summary>
public class BestScoreConfig : IEntityTypeConfiguration<BestScore>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BestScore> builder)
    {
        builder.Property(score => score.Mode)
            .HasColumnName("gamemode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value))
            .IsRequired();

        builder.Property(score => score.Pp).HasColumnType("float");
        builder.Property(score => score.Accuracy).HasColumnName("acc").HasColumnType("float");
    }
}