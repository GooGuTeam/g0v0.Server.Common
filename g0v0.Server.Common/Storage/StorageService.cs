// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Base class for storage service implementations.
/// </summary>
public abstract class StorageService : IStorageService
{
    /// <inheritdoc />
    public abstract Task WriteFileAsync(
        string filePath,
        byte[] content,
        string contentType = "application/octet-stream",
        string cacheControl = "public, max-age=31536000",
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract string? GetFileNameByUrl(string url);

    /// <inheritdoc />
    public virtual Task CloseAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
