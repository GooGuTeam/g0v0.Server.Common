// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Defines common file operations for storage backends.
/// </summary>
public interface IStorageService : IAsyncDisposable
{
    /// <summary>
    /// Writes bytes to the given file path or object key.
    /// </summary>
    /// <param name="filePath">The relative file path or object key.</param>
    /// <param name="content">The content bytes to write.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="cacheControl">The Cache-Control header value for object storage backends.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteFileAsync(
        string filePath,
        byte[] content,
        string contentType = "application/octet-stream",
        string cacheControl = "public, max-age=31536000",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all bytes from the given file path or object key.
    /// </summary>
    /// <param name="filePath">The relative file path or object key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The file content bytes.</returns>
    Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file at the given file path or object key if it exists.
    /// </summary>
    /// <param name="filePath">The relative file path or object key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a file exists.
    /// </summary>
    /// <param name="filePath">The relative file path or object key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a URL for accessing the file.
    /// </summary>
    /// <param name="filePath">The relative file path or object key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The URL for accessing the file.</returns>
    Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a file path or object key from a URL.
    /// </summary>
    /// <param name="url">The URL to parse.</param>
    /// <returns>The file path or object key when it can be extracted; otherwise, <see langword="null"/>.</returns>
    string? GetFileNameByUrl(string url);

    /// <summary>
    /// Closes the storage service and releases resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CloseAsync();
}
