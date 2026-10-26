// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using g0v0.Server.Common.Achievements;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Achievements;

public class AchievementServiceTests
{
    [Test]
    public async Task ConcurrentSubmissionsOnlyReturnMedalsActuallyInsertedByTheirOwnCall()
    {
        RecordingRepository repository = new();
        RecordingEvaluator evaluator = new();
        AchievementService service = new(repository, evaluator, TimeProvider.System);
        Task<IReadOnlyList<Achievement>> first = service.UnlockAsync(new Score { Id = 1, UserId = 10, Processed = true });
        Task<IReadOnlyList<Achievement>> second = service.UnlockAsync(new Score { Id = 2, UserId = 10, Processed = true });
        evaluator.Ready.SetResult();
        IReadOnlyList<Achievement>[] results = await Task.WhenAll(first, second);
        Assert.That(results.SelectMany(items => items).Select(item => item.Id), Is.EqualTo((int[])[1]));
        Assert.That(repository.Awards, Has.Count.EqualTo(1));
        Assert.That(await service.UnlockAsync(new Score { Id = 1, UserId = 10, Processed = true }), Is.Empty);
    }

    [TestCase(false, 0)]
    [TestCase(true, 4)]
    [TestCase(true, 5)]
    public async Task PendingAndRelaxAutopilotScoresAreNotEvaluated(bool processed, int mode)
    {
        RecordingRepository repository = new();
        RecordingEvaluator evaluator = new();
        AchievementService service = new(repository, evaluator, TimeProvider.System);
        Assert.That(await service.UnlockAsync(new Score { Processed = processed, Mode = mode }), Is.Empty);
        Assert.That(evaluator.Calls, Is.Zero);
        Assert.That(repository.Awards, Is.Empty);
    }

    private sealed class RecordingEvaluator : IAchievementEvaluator
    {
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Calls { get; private set; }

        public async Task<IReadOnlyList<Achievement>> EvaluateAsync(Score score, IReadOnlySet<int> owned, CancellationToken cancellationToken = default)
        {
            Calls++;
            await Ready.Task.WaitAsync(cancellationToken);
            return owned.Contains(1) ? [] : [new Achievement(1, "Rising Star", "First step", "osu-skill-pass-1")];
        }
    }

    private sealed class RecordingRepository : IAchievementRepository
    {
        public ConcurrentDictionary<(int UserId, int AchievementId), DateTimeOffset> Awards { get; } = new();

        public Task<IReadOnlyList<int>> GetUnlockedIdsAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(Awards.Keys.Where(key => key.UserId == userId).Select(key => key.AchievementId).ToArray());

        public Task<IReadOnlyList<int>> UnlockAsync(int userId, IReadOnlyList<int> achievementIds, DateTimeOffset achievedAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<int>>(achievementIds.Where(id => Awards.TryAdd((userId, id), achievedAt)).ToArray());
    }
}