// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using StackExchange.Redis;

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Implements the IPC transport on top of Redis pub/sub.
/// </summary>
public class RedisInterProcessCommunicationTransport : IInterProcessCommunicationTransport
{
    private readonly ISubscriber _subscriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisInterProcessCommunicationTransport"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    public RedisInterProcessCommunicationTransport(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    /// <inheritdoc/>
    public Task PublishAsync(string channel, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(payload);

        return _subscriber.PublishAsync(RedisChannel.Literal(channel), payload);
    }

    /// <inheritdoc/>
    public void Subscribe(string channel, Func<string, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(handler);

        _subscriber.Subscribe(RedisChannel.Literal(channel), (redisChannel, value) =>
        {
            _ = handler(value.ToString());
        });
    }
}