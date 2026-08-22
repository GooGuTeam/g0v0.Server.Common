// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Stores files in AWS S3 or an S3-compatible object storage service.
/// </summary>
public class S3StorageService : StorageService
{
    private readonly AmazonS3Client _client;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether to disable SigV4 payload signing.
    /// </summary>
    protected virtual bool DisablePayloadSigning => false;

    /// <summary>
    /// Gets a value indicating whether to disable checksum validation for S3 objects.
    /// </summary>
    protected virtual bool DisableDefaultChecksumValidation => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="settings">The AWS S3 storage settings.</param>
    public S3StorageService(S3StorageSettings settings)
        : this(
            (settings ?? throw new ArgumentNullException(nameof(settings))).S3AccessKeyId,
            settings.S3SecretAccessKey,
            settings.S3BucketName,
            settings.S3RegionName,
            settings.S3PublicUrlBase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="accessKeyId">The AWS access key ID.</param>
    /// <param name="secretAccessKey">The AWS secret access key.</param>
    /// <param name="bucketName">The S3 bucket name.</param>
    /// <param name="regionName">The AWS region name.</param>
    /// <param name="publicUrlBase">An optional public URL base for files in the bucket.</param>
    public S3StorageService(
        string accessKeyId,
        string secretAccessKey,
        string bucketName,
        string regionName,
        string? publicUrlBase = null)
        : this(accessKeyId, secretAccessKey, bucketName, regionName, publicUrlBase, endpointUrl: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageService"/> class.
    /// </summary>
    /// <param name="accessKeyId">The access key ID.</param>
    /// <param name="secretAccessKey">The secret access key.</param>
    /// <param name="bucketName">The bucket name.</param>
    /// <param name="regionName">The signing region name.</param>
    /// <param name="publicUrlBase">An optional public URL base for files in the bucket.</param>
    /// <param name="endpointUrl">An optional S3-compatible endpoint URL.</param>
    protected S3StorageService(
        string accessKeyId,
        string secretAccessKey,
        string bucketName,
        string regionName,
        string? publicUrlBase,
        string? endpointUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretAccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(regionName);

        BucketName = bucketName;
        PublicUrlBase = publicUrlBase;
        RegionName = regionName;
        EndpointUrl = endpointUrl;
        _client = CreateClient(accessKeyId, secretAccessKey, regionName, endpointUrl);
    }

    /// <summary>
    /// Gets the bucket name.
    /// </summary>
    public string BucketName { get; }

    /// <summary>
    /// Gets the region name used for signing requests.
    /// </summary>
    public string RegionName { get; }

    /// <summary>
    /// Gets the configured public URL base.
    /// </summary>
    public string? PublicUrlBase { get; }

    /// <summary>
    /// Gets the custom S3 endpoint URL, or <see langword="null"/> for AWS S3.
    /// </summary>
    public string? EndpointUrl { get; }

    /// <inheritdoc />
    public override async Task WriteFileAsync(
        string filePath,
        byte[] content,
        string contentType = "application/octet-stream",
        string cacheControl = "public, max-age=31536000",
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheControl);

        using MemoryStream stream = new(content, writable: false);
        PutObjectRequest request = new()
        {
            BucketName = BucketName,
            Key = filePath,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = DisablePayloadSigning,
            DisableDefaultChecksumValidation = DisableDefaultChecksumValidation,
        };
        request.Headers.CacheControl = cacheControl;

        try
        {
            await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Failed to write file to S3: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            using GetObjectResponse response = await _client.GetObjectAsync(
                new GetObjectRequest { BucketName = BucketName, Key = filePath, },
                cancellationToken).ConfigureAwait(false);
            using MemoryStream output = new();
            await response.ResponseStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath, ex);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Failed to read file from S3: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            await _client.DeleteObjectAsync(
                new DeleteObjectRequest { BucketName = BucketName, Key = filePath, },
                cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file from S3: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            await _client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = BucketName, Key = filePath, },
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Failed to check file existence in S3: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<string> GetFileUrlAsync(string filePath, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!string.IsNullOrWhiteSpace(PublicUrlBase))
        {
            return JoinPublicUrl(PublicUrlBase, filePath);
        }

        GetPreSignedUrlRequest request = new()
        {
            BucketName = BucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.AddHours(1),
        };

        try
        {
            return await _client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate file URL: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public override string? GetFileNameByUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        string path = GetUrlPath(url);
        if (!string.IsNullOrWhiteSpace(PublicUrlBase) &&
            url.StartsWith(PublicUrlBase.TrimEnd('/'), StringComparison.Ordinal))
        {
            return NullIfEmpty(path);
        }

        string? host = GetUrlHost(url);
        if (!string.Equals(host, "s3.amazonaws.com", StringComparison.Ordinal))
        {
            return NullIfEmpty(path);
        }

        string[] parts = path.Split('/', 2);
        return parts.Length > 1 ? NullIfEmpty(parts[1]) : null;
    }

    /// <inheritdoc />
    public override Task CloseAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _client.Dispose();
        _disposed = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the path component from a URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns>The path without a leading slash.</returns>
    protected static string GetUrlPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        int pathEnd = GetPathEndIndex(url);
        return url[..pathEnd].TrimStart('/');
    }

    /// <summary>
    /// Gets the host component from an absolute URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns>The host, or <see langword="null"/> when the URL is not absolute.</returns>
    protected static string? GetUrlHost(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host : null;

    /// <summary>
    /// Converts empty strings to <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns><see langword="null"/> for empty strings; otherwise, the original value.</returns>
    protected static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    /// <summary>
    /// Joins a public URL base with a file path.
    /// </summary>
    /// <param name="publicUrlBase">The public URL base.</param>
    /// <param name="filePath">The object key.</param>
    /// <returns>The public URL.</returns>
    private static string JoinPublicUrl(string publicUrlBase, string filePath)
        => $"{publicUrlBase.TrimEnd('/')}/{filePath.TrimStart('/')}";

    private static int GetPathEndIndex(string value)
    {
        int queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        int fragmentIndex = value.IndexOf('#', StringComparison.Ordinal);

        return queryIndex < 0
            ? fragmentIndex < 0 ? value.Length : fragmentIndex
            : fragmentIndex < 0
                ? queryIndex
                : Math.Min(queryIndex, fragmentIndex);
    }

    private static bool IsNotFound(AmazonS3Exception exception)
        => exception.StatusCode == HttpStatusCode.NotFound ||
           string.Equals(exception.ErrorCode, "404", StringComparison.Ordinal) ||
           string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.Ordinal) ||
           string.Equals(exception.ErrorCode, "NotFound", StringComparison.Ordinal);

    private static AmazonS3Client CreateClient(
        string accessKeyId,
        string secretAccessKey,
        string regionName,
        string? endpointUrl)
    {
        BasicAWSCredentials credentials = new(accessKeyId, secretAccessKey);
        AmazonS3Config config = new();
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(regionName);
        }
        else
        {
            config.ServiceURL = endpointUrl;
            config.AuthenticationRegion = regionName;
            config.ForcePathStyle = true;
        }

        return new AmazonS3Client(credentials, config);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}