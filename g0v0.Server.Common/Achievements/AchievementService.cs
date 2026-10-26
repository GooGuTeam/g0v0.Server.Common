// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;

namespace g0v0.Server.Common.Achievements;

/// <summary>Coordinates score evaluation with idempotent, concurrent-safe medal storage.</summary>
/// <param name="repository">The unlock repository.</param>
/// <param name="evaluator">The shared medal evaluator.</param>
/// <param name="clock">The clock used for unlock timestamps.</param>
public sealed class AchievementService(IAchievementRepository repository, IAchievementEvaluator evaluator, TimeProvider clock) : IAchievementService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<Achievement>> UnlockAsync(Score score, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        cancellationToken.ThrowIfCancellationRequested();
        if (!score.Processed || score.Mode is < 0 or > 3)
        {
            return [];
        }

        HashSet<int> owned = (await repository.GetUnlockedIdsAsync(score.UserId, cancellationToken).ConfigureAwait(false)).ToHashSet();
        IReadOnlyList<Achievement> eligible = await evaluator.EvaluateAsync(score, owned, cancellationToken).ConfigureAwait(false);
        if (eligible.Count == 0)
        {
            return [];
        }

        IReadOnlyList<int> unlocked = await repository.UnlockAsync(score.UserId, eligible.Select(medal => medal.Id).ToArray(), clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        HashSet<int> ids = unlocked.ToHashSet();
        return eligible.Where(medal => ids.Contains(medal.Id)).ToArray();
    }
}