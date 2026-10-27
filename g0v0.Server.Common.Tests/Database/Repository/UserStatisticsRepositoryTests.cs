// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlDbContext = g0v0.Server.Common.Database.MySQL.MysqlDbContext;
using MySqlUserStatisticsRepository = g0v0.Server.Common.Database.MySQL.Repository.UserStatisticsRepository;
using PostgreSqlDbContext = g0v0.Server.Common.Database.PostgreSQL.PostgreSqlDbContext;
using PostgreSqlUserStatisticsRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.UserStatisticsRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

/// <summary>
/// Runs the <see cref="IUserStatisticsRepository"/> contract against both database backends.
/// </summary>
[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class UserStatisticsRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IUserStatisticsRepository _repository = null!;

    private DbSet<UserStatistics> UserStatistics => _context.Set<UserStatistics>();

    [SetUp]
    public void SetUp()
    {
        _context = RepositoryTestContexts.Create(backend);
        _repository = backend == RepositoryBackend.MySql
            ? new MySqlUserStatisticsRepository((MySqlDbContext)_context)
            : new PostgreSqlUserStatisticsRepository((PostgreSqlDbContext)_context);
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

        Assert.That(UserStatistics.Count(), Is.EqualTo(1));
        UserStatistics stored = UserStatistics.Single();
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

        UserStatistics stored = UserStatistics.Single();
        Assert.That(stored.Pp, Is.EqualTo(900));
        Assert.That(stored.PlayCount, Is.EqualTo(42));
    }

    [Test]
    public async Task IncrementReplayViewsAsync_OnlyChangesTheOwnerAndStoredMode()
    {
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 4, ReplaysWatchedByOthers = 7, PlayCount = 12 });
        await _repository.CreateAsync(new UserStatistics { UserId = 1, Mode = 0, ReplaysWatchedByOthers = 3 });
        await _repository.CreateAsync(new UserStatistics { UserId = 2, Mode = 4, ReplaysWatchedByOthers = 5 });

        await _repository.IncrementReplayViewsAsync(1, 4);
        await _repository.IncrementReplayViewsAsync(1, 4);
        await _repository.IncrementReplayViewsAsync(99, 4);

        Assert.That((await _repository.GetByUserIdAndModeAsync(1, 4))!.ReplaysWatchedByOthers, Is.EqualTo(9));
        Assert.That((await _repository.GetByUserIdAndModeAsync(1, 4))!.PlayCount, Is.EqualTo(12));
        Assert.That((await _repository.GetByUserIdAndModeAsync(1, 0))!.ReplaysWatchedByOthers, Is.EqualTo(3));
        Assert.That((await _repository.GetByUserIdAndModeAsync(2, 4))!.ReplaysWatchedByOthers, Is.EqualTo(5));
        Assert.That(UserStatistics.Count(), Is.EqualTo(3));
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