// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Configures the <see cref="Relationship"/> entity mapping for the legacy MySQL schema.
/// </summary>
/// <remarks>
/// The legacy <c>relationship</c> table has no surrogate <c>id</c> column; rows are keyed by
/// the composite (<c>user_id</c>, <c>target_id</c>, <c>type</c>).
/// </remarks>
public class RelationshipConfig : IEntityTypeConfiguration<Relationship>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationship");

        builder.Ignore(r => r.Id);

        builder.HasKey(r => new { r.UserId, r.TargetId, r.Type });

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(r => r.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasColumnType("enum('FOLLOW','BLOCK')")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<RelationshipType>(value, true))
            .IsRequired();

        builder.HasIndex(r => r.TargetId)
            .HasDatabaseName("ix_relationship_target_id");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("ix_relationship_user_id");

        builder.HasOne(r => r.Target)
            .WithMany()
            .HasForeignKey(r => r.TargetId)
            .HasConstraintName("relationship_ibfk_1")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("relationship_ibfk_2")
            .OnDelete(DeleteBehavior.Restrict);
    }
}