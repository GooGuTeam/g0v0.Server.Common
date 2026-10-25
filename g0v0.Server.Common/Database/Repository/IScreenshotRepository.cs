// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;

namespace g0v0.Server.Common.Database.Repository;

/// <summary>Persists screenshot uploads.</summary>
public interface IScreenshotRepository
{
    /// <summary>Creates or refreshes an upload, preserving existing access statistics.</summary>
    /// <param name="screenshot">The uploaded screenshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted screenshot.</returns>
    Task<Screenshot> RecordUploadAsync(Screenshot screenshot, CancellationToken cancellationToken = default);
}