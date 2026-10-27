// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Caching;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using g0v0.Server.Common.Storage;
using osu.Game.IO.Legacy;

namespace g0v0.Server.Common.Scoring;

/// <summary>Reads the shared .osr storage and extracts compressed frames for stable clients.</summary>
/// <param name="scores">The score repository.</param>
/// <param name="storage">The replay storage.</param>
/// <param name="statistics">The replay owner's statistics.</param>
/// <param name="replayViewLocks">The distributed cache used to deduplicate replay views.</param>
public sealed class ReplayService(
    IScoreRepository scores,
    IStorageService storage,
    IUserStatisticsRepository statistics,
    IStringCache replayViewLocks)
{
    private static readonly TimeSpan ReplayDownloadCountInterval = TimeSpan.FromDays(1);

    /// <summary>Reads the compressed replay frames without the .osr header or lazer trailer.</summary>
    /// <param name="scoreId">The score ID.</param>
    /// <param name="mode">The requested legacy ruleset ID.</param>
    /// <param name="viewerId">The authenticated viewer's user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compressed frames, or null when the score or valid replay is unavailable.</returns>
    public async Task<byte[]?> GetRawReplayAsync(long scoreId, int mode, int viewerId, CancellationToken cancellationToken = default)
    {
        Score? score = await scores.GetByIdAsync(scoreId).ConfigureAwait(false);
        if (score is null || !score.HasReplay || !await storage.IsExistsAsync(score.ReplayFilename, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        byte[] replay;
        try
        {
            replay = await storage.ReadFileAsync(score.ReplayFilename, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        byte[]? frames = ExtractFrames(replay, mode);
        if (frames is null || score.UserId == viewerId)
        {
            return frames;
        }

        string lockKey = $"view:score_replay:{score.Id}:{viewerId}";
        if (await replayViewLocks.TrySetAsync(lockKey, "1", ReplayDownloadCountInterval).ConfigureAwait(false))
        {
            await statistics.IncrementReplayViewsAsync(score.UserId, score.Mode, cancellationToken).ConfigureAwait(false);
        }

        return frames;
    }

    private static byte[]? ExtractFrames(byte[] replay, int mode)
    {
        using MemoryStream stream = new(replay, writable: false);
        using SerializationReader reader = new(stream);
        try
        {
            if (reader.ReadByte() != mode)
            {
                return null;
            }

            reader.ReadInt32(); // Client version.
            reader.ReadString(); // Beatmap checksum.
            reader.ReadString(); // Username.
            reader.ReadString(); // Replay checksum.
            stream.Seek(23, SeekOrigin.Current); // Hit counts, score, combo, perfect flag and mods.
            reader.ReadString(); // Life bar graph.
            reader.ReadInt64(); // Play time.
            int length = reader.ReadInt32();
            return length > 0 && length <= stream.Length - stream.Position ? reader.ReadBytes(length) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}