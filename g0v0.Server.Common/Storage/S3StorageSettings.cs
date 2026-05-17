// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Configuration options for AWS S3 storage.
/// </summary>
public class S3StorageSettings
{
    /// <summary>
    /// Gets or sets the AWS access key ID.
    /// </summary>
    [JsonProperty("s3_access_key_id")]
    public string S3AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    [JsonProperty("s3_secret_access_key")]
    public string S3SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 bucket name.
    /// </summary>
    [JsonProperty("s3_bucket_name")]
    public string S3BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region name.
    /// </summary>
    [JsonProperty("s3_region_name")]
    public string S3RegionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional public URL base for files in the bucket.
    /// </summary>
    [JsonProperty("s3_public_url_base")]
    public string? S3PublicUrlBase { get; set; }
}
