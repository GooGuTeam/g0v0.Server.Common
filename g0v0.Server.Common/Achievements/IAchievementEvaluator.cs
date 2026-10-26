// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Achievements;

/// <summary>Evaluates medal conditions using authoritative score data.</summary>
public interface IAchievementEvaluator
{
    /// <summary>Finds the eligible medals, excluding IDs the user already owns.</summary>
    /// <param name="score">The processed score.</param>
    /// <param name="owned">The already unlocked IDs.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The matching medal definitions.</returns>
    Task<IReadOnlyList<Achievement>> EvaluateAsync(Score score, IReadOnlySet<int> owned, CancellationToken cancellationToken = default);
}