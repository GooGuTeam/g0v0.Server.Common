// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.MySQL.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="UserStatisticsRepository"/> against an in-memory Mysql context.
/// </summary>
[TestFixture]
public class MySqlUserStatisticsRepositoryTests
{
    private MysqlDbContext _context = null!;
    private UserStatisticsRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<MysqlDbContext> options = new DbContextOptionsBuilder<MysqlDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MysqlDbContext(options);
        _repository = new UserStatisticsRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_ShouldStoreStatisticsWithCompositeKey()
    {
        UserStatistics statistics = new()
        {
            UserId = 1,
            Mode = 0,
            Pp = 1234.5,
            RankedScore = 50_000,
            HitAccuracy = 98.25,
            PlayCount = 42,
        };

        await _repository.CreateAsync(statistics);

        Assert.That(_context.UserStatistics.Count(), Is.EqualTo(1));
        UserStatistics stored = _context.UserStatistics.Single();
        Assert.That(stored.UserId, Is.EqualTo(1));
        Assert.That(stored.Mode, Is.EqualTo(0));
        Assert.That(stored.Pp, Is.EqualTo(1234.5));
        Assert.That(stored.IsRanked, Is.True);
    }

    [Test]
    public async Task GetByUserIdAndModeAsync_ShouldReturnOnlyTheMatchingRow()
    {
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 0, Pp = 100 });
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 1, Pp = 200 });
        await _repository.CreateAsync(new UserStatistics { UserId = 2, Mode = 0, Pp = 300 });

        UserStatistics? found = await _repository.GetByUserIdAndModeAsync(1, 1);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Pp, Is.EqualTo(200));

        UserStatistics? missing = await _repository.GetByUserIdAndModeAsync(2, 3);
        Assert.That(missing, Is.Null);
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnAllModesOfTheUser()
    {
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 0, Pp = 100 });
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 1, Pp = 200 });
        await _repository.CreateAsync(new UserStatistics { UserId = 2, Mode = 0, Pp = 300 });

        IReadOnlyList<UserStatistics> rows = await _repository.GetByUserIdAsync(1);

        Assert.That(rows.Select(row => row.Mode), Is.EquivalentTo([0, 1]));
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 0, Pp = 100, PlayCount = 5 });

        UserStatistics statistics = (await _repository.GetByUserIdAndModeAsync(1, 0))!;
        statistics.Pp = 900;
        statistics.PlayCount = 42;

        await _repository.UpdateAsync(statistics);

        UserStatistics stored = _context.UserStatistics.Single();
        Assert.That(stored.Pp, Is.EqualTo(900));
        Assert.That(stored.PlayCount, Is.EqualTo(42));
    }

    [Test]
    public async Task GetGlobalRankAsync_ShouldRankByPerformancePoints()
    {
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 0, Pp = 100 });
        await _repository.CreateAsync(new UserStatistics { UserId = 2, Mode = 0, Pp = 300 });
        await _repository.CreateAsync(new UserStatistics { UserId = 3, Mode = 0, Pp = 200 });

        Assert.That(await _repository.GetGlobalRankAsync(2, 0), Is.EqualTo(1));
        Assert.That(await _repository.GetGlobalRankAsync(3, 0), Is.EqualTo(2));
        Assert.That(await _repository.GetGlobalRankAsync(1, 0), Is.EqualTo(3));

        // Other modes and unranked rows do not participate in the ranking.
        await _repository.CreateAsync(new UserStatistics { UserId = 4, Mode = 0, Pp = 999, IsRanked = false });
        await _repository.CreateAsync(new UserStatistics { UserId = 4, Mode = 1, Pp = 999 });

        Assert.That(await _repository.GetGlobalRankAsync(1, 0), Is.EqualTo(3));
        Assert.That(await _repository.GetGlobalRankAsync(4, 0), Is.Null);
    }
}