// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Configuration options for Cloudflare R2 storage.
/// </summary>
public class CloudflareR2Settings
{
    /// <summary>
    /// Gets or sets the Cloudflare account ID.
    /// </summary>
    [JsonProperty("r2_account_id")]
    public string R2AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the R2 access key ID.
    /// </summary>
    [JsonProperty("r2_access_key_id")]
    public string R2AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the R2 secret access key.
    /// </summary>
    [JsonProperty("r2_secret_access_key")]
    public string R2SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the R2 bucket name.
    /// </summary>
    [JsonProperty("r2_bucket_name")]
    public string R2BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional public URL base for files in the bucket.
    /// </summary>
    [JsonProperty("r2_public_url_base")]
    public string? R2PublicUrlBase { get; set; }
}
