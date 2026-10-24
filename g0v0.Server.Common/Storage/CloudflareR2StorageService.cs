// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Stores files in Cloudflare R2.
/// </summary>
public class CloudflareR2StorageService : S3StorageService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareR2StorageService"/> class.
    /// </summary>
    /// <param name="settings">The Cloudflare R2 storage settings.</param>
    public CloudflareR2StorageService(CloudflareR2Settings settings)
        : this(
            (settings ?? throw new ArgumentNullException(nameof(settings))).R2AccountId,
            settings.R2AccessKeyId,
            settings.R2SecretAccessKey,
            settings.R2BucketName,
            settings.R2PublicUrlBase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareR2StorageService"/> class.
    /// </summary>
    /// <param name="accountId">The Cloudflare account ID.</param>
    /// <param name="accessKeyId">The R2 access key ID.</param>
    /// <param name="secretAccessKey">The R2 secret access key.</param>
    /// <param name="bucketName">The R2 bucket name.</param>
    /// <param name="publicUrlBase">An optional public URL base for files in the bucket.</param>
    public CloudflareR2StorageService(
        string accountId,
        string accessKeyId,
        string secretAccessKey,
        string bucketName,
        string? publicUrlBase = null)
        : base(
            accessKeyId,
            secretAccessKey,
            bucketName,
            regionName: "auto",
            publicUrlBase,
            endpointUrl: CreateEndpointUrl(accountId))
    {
        AccountId = accountId;
    }

    /// <summary>
    /// Gets the Cloudflare account ID.
    /// </summary>
    public string AccountId { get; }

    /// <inheritdoc/>
    protected override bool DisablePayloadSigning => true;

    /// <inheritdoc/>
    protected override bool DisableDefaultChecksumValidation => true;

    /// <inheritdoc />
    public override string? GetFileNameByUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        string path = GetUrlPath(url);
        return NullIfEmpty(path);
    }

    private static string CreateEndpointUrl(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        return $"https://{accountId}.r2.cloudflarestorage.com";
    }
}