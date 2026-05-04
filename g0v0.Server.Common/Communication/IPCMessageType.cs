// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Runtime.Serialization;
using g0v0.Server.Common.Json;
using Newtonsoft.Json;

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Defines the IPC message type matching the <c>IPCMessageType</c> Python enum.
/// </summary>
[JsonConverter(typeof(LowercaseStringEnumConverter))]
public enum IpcMessageType
{
    /// <summary>
    /// A one-way notification message.
    /// </summary>
    [EnumMember(Value = "notice")]
    Notice,

    /// <summary>
    /// A request that expects a response.
    /// </summary>
    [EnumMember(Value = "request")]
    Request,

    /// <summary>
    /// A response to a previous request.
    /// </summary>
    [EnumMember(Value = "response")]
    Response,

    /// <summary>
    /// An error in response to a previous request.
    /// </summary>
    [EnumMember(Value = "error")]
    Error,
}