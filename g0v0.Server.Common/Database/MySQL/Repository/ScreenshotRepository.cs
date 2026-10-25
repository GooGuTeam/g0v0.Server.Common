// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace g0v0.Server.Common.Database.MySQL.Repository;

/// <summary>Persists screenshot metadata using MySQL.</summary>
/// <param name="context">The database context.</param>
public class ScreenshotRepository(MysqlDbContext context) : IScreenshotRepository, IMySqlRepository
{
    /// <inheritdoc/>
    public async Task<Screenshot> RecordUploadAsync(Screenshot screenshot, CancellationToken cancellationToken = default)
    {
        Screenshot? existing = await context.Screenshots
            .Where(item => item.UserId == screenshot.UserId && item.Sha256Hash == screenshot.Sha256Hash)
            .OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            context.Screenshots.Add(screenshot);
        }
        else
        {
            existing.Url = screenshot.Url;
            existing.Timestamp = screenshot.Timestamp;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing ?? screenshot;
    }
}