// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using BeatmapModel = g0v0.Server.Common.Database.Models.Beatmap;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Configures the parts of <see cref="BeatmapModel"/> that EF Core cannot infer for PostgreSQL.
/// </summary>
public class BeatmapConfig : IEntityTypeConfiguration<BeatmapModel>
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BeatmapModel> builder)
    {
        builder.Property(b => b.Covers)
            .HasColumnName("covers")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonConvert.SerializeObject(value, JsonSettings),
                value => JsonConvert.DeserializeObject<BeatmapCovers>(value, JsonSettings) ?? new BeatmapCovers())
            .Metadata.SetValueComparer(ComparerFor<BeatmapCovers>.Create());
    }

    private static class ComparerFor<T>
    {
        public static ValueComparer<T> Create()
        {
            return new ValueComparer<T>(
                (left, right) => JsonConvert.SerializeObject(left, JsonSettings) == JsonConvert.SerializeObject(right, JsonSettings),
                value => JsonConvert.SerializeObject(value, JsonSettings).GetHashCode(StringComparison.Ordinal),
                value => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, JsonSettings), JsonSettings)!);
        }
    }
}