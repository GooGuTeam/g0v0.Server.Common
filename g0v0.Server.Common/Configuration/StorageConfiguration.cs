// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Configuration.Attributes;
using Newtonsoft.Json.Linq;

namespace g0v0.Server.Common.Configuration;

/// <summary>
/// Represents the storage settings for the application.
/// </summary>
[ConfigurationFile("storage")]
public class StorageConfiguration
{
    /// <summary>
    /// Represents the type of storage backend.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// The local storage.
        /// </summary>
        Local,

        /// <summary>
        /// Amazon S3 storage service.
        /// </summary>
        S3,

        /// <summary>
        /// Cloudflare R2 storage service.
        /// </summary>
        R2,
    }

    /// <summary>
    /// Gets or sets the storage type.
    /// </summary>
    public StorageType Type { get; set; }

    /// <summary>
    /// Gets or sets the storage options.
    /// </summary>
    public JToken Options { get; set; } = new JObject();
}