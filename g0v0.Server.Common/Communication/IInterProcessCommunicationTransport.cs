// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Defines the transport used by the inter-process communication client.
/// </summary>
public interface IInterProcessCommunicationTransport
{
    /// <summary>
    /// Subscribes to a raw transport channel.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <param name="handler">The callback invoked for each raw message payload.</param>
    void Subscribe(string channel, Func<string, Task> handler);

    /// <summary>
    /// Publishes a raw payload to a transport channel.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <param name="payload">The serialized payload.</param>
    /// <returns>A task that completes once the payload has been published.</returns>
    Task PublishAsync(string channel, string payload);
}