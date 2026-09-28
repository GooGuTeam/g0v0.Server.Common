// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Caching;

/// <summary>
/// Provides a cache of members ranked by a numeric score, shared across the g0v0 server.
/// </summary>
/// <remarks>
/// Backed by a Redis sorted set. The chat server uses it for two rolling
/// windows: the per-channel activity markers
/// (<c>chat:channel:{channelId}</c>, score is the last ack timestamp) and the
/// per-user message throttle (<c>message_throttle:{userId}:{kind}</c>, score is
/// the send timestamp).
/// </remarks>
public interface IRankedCache
{
    /// <summary>
    /// Adds or updates a member with the given score and refreshes the key's time-to-live.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="member">The member to add.</param>
    /// <param name="score">The score to rank the member by.</param>
    /// <param name="expiry">The time-to-live of the key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetScoreAsync(string key, string member, double score, TimeSpan expiry);

    /// <summary>
    /// Removes every member scored below <paramref name="windowStart"/>, counts the
    /// remaining members and then adds <paramref name="member"/> to the set.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="member">The member to add.</param>
    /// <param name="score">The score to rank the new member by.</param>
    /// <param name="windowStart">The inclusive lower bound of the rolling window.</param>
    /// <param name="expiry">The time-to-live of the key.</param>
    /// <returns>The number of members inside the window, excluding the newly added member.</returns>
    Task<int> CountWindowAndAddAsync(string key, string member, double score, double windowStart, TimeSpan expiry);

    /// <summary>
    /// Gets every member whose score falls inside the given range, ordered by score.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="minScore">The inclusive lower bound.</param>
    /// <param name="maxScore">The inclusive upper bound.</param>
    /// <returns>The matching members.</returns>
    Task<IReadOnlyList<string>> GetByScoreAsync(string key, double minScore, double maxScore = double.PositiveInfinity);
}