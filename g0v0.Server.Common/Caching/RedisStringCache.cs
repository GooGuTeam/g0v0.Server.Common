// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using StackExchange.Redis;

namespace g0v0.Server.Common.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="IStringCache"/>.
/// </summary>
/// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
public class RedisStringCache(IConnectionMultiplexer connectionMultiplexer) : IStringCache
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key)
    {
        RedisValue value = await _database.StringGetAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc/>
    public Task SetAsync(string key, string value, TimeSpan expiry)
    {
        return _database.StringSetAsync(key, value, expiry);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string key)
    {
        return _database.KeyDeleteAsync(key);
    }

    /// <inheritdoc/>
    public Task RefreshAsync(string key, TimeSpan expiry)
    {
        return _database.KeyExpireAsync(key, expiry);
    }
}