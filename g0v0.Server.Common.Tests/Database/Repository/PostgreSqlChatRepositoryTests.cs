// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.PostgreSQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using osu.Game.Online.Chat;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture]
public class PostgreSqlChatRepositoryTests
{
    private PostgreSqlDbContext _context = null!;
    private ChatRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PostgreSqlDbContext> options = new DbContextOptionsBuilder<PostgreSqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PostgreSqlDbContext(options);
        _repository = new ChatRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region Membership

    [Test]
    public async Task AddUserChannelAsync_ShouldInsertThenReportNoChange()
    {
        Assert.That(await _repository.AddUserChannelAsync(5, 1), Is.True);
        Assert.That(await _repository.AddUserChannelAsync(5, 1), Is.False);

        ChatUserChannel? membership = await _repository.GetUserChannelAsync(5, 1);
        Assert.That(membership, Is.Not.Null);
        Assert.That(membership!.Hidden, Is.False);
        Assert.That(membership.LastReadId, Is.Null);
    }

    [Test]
    public async Task AddUserChannelAsync_WhenHidden_ShouldUnhideAndReportChange()
    {
        await _repository.AddUserChannelAsync(5, 1);
        await _repository.RemoveUserChannelAsync(5, 1, hide: true);

        Assert.That(await _repository.AddUserChannelAsync(5, 1), Is.True);
        Assert.That((await _repository.GetUserChannelAsync(5, 1))!.Hidden, Is.False);
    }

    [Test]
    public async Task RemoveUserChannelAsync_ShouldHideOrDelete()
    {
        await _repository.AddUserChannelAsync(5, 1);
        await _repository.AddUserChannelAsync(6, 1);

        Assert.That(await _repository.RemoveUserChannelAsync(5, 1, hide: true), Is.True);
        Assert.That(await _repository.RemoveUserChannelAsync(5, 1, hide: true), Is.False);

        Assert.That(await _repository.RemoveUserChannelAsync(6, 1, hide: false), Is.True);
        Assert.That(await _repository.GetUserChannelAsync(6, 1), Is.Null);
        Assert.That((await _repository.GetUserChannelAsync(5, 1))!.Hidden, Is.True);
    }

    [Test]
    public async Task GetUserChannelsAsync_ShouldSkipHiddenRowsUnlessRequested()
    {
        await _repository.AddUserChannelAsync(2, 1);
        await _repository.AddUserChannelAsync(1, 1);
        await _repository.AddUserChannelAsync(3, 1);
        await _repository.RemoveUserChannelAsync(3, 1, hide: true);
        await _repository.AddUserChannelAsync(4, 2);

        IReadOnlyList<ChatUserChannel> visible = await _repository.GetUserChannelsAsync(1);
        IReadOnlyList<ChatUserChannel> all = await _repository.GetUserChannelsAsync(1, includeHidden: true);
        IReadOnlyList<ChatUserChannel> limited = await _repository.GetUserChannelsAsync(1, includeHidden: true, limit: 1);

        int[] expectedVisible = [1, 2];
        int[] expectedAll = [1, 2, 3];
        int[] expectedLimited = [1];
        Assert.That(visible.Select(userChannel => userChannel.ChannelId), Is.EqualTo(expectedVisible));
        Assert.That(all.Select(userChannel => userChannel.ChannelId), Is.EqualTo(expectedAll));
        Assert.That(limited.Select(userChannel => userChannel.ChannelId), Is.EqualTo(expectedLimited));
    }

    [Test]
    public async Task MarkUserChannelAsReadAsync_ShouldNeverMoveBackwards()
    {
        Assert.That(await _repository.MarkUserChannelAsReadAsync(5, 1, 42), Is.False);

        await _repository.AddUserChannelAsync(5, 1);
        Assert.That(await _repository.MarkUserChannelAsReadAsync(5, 1, 42), Is.True);
        Assert.That(await _repository.MarkUserChannelAsReadAsync(5, 1, 41), Is.True);
        Assert.That((await _repository.GetUserChannelAsync(5, 1))!.LastReadId, Is.EqualTo(42));
    }

    [Test]
    public async Task UnhideChannelAsync_ShouldUnhideEveryRowOfTheChannel()
    {
        await _repository.AddUserChannelAsync(5, 1);
        await _repository.AddUserChannelAsync(5, 2);
        await _repository.RemoveUserChannelAsync(5, 1, hide: true);
        await _repository.RemoveUserChannelAsync(5, 2, hide: true);

        Assert.That(await _repository.UnhideChannelAsync(5), Is.EqualTo(2));
        Assert.That(await _repository.UnhideChannelAsync(5), Is.EqualTo(0));
        Assert.That((await _repository.GetUserChannelAsync(5, 1))!.Hidden, Is.False);
    }

    [Test]
    public async Task GetChannelUserIdsAsync_ShouldIncludeHiddenRows()
    {
        await _repository.AddUserChannelAsync(5, 1);
        await _repository.AddUserChannelAsync(5, 2);
        await _repository.RemoveUserChannelAsync(5, 2, hide: true);

        IReadOnlyList<int> userIds = await _repository.GetChannelUserIdsAsync(5);

        int[] expectedUserIds = [1, 2];
        Assert.That(userIds.OrderBy(static userId => userId), Is.EqualTo(expectedUserIds));
    }

    #endregion

    #region Channels and messages

    [Test]
    public async Task UpdateLastMessageIdAsync_ShouldStoreTheMarker()
    {
        ChatChannel channel = await _repository.CreateChannelAsync("#osu", "official", ChannelType.Public);

        await _repository.UpdateLastMessageIdAsync(channel.ChannelId, 7);

        ChatChannel? updated = await _repository.GetChannelAsync(channel.ChannelId);
        Assert.That(updated?.LastMessageId, Is.EqualTo(7));
    }

    [Test]
    public async Task GetRecentMessagesAsync_ShouldReturnTheNewestMessagesPerChannel()
    {
        ChatChannel first = await _repository.CreateChannelAsync("#one", "first", ChannelType.Public);
        ChatChannel second = await _repository.CreateChannelAsync("#two", "second", ChannelType.Public);

        for (int index = 1; index <= 3; index++)
        {
            await _repository.CreateMessageAsync(first.ChannelId, 1, $"first-{index}", MessageType.Plain);
            await _repository.CreateMessageAsync(second.ChannelId, 1, $"second-{index}", MessageType.Plain);
        }

        IReadOnlyList<ChatMessage> messages = await _repository.GetRecentMessagesAsync(
            [first.ChannelId, second.ChannelId],
            limitPerChannel: 2);

        string[] expectedContents = ["first-2", "first-3", "second-2", "second-3"];
        Assert.That(messages.Select(message => message.Content), Is.EqualTo(expectedContents));
    }

    [Test]
    public async Task GetChannelsAsync_ShouldReturnRequestedChannelsInOrder()
    {
        ChatChannel first = await _repository.CreateChannelAsync("#one", "first", ChannelType.Public);
        ChatChannel second = await _repository.CreateChannelAsync("#two", "second", ChannelType.Public);

        IReadOnlyList<ChatChannel> channels = await _repository.GetChannelsAsync([second.ChannelId, first.ChannelId]);

        int[] expectedChannelIds = [first.ChannelId, second.ChannelId];
        Assert.That(channels.Select(channel => channel.ChannelId), Is.EqualTo(expectedChannelIds));
        Assert.That(await _repository.GetChannelsAsync([]), Is.Empty);
    }

    #endregion
}