// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using StackExchange.Redis;

namespace g0v0.Server.Common.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="IRankedCache"/> using sorted sets.
/// </summary>
/// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
public class RedisRankedCache(IConnectionMultiplexer connectionMultiplexer) : IRankedCache
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    /// <inheritdoc/>
    public async Task SetScoreAsync(string key, string member, double score, TimeSpan expiry)
    {
        ITransaction transaction = _database.CreateTransaction();
        _ = transaction.SortedSetAddAsync(key, member, score);
        _ = transaction.KeyExpireAsync(key, expiry);
        await transaction.ExecuteAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountWindowAndAddAsync(string key, string member, double score, double windowStart, TimeSpan expiry)
    {
        // The trim, the count and the insert run in one transaction so that
        // concurrent sends cannot slip past the rolling window.
        ITransaction transaction = _database.CreateTransaction();
        _ = transaction.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStart);
        Task<long> count = transaction.SortedSetLengthAsync(key, windowStart);
        _ = transaction.SortedSetAddAsync(key, member, score);
        _ = transaction.KeyExpireAsync(key, expiry);
        await transaction.ExecuteAsync().ConfigureAwait(false);

        return (int)await count.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetByScoreAsync(string key, double minScore, double maxScore = double.PositiveInfinity)
    {
        RedisValue[] values = await _database.SortedSetRangeByScoreAsync(key, minScore, maxScore).ConfigureAwait(false);
        return values.Select(static value => value.ToString()).ToList();
    }
}