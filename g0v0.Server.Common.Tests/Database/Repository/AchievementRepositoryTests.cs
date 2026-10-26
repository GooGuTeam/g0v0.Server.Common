// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlRepository = g0v0.Server.Common.Database.MySQL.Repository.AchievementRepository;
using PostgreSqlRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.AchievementRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class AchievementRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IAchievementRepository _repository = null!;

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
    public async Task UnlocksAreIsolatedIdempotentAndPreserveTheirFirstTimestamp()
    {
        DateTimeOffset first = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);
        Assert.That(await _repository.UnlockAsync(1, [2, 1, 2], first), Is.EquivalentTo((int[])[1, 2]));
        Assert.That(await _repository.UnlockAsync(1, [1, 2, 3], first.AddDays(1)), Is.EqualTo((int[])[3]));
        Assert.That(await _repository.UnlockAsync(2, [1], first.AddDays(1)), Is.EqualTo((int[])[1]));
        Assert.That(await _repository.UnlockAsync(1, [1], first.AddDays(2)), Is.Empty);
        Assert.That(await _repository.GetUnlockedIdsAsync(1), Is.EqualTo((int[])[1, 2, 3]));
        Assert.That(await _repository.GetUnlockedIdsAsync(2), Is.EqualTo((int[])[1]));
        Assert.That(await _repository.GetUnlockedIdsAsync(3), Is.Empty);
        Assert.That((await _context.Set<UserAchievement>().SingleAsync(item => item.UserId == 1 && item.AchievementId == 1)).AchievedAt, Is.EqualTo(first));
    }
}