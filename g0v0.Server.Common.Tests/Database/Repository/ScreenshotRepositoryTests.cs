// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlRepository = g0v0.Server.Common.Database.MySQL.Repository.ScreenshotRepository;
using PostgreSqlRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.ScreenshotRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class ScreenshotRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IScreenshotRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlRepository((MysqlDbContext)_context)
            : new PostgreSqlRepository((PostgreSqlDbContext)_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task ReuploadRefreshesTheUrlAndTimeWithoutLosingViewsOrDuplicatingOwnership()
    {
        DateTimeOffset first = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        Screenshot original = await _repository.RecordUploadAsync(Create(1, first, "old-url"));
        original.Hits = 3;
        original.LastAccess = first.AddHours(1);
        await _context.SaveChangesAsync();
        Screenshot repeated = await _repository.RecordUploadAsync(Create(1, first.AddDays(1), "new-url"));
        Screenshot otherUser = await _repository.RecordUploadAsync(Create(2, first.AddDays(1), "other-url"));
        _context.ChangeTracker.Clear();
        Screenshot stored = await _context.Set<Screenshot>().SingleAsync(item => item.Id == original.Id);

        Assert.That(repeated.Id, Is.EqualTo(original.Id));
        Assert.That(otherUser.Id, Is.Not.EqualTo(original.Id));
        Assert.That(await _context.Set<Screenshot>().CountAsync(), Is.EqualTo(2));
        Assert.That(stored.Url, Is.EqualTo("new-url"));
        Assert.That(stored.Timestamp, Is.EqualTo(first.AddDays(1)));
        Assert.That(stored.Hits, Is.EqualTo(3));
        Assert.That(stored.LastAccess, Is.EqualTo(first.AddHours(1)));
    }

    private static Screenshot Create(int userId, DateTimeOffset time, string url)
        => new() { UserId = userId, Sha256Hash = new string('a', 64), Url = url, Timestamp = time, LastAccess = time };
}