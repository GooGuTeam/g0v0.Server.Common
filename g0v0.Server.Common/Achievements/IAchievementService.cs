// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Achievements;

/// <summary>Awards the medals earned by a processed score.</summary>
public interface IAchievementService
{
    /// <summary>Evaluates a score and persists only previously locked achievements.</summary>
    /// <param name="score">The successfully processed score.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The medals newly unlocked by this call.</returns>
    Task<IReadOnlyList<Achievement>> UnlockAsync(Score score, CancellationToken cancellationToken = default);
}