// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Configurations;
using g0v0.Server.Common.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace g0v0.Server.Common.Database.MySQL.Configurations;

/// <summary>
/// Applies legacy MySQL table naming for user records.
/// </summary>
/// <remarks>
/// <para>
/// The legacy lazer API table stores the privilege bit field as <c>priv</c> and both ruleset
/// preferences as native <c>gamemode</c> enum columns, so the integer properties are converted to
/// their <c>UPPER_SNAKE_CASE</c> labels.
/// </para>
/// <para>
/// The table also declares a group of <c>NOT NULL</c> columns without a database default that this
/// server does not expose (the privilege flags and the profile counters). They are mapped as
/// shadow properties and filled in by <see cref="SupplyLegacyColumns"/>, because MySQL rejects an
/// insert that omits them and the lazer API expects to read values rather than nulls.
/// </para>
/// </remarks>
public class UserConfig : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// The shadow columns the legacy table requires but the shared model does not expose, with the
    /// value a newly registered account is stored with.
    /// </summary>
    private static readonly (string Name, object Value)[] LegacyColumns =
    [
        ("Priv", 1),
        ("HasSupported", false),
        ("MaxBlocks", 50),
        ("MaxFriends", 500),
        ("CommentsCount", 0),
        ("PostCount", 0),
        ("IsAdmin", false),
        ("IsGmt", false),
        ("IsQat", false),
        ("IsBng", false),
    ];

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("lazer_users");

        builder.Property(user => user.LastVisit)
            .HasColumnName("last_visit")
            .HasColumnType("datetime")
            .HasConversion(
                value => ConvertNullableDateTimeOffsetToDateTime(value),
                value => ConvertNullableDateTimeToDateTimeOffset(value));

        builder.Property(user => user.JoinDate)
            .HasColumnName("join_date")
            .HasColumnType("datetime")
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        builder.Property(user => user.Playmode)
            .HasColumnName("playmode")
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value));

        builder.Property(user => user.G0V0Playmode)
            .HasColumnType(
                "enum('OSU','TAIKO','FRUITS','MANIA','OSURX','OSUAP','TAIKORX','FRUITSRX','SENTAKKI','TAU','RUSH','HISHIGATA','SOYOKAZE')")
            .HasConversion(
                value => ConfigurationHelper.ConvertModeToDatabaseValue(value),
                value => ConfigurationHelper.ConvertDatabaseValueToMode(value));

        builder.Property<int>("Priv");
        builder.Property<bool>("HasSupported");
        builder.Property<int>("MaxBlocks");
        builder.Property<int>("MaxFriends");
        builder.Property<int>("CommentsCount");
        builder.Property<int>("PostCount");
        builder.Property<bool>("IsAdmin");
        builder.Property<bool>("IsGmt");
        builder.Property<bool>("IsQat");
        builder.Property<bool>("IsBng");
    }

    /// <summary>
    /// Fills the legacy-only shadow columns of a user that is about to be inserted, and keeps the
    /// stored values when an existing user is updated.
    /// </summary>
    /// <param name="entry">The tracked user entry.</param>
    /// <remarks>
    /// Attaching a detached user with <c>DbSet.Update</c> marks every mapped property as modified,
    /// including the shadow columns, whose CLR defaults would otherwise reset the stored privilege
    /// flags and limits.
    /// </remarks>
    internal static void SupplyLegacyColumns(EntityEntry<User> entry)
    {
        foreach ((string name, object value) in LegacyColumns)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(name).CurrentValue = value;
            }
            else
            {
                entry.Property(name).IsModified = false;
            }
        }
    }

    private static DateTime? ConvertNullableDateTimeOffsetToDateTime(DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.UtcDateTime : null;
    }

    private static DateTimeOffset? ConvertNullableDateTimeToDateTimeOffset(DateTime? value)
    {
        return value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
    }
}