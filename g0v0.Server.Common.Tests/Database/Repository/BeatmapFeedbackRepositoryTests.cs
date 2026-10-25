// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.MySQL;
using g0v0.Server.Common.Database.PostgreSQL;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using MySqlRepository = g0v0.Server.Common.Database.MySQL.Repository.BeatmapFeedbackRepository;
using PostgreSqlRepository = g0v0.Server.Common.Database.PostgreSQL.Repository.BeatmapFeedbackRepository;

namespace g0v0.Server.Common.Tests.Database.Repository;

[TestFixture(RepositoryBackend.MySql)]
[TestFixture(RepositoryBackend.PostgreSql)]
public class BeatmapFeedbackRepositoryTests(RepositoryBackend backend)
{
    private DbContext _context = null!;
    private IBeatmapFeedbackRepository _repository = null!;

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
    public async Task FavouritesAreIsolatedSortedAndIdempotent()
    {
        DateTimeOffset originalDate = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        Assert.That(await _repository.AddFavouriteAsync(new FavouriteBeatmapSet { UserId = 1, BeatmapSetId = 20, Date = originalDate }), Is.True);
        await _repository.AddFavouriteAsync(new FavouriteBeatmapSet { UserId = 1, BeatmapSetId = 10, Date = originalDate });
        await _repository.AddFavouriteAsync(new FavouriteBeatmapSet { UserId = 2, BeatmapSetId = 30, Date = originalDate });
        bool duplicate = await _repository.AddFavouriteAsync(new FavouriteBeatmapSet { UserId = 1, BeatmapSetId = 20, Date = originalDate.AddDays(1) });

        Assert.That(duplicate, Is.False);
        Assert.That(await _repository.GetFavouritesAsync(1), Is.EqualTo(new List<int> { 10, 20 }));
        Assert.That(await _repository.GetFavouritesAsync(3), Is.Empty);
        Assert.That((await _context.Set<FavouriteBeatmapSet>().SingleAsync(item => item.UserId == 1 && item.BeatmapSetId == 20)).Date, Is.EqualTo(originalDate));
    }

    [Test]
    public async Task ARepeatedVoteCannotReplaceTheOriginalOrChangeTheSetAverage()
    {
        Assert.That(await _repository.AddRatingAsync(new BeatmapRating { UserId = 1, BeatmapSetId = 10, Rating = 7 }), Is.True);
        await _repository.AddRatingAsync(new BeatmapRating { UserId = 2, BeatmapSetId = 10, Rating = 8 });
        await _repository.AddRatingAsync(new BeatmapRating { UserId = 1, BeatmapSetId = 20, Rating = 1 });
        bool duplicate = await _repository.AddRatingAsync(new BeatmapRating { UserId = 1, BeatmapSetId = 10, Rating = 1 });

        Assert.That(duplicate, Is.False);
        Assert.That(await _repository.GetAverageRatingAsync(10), Is.EqualTo(7.5));
        Assert.That(await _repository.GetAverageRatingAsync(30), Is.Zero);
        Assert.That(await _repository.HasRatingAsync(1, 10), Is.True);
        Assert.That(await _repository.HasRatingAsync(3, 10), Is.False);
    }

    [Test]
    public void RejectsRatingsOutsideTheAllowedRange()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _repository.AddRatingAsync(new BeatmapRating { UserId = 1, BeatmapSetId = 10, Rating = 11 }));
        Assert.That(_context.Set<BeatmapRating>().Count(), Is.Zero);
    }
}