// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Data model for IPC errors, matching the <c>IPCErrorBody</c> Python model.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public sealed class IpcErrorBody
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IpcErrorBody"/> class.
    /// </summary>
    [JsonConstructor]
    public IpcErrorBody()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcErrorBody"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public IpcErrorBody(int code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonProperty("code", Required = Required.Always)]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonProperty("message", Required = Required.Always)]
    public string Message { get; set; } = string.Empty;
}