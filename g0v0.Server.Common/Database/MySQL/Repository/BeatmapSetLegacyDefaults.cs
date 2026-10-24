// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>
/// Fills the optional legacy beatmap set columns that the modern extraction must not leave <c>NULL</c>.
/// </summary>
/// <remarks>
/// The legacy schema declares <c>pack_tags</c>, <c>can_be_hyped</c> and <c>discussion_locked</c> as
/// nullable, and the osu! API no longer exposes them. They are mapped as EF shadow properties because
/// no model member represents them, so they would otherwise be written as <c>NULL</c>; consumers
/// (the web frontend, the legacy API) expect <c>[]</c>, <c>false</c> and <c>false</c> instead.
/// Values already stored are preserved: only <c>NULL</c> is replaced.
/// </remarks>
internal static class BeatmapSetLegacyDefaults
{
    /// <summary>
    /// The empty JSON array written to <c>pack_tags</c>.
    /// </summary>
    internal const string EmptyPackTags = "[]";

    /// <summary>
    /// Applies the defaults to a tracked beatmap set, when the model maps them.
    /// </summary>
    /// <param name="context">The database context that owns the entity.</param>
    /// <param name="beatmapSet">The tracked beatmap set.</param>
    internal static void Apply(DbContext context, BeatmapSet beatmapSet)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(beatmapSet);
        IEntityType? entityType = context.Model.FindEntityType(typeof(BeatmapSet));
        if (entityType is null)
        {
            return;
        }

        SetIfNullOrMissing(context, beatmapSet, entityType, name: "CanBeHyped", fallback: false);
        SetIfNullOrMissing(context, beatmapSet, entityType, name: "DiscussionLocked", fallback: false);
        SetIfNullOrMissing(context, beatmapSet, entityType, name: "PackTags", fallback: EmptyPackTags);
    }

    private static void SetIfNullOrMissing(DbContext context, BeatmapSet beatmapSet, IEntityType entityType, string name, object fallback)
    {
        if (entityType.FindProperty(name) is null)
        {
            // PostgreSQL stores these columns differently (or not at all); nothing to fill.
            return;
        }

        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry = context.Entry(beatmapSet);
        Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry property = entry.Property(name);
        if (property.CurrentValue is null)
        {
            property.CurrentValue = fallback;
        }
    }
}