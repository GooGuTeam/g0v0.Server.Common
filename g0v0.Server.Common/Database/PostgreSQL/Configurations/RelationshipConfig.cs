// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.PostgreSQL.Configurations;

/// <summary>
/// Maps <see cref="Relationship"/> to the v2 PostgreSQL <c>relationship</c> table.
///
/// <para>
/// Relationship rows are keyed by the composite (<c>user_id</c>, <c>target_id</c>, <c>type</c>)
/// primary key on both backends; there is no surrogate <c>id</c> column.
/// </para>
/// </summary>
public class RelationshipConfig : IEntityTypeConfiguration<Relationship>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationship");

        builder.HasKey(relationship => new { relationship.UserId, relationship.TargetId, relationship.Type });
    }
}