// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Stores files on the local filesystem.
/// </summary>
public class LocalStorageService : StorageService
{
    private const string LocalFileUrlPrefix = "file/";

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
    /// </summary>
    /// <param name="settings">The local storage settings.</param>
    /// <param name="pathProvider">The path provider used to resolve relative storage paths.</param>
    public LocalStorageService(LocalStorageSettings settings, IPathProvider pathProvider)
        : this(ResolveStoragePath(settings, pathProvider))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
    /// </summary>
    /// <param name="storagePath">The storage directory path.</param>
    public LocalStorageService(string storagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        StoragePath = Path.GetFullPath(storagePath);
        Directory.CreateDirectory(StoragePath);
    }

    /// <summary>
    /// Gets the resolved storage root path.
    /// </summary>
    public string StoragePath { get; }

    /// <inheritdoc />
    public override async Task WriteFileAsync(
        string filePath,
        byte[] content,
        string contentType = "application/octet-stream",
        string cacheControl = "public, max-age=31536000",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string fullPath = GetFullPath(filePath);
        string? parent = Path.GetDirectoryName(fullPath);
        if (parent != null)
        {
            Directory.CreateDirectory(parent);
        }

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            return Task.CompletedTask;
        }

        File.Delete(fullPath);
        DeleteEmptyParentDirectories(fullPath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = GetFullPath(filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public override Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(LocalFileUrlPrefix + NormalizeRelativeFilePath(filePath).Replace('\\', '/'));
    }

    /// <inheritdoc />
    public override string? GetFileNameByUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string path = GetUrlPath(url);
        if (!path.StartsWith(LocalFileUrlPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        string fileName = path[LocalFileUrlPrefix.Length..];
        return fileName.Length == 0 ? null : fileName;
    }

    private static string ResolveStoragePath(LocalStorageSettings settings, IPathProvider pathProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pathProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.LocalStoragePath);

        string configuredPath = settings.LocalStoragePath;
        if (Path.IsPathFullyQualified(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        string basePath = Path.GetFullPath(pathProvider.GetBasePath());
        return Path.GetFullPath(Path.Combine(basePath, configuredPath));
    }

    private static string NormalizeRelativeFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string cleanPath = filePath.TrimStart('/', '\\');
        if (string.IsNullOrWhiteSpace(cleanPath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        return cleanPath;
    }

    private static string GetUrlPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        int pathEnd = GetPathEndIndex(url);
        return url[..pathEnd].TrimStart('/');
    }

    private static int GetPathEndIndex(string value)
    {
        int queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        int fragmentIndex = value.IndexOf('#', StringComparison.Ordinal);

        if (queryIndex < 0)
        {
            return fragmentIndex < 0 ? value.Length : fragmentIndex;
        }

        return fragmentIndex < 0 ? queryIndex : Math.Min(queryIndex, fragmentIndex);
    }

    private string GetFullPath(string filePath)
    {
        string cleanPath = NormalizeRelativeFilePath(filePath);
        string fullPath = Path.GetFullPath(Path.Combine(StoragePath, cleanPath));

        if (!IsInsideStoragePath(fullPath))
        {
            throw new ArgumentException($"Invalid file path: {filePath}", nameof(filePath));
        }

        return fullPath;
    }

    private bool IsInsideStoragePath(string fullPath)
    {
        string relativePath = Path.GetRelativePath(StoragePath, fullPath);
        return relativePath.Length == 0 ||
               (!relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath));
    }

    private void DeleteEmptyParentDirectories(string fullPath)
    {
        DirectoryInfo? parent = Directory.GetParent(fullPath);
        while (parent != null &&
               !string.Equals(Path.GetFullPath(parent.FullName), StoragePath, StringComparison.Ordinal) &&
               !Directory.EnumerateFileSystemEntries(parent.FullName).Any())
        {
            DirectoryInfo current = parent;
            parent = current.Parent;
            current.Delete();
        }
    }
}