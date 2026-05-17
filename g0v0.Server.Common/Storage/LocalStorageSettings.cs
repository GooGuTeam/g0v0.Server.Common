// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Storage;

/// <summary>
/// Configuration options for local filesystem storage.
/// </summary>
public class LocalStorageSettings
{
    /// <summary>
    /// Gets or sets the storage directory path, relative to <see cref="Configuration.IPathProvider.GetBasePath"/> when not rooted.
    /// </summary>
    [JsonProperty("local_storage_path")]
    public string LocalStoragePath { get; set; } = "./storage";
}
