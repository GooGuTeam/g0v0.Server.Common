// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using g0v0.Server.Common.Communication;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Communication;

[TestFixture]
public class InterProcessCommunicationClientTests
{
    [Test]
    public async Task SendNoticeAsync_WithExplicitPayloadType_ShouldConvertPayloadBeforeInvokingHandler()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, "gateway");
        InterProcessCommunicationClient receiver = new(transport, "realtime");
        TaskCompletionSource<AddRequest> receivedPayload = new(TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.RegisterNoticeHandler<AddRequest>("math.notice", payload =>
        {
            receivedPayload.TrySetResult(payload!);
            return Task.CompletedTask;
        });

        await sender.SendNoticeAsync("realtime", "math.notice", new { left = 3, right = 4 });

        AddRequest request = await receivedPayload.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.That(request.Left, Is.EqualTo(3));
        Assert.That(request.Right, Is.EqualTo(4));
    }

    [Test]
    public async Task SendNoticeAsync_WithTypedSourceHandler_ShouldPassSourceServerAndConvertPayload()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, "lazer");
        InterProcessCommunicationClient receiver = new(transport, "realtime");
        TaskCompletionSource<(string Source, AddRequest Payload)> receivedNotice = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.RegisterNoticeHandler<AddRequest>("math.notice", (source, payload) =>
        {
            receivedNotice.TrySetResult((source, payload!));
            return Task.CompletedTask;
        });

        await sender.SendNoticeAsync("realtime", "math.notice", new { left = 8, right = 13 });

        (string Source, AddRequest Payload) notice = await receivedNotice.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(notice.Source, Is.EqualTo("lazer"));
            Assert.That(notice.Payload.Left, Is.EqualTo(8));
            Assert.That(notice.Payload.Right, Is.EqualTo(13));
        });
    }

    [Test]
    public async Task SendNoticeAsync_WithExplicitSourceHandler_ShouldPassSourceServerAndConvertPayload()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, "gateway");
        InterProcessCommunicationClient receiver = new(transport, "realtime");
        TaskCompletionSource<(string Source, AddRequest Payload)> receivedNotice = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.RegisterNoticeHandler<AddRequest>("math.notice", (source, payload) =>
        {
            receivedNotice.TrySetResult((source, payload!));
            return Task.CompletedTask;
        });

        await sender.SendNoticeAsync("realtime", "math.notice", new { left = 21, right = 34 });

        (string Source, AddRequest Payload) notice = await receivedNotice.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(notice.Source, Is.EqualTo("gateway"));
            Assert.That(notice.Payload.Left, Is.EqualTo(21));
            Assert.That(notice.Payload.Right, Is.EqualTo(34));
        });
    }

    [Test]
    public async Task RequestAsync_WithExplicitPayloadType_ShouldConvertRequestAndResponsePayloads()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient requester = new(transport, "gateway");
        InterProcessCommunicationClient responder = new(transport, "realtime");

        responder.RegisterResponder<AddRequest>("math.sum", payload =>
        {
            AddRequest request = payload!;
            return Task.FromResult<object?>(new AddResponse { Total = request.Left + request.Right });
        });

        AddResponse? response = await requester.RequestAsync<AddResponse>(
            "realtime",
            "math.sum",
            new { left = 5, right = 7 });

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Total, Is.EqualTo(12));
    }

    [Test]
    public void RequestAsync_WhenResponderThrows_ShouldSurfaceRemoteException()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient requester = new(transport, "gateway");
        InterProcessCommunicationClient responder = new(transport, "realtime");

        responder.RegisterResponder<AddRequest>("math.fail", _ => Task.FromException<object?>(new InvalidOperationException("boom")));

        Assert.That(
            async () => await requester.RequestAsync<object?>(
                "realtime",
                "math.fail",
                new { left = 1, right = 2 }),
            Throws.TypeOf<InterProcessCommunicationRemoteException>()
                .With.Message.Contain("boom"));
    }

    private sealed class InMemoryInterProcessCommunicationTransport : IInterProcessCommunicationTransport
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, Task>>> _subscriptions =
            new(StringComparer.Ordinal);

        public Task PublishAsync(string channel, string payload)
        {
            if (!_subscriptions.TryGetValue(channel, out ConcurrentBag<Func<string, Task>>? handlers))
            {
                return Task.CompletedTask;
            }

            foreach (Func<string, Task> handler in handlers)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(payload).ConfigureAwait(false);
                    }
                    catch
                    {
                        // The client under test is responsible for surfacing request errors.
                    }
                });
            }

            return Task.CompletedTask;
        }

        public void Subscribe(string channel, Func<string, Task> handler)
        {
            ConcurrentBag<Func<string, Task>> handlers = _subscriptions.GetOrAdd(
                channel,
                static _ => new ConcurrentBag<Func<string, Task>>());
            handlers.Add(handler);
        }
    }

    private sealed class AddRequest
    {
        public int Left { get; set; }

        public int Right { get; set; }
    }

    private sealed class AddResponse
    {
        public int Total { get; set; }
    }
}