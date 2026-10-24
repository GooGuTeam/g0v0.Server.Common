// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Scoring;

/// <summary>
/// Calculates a persisted score from its beatmap file and applies it to the owner's statistics.
/// </summary>
public interface IScoreProcessingService
{
    /// <summary>
    /// Processes a queued score, marking it complete only after all calculations succeed.
    /// </summary>
    /// <param name="score">The unprocessed record.</param>
    /// <param name="cancellationToken">Worker cancellation.</param>
    /// <returns>A task representing calculation and persistence.</returns>
    Task ProcessAsync(Score score, CancellationToken cancellationToken);
}