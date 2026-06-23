// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class MySqlUserModelConfigurationTests
{
    [Test]
    public void MysqlDbContext_ShouldApplyUserConfigurationAutomatically()
    {
        var options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseMySql(
                "Server=localhost;Database=g0v0_test;User=root;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        using var relationalContext = new MysqlDbContext(options);

        var entityType = relationalContext.Model.FindEntityType(typeof(User));

        Assert.That(entityType, Is.Not.Null);
        Assert.That(entityType!.GetTableName(), Is.EqualTo("lazer_users"));

        Assert.Multiple(() =>
        {
            Assert.That(entityType.FindProperty(nameof(User.Id))!.GetColumnName(), Is.EqualTo("id"));
            Assert.That(entityType.FindProperty(nameof(User.AvatarUrl))!.GetColumnName(), Is.EqualTo("avatar_url"));
            Assert.That(entityType.FindProperty(nameof(User.CountryCode))!.GetColumnName(), Is.EqualTo("country_code"));
            Assert.That(entityType.FindProperty(nameof(User.IsActive))!.GetColumnName(), Is.EqualTo("is_active"));
            Assert.That(entityType.FindProperty(nameof(User.IsBot))!.GetColumnName(), Is.EqualTo("is_bot"));
            Assert.That(entityType.FindProperty(nameof(User.IsSupporter))!.GetColumnName(), Is.EqualTo("is_supporter"));
            Assert.That(entityType.FindProperty(nameof(User.IsOnline))!.GetColumnName(), Is.EqualTo("is_online"));
            Assert.That(entityType.FindProperty(nameof(User.LastVisit))!.GetColumnName(), Is.EqualTo("last_visit"));
            Assert.That(entityType.FindProperty(nameof(User.LastVisit))!.GetColumnType(), Is.EqualTo("datetime"));
            Assert.That(entityType.FindProperty(nameof(User.LastVisit))!.GetValueConverter(), Is.Not.Null);
            Assert.That(entityType.FindProperty(nameof(User.PmFriendsOnly))!.GetColumnName(), Is.EqualTo("pm_friends_only"));
            Assert.That(entityType.FindProperty(nameof(User.ProfileColour))!.GetColumnName(), Is.EqualTo("profile_colour"));
            Assert.That(entityType.FindProperty(nameof(User.Username))!.GetColumnName(), Is.EqualTo("username"));
        });

        var countryCodeIndex = entityType.GetIndexes().Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(User.CountryCode));
        Assert.That(countryCodeIndex.GetDatabaseName(), Is.EqualTo("ix_lazer_users_country_code"));

        var usernameIndex = entityType.GetIndexes().Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(User.Username));
        Assert.That(usernameIndex.IsUnique, Is.True);
        Assert.That(usernameIndex.GetDatabaseName(), Is.EqualTo("ix_lazer_users_username"));
    }
}