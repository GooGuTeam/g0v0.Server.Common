// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

public class ScoreConfig : IEntityTypeConfiguration<Score>
{
    public void Configure(EntityTypeBuilder<Score> builder)
    {
        builder.Property(s => s.ClientVersion)
            .HasColumnName("client_version")
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(s => s.Statistics)
            .HasColumnType("jsonb")
            .HasConversion(
                value => ConfigurationHelper.SerializeStatistics(value),
                value => ConfigurationHelper.DeserializeStatistics(value))
            .Metadata.SetValueComparer(ConfigurationHelper.StatisticsComparer);

        builder.Property(s => s.Mods)
            .HasColumnType("jsonb")
            .HasConversion(
                value => ConfigurationHelper.SerializeMods(value),
                value => ConfigurationHelper.DeserializeMods(value))
            .Metadata.SetValueComparer(ConfigurationHelper.ModsComparer);

        builder.Property(s => s.MaximumStatistics)
            .HasColumnType("jsonb")
            .HasConversion(
                value => ConfigurationHelper.SerializeStatistics(value),
                value => ConfigurationHelper.DeserializeStatistics(value))
            .Metadata.SetValueComparer(ConfigurationHelper.StatisticsComparer);
    }
}