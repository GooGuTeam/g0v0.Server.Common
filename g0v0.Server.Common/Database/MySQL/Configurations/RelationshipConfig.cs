// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

public class RelationshipConfig : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationship");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

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