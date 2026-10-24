// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Data model for IPC messages, matching the <c>IPCMessage</c> Python model.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public sealed class IpcMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcMessage"/> class.
    /// </summary>
    [JsonConstructor]
    public IpcMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcMessage"/> class.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="name">The message name.</param>
    /// <param name="uuid">The message identifier.</param>
    /// <param name="data">The message data payload.</param>
    public IpcMessage(IpcMessageType type, string name, Guid uuid, object? data = null)
    {
        Type = type;
        Name = name;
        Uuid = uuid;
        Data = data;
    }

    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    [JsonProperty("type", Required = Required.Always)]
    public IpcMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the message name.
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    [JsonProperty("uuid", Required = Required.Always)]
    public Guid Uuid { get; set; }

    /// <summary>
    /// Gets or sets the source server identifier used for request/response routing.
    /// Only set on requests; <see langword="null"/> for notices and responses.
    /// </summary>
    [JsonProperty("source_server", NullValueHandling = NullValueHandling.Ignore)]
    public string? SourceServer { get; set; }

    /// <summary>
    /// Gets or sets the message data payload.
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Include)]
    public object? Data { get; set; }
}