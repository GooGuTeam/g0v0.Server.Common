// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="NotificationRepository"/> against an in-memory MySql context.
/// </summary>
[TestFixture]
public class MySqlNotificationRepositoryTests
{
    private MysqlDbContext _context = null!;
    private NotificationRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<MysqlDbContext> options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MysqlDbContext(options);
        _repository = new NotificationRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_ShouldStoreNotificationAndUnreadDeliveryRecords()
    {
        Notification notification = new()
        {
            Name = "channel_message",
            Category = "channel",
            CreatedAt = DateTimeOffset.UtcNow,
            ObjectType = "channel",
            ObjectId = 42,
            SourceUserId = 1,
            Details = "{\"title\":\"hello\"}",
        };

        Notification stored = await _repository.CreateAsync(notification, [2, 3]);

        Assert.That(stored.Id, Is.GreaterThan(0));
        Assert.That(_context.Notifications.Count(), Is.EqualTo(1));
        Assert.That(_context.UserNotifications.Count(), Is.EqualTo(2));
        Assert.That(_context.UserNotifications.Select(record => record.UserId), Is.EquivalentTo([2, 3]));
        Assert.That(_context.UserNotifications.All(record => !record.IsRead), Is.True);
    }

    [Test]
    public async Task GetUserNotificationsAsync_ShouldFilterAndOrder()
    {
        Notification first = await _repository.CreateAsync(
            new Notification { Name = "channel_message", Category = "channel", ObjectType = "channel", ObjectId = 1, SourceUserId = 1 },
            [7]);
        Notification second = await _repository.CreateAsync(
            new Notification { Name = "channel_mention", Category = "channel_mention", ObjectType = "channel", ObjectId = 2, SourceUserId = 1 },
            [7]);

        IReadOnlyList<UserNotification> records = await _repository.GetUserNotificationsAsync(7, unreadOnly: true, maxId: null, limit: 50);
        Assert.That(records.Select(record => record.NotificationId), Is.EqualTo([second.Id, first.Id]));
        Assert.That(records.All(record => record.Notification != null), Is.True);

        IReadOnlyList<UserNotification> paged = await _repository.GetUserNotificationsAsync(7, unreadOnly: true, maxId: second.Id, limit: 50);
        Assert.That(paged.Select(record => record.NotificationId), Is.EqualTo([first.Id]));
    }

    [Test]
    public async Task CountUnreadAndMarkAsRead_ShouldReflectReadState()
    {
        Notification notification = await _repository.CreateAsync(
            new Notification { Name = "channel_message", Category = "channel", ObjectType = "channel", ObjectId = 1, SourceUserId = 1 },
            [7]);

        Assert.That(await _repository.CountUnreadAsync(7), Is.EqualTo(1));

        int marked = await _repository.MarkAsReadAsync(7, [notification.Id]);

        Assert.That(marked, Is.EqualTo(1));
        Assert.That(await _repository.CountUnreadAsync(7), Is.EqualTo(0));

        // Marking again does not count anything.
        Assert.That(await _repository.MarkAsReadAsync(7, [notification.Id]), Is.EqualTo(0));
    }

    [Test]
    public async Task GetNotificationIdsAsync_ShouldApplyOnlyTheSetFilters()
    {
        Notification first = await _repository.CreateAsync(
            new Notification { Name = "channel_message", Category = "channel", ObjectType = "channel", ObjectId = 1, SourceUserId = 1 },
            [2]);
        Notification second = await _repository.CreateAsync(
            new Notification { Name = "channel_team", Category = "channel_team", ObjectType = "channel", ObjectId = 2, SourceUserId = 1 },
            [2]);

        // Only object type: matches everything created so far.
        IReadOnlyList<int> all = await _repository.GetNotificationIdsAsync([], objectType: null, objectId: null, id: null);
        Assert.That(all, Is.EquivalentTo([first.Id, second.Id]));

        // The legacy MySQL enum stores UPPER_SNAKE_CASE values; production
        // matches lower-case input through MySQL's case-insensitive collation,
        // which an in-memory database cannot reproduce.
        IReadOnlyList<int> byName = await _repository.GetNotificationIdsAsync(["CHANNEL_TEAM"], objectType: null, objectId: null, id: null);
        Assert.That(byName, Is.EquivalentTo([second.Id]));

        IReadOnlyList<int> byObject = await _repository.GetNotificationIdsAsync([], "channel", objectId: 1, id: null);
        Assert.That(byObject, Is.EquivalentTo([first.Id]));
    }
}