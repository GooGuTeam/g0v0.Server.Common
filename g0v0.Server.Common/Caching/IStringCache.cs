// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Caching;

/// <summary>
/// Provides string-keyed cache operations shared across the g0v0 server.
/// </summary>
public interface IStringCache
{
    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or <see langword="null"/> when absent.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets a cached value with a time-to-live.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiry">The time-to-live.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetAsync(string key, string value, TimeSpan expiry);

    /// <summary>
    /// Deletes a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(string key);

    /// <summary>
    /// Extends the time-to-live of an existing cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="expiry">The new time-to-live.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RefreshAsync(string key, TimeSpan expiry);
}