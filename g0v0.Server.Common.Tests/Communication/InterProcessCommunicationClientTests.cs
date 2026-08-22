// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using g0v0.Server.Common.Communication;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Communication;

[TestFixture]
public class InterProcessCommunicationClientTests
{
    [Test]
    public void GetServerIdentifier_WithKnownServerIdentifiers_ShouldConvertEnumNamesToSnakeCase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InterProcessCommunicationClient.GetServerIdentifier(ServerIdentify.Lazer), Is.EqualTo("lazer"));
            Assert.That(InterProcessCommunicationClient.GetServerIdentifier(ServerIdentify.Realtime), Is.EqualTo("realtime"));
        });
    }

    [Test]
    [Ignore("We don't have server with more than one word name.")]
    public void GetChannelName_WithServerIdentify_ShouldUseSnakeCaseIdentifier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InterProcessCommunicationClient.GetChannelName(ServerIdentify.Lazer), Is.EqualTo("g0v0:ipc:lazer"));
        });
    }

    [Test]
    public void GetChannelName_WithRawServerIdentifier_ShouldKeepIdentifierUnchanged()
    {
        Assert.That(InterProcessCommunicationClient.GetChannelName("MixedCaseServer"), Is.EqualTo("g0v0:ipc:MixedCaseServer"));
    }

    [Test]
    [Ignore("We don't have server with more than one word name.")]
    public void Constructor_WithServerIdentify_ShouldSubscribeToSnakeCaseChannel()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient client = new(transport, ServerIdentify.Lazer);

        Assert.Multiple(() =>
        {
            Assert.That(client.ServerIdentifier, Is.EqualTo("lazer"));
            Assert.That(transport.SubscribedChannels, Does.Contain("g0v0:ipc:lazer"));
        });
    }

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
    public async Task SendNoticeAsync_WithServerIdentifyTarget_ShouldPublishToSnakeCaseChannel()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, ServerIdentify.Lazer);
        InterProcessCommunicationClient receiver = new(transport, ServerIdentify.Realtime);
        TaskCompletionSource<AddRequest> receivedPayload = new(TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.RegisterNoticeHandler<AddRequest>("math.notice", payload =>
        {
            receivedPayload.TrySetResult(payload!);
            return Task.CompletedTask;
        });

        await sender.SendNoticeAsync(ServerIdentify.Realtime, "math.notice", new { left = 8, right = 13 });

        AddRequest request = await receivedPayload.Task.WaitAsync(TimeSpan.FromSeconds(1));
        PublishedMessage publishedMessage = transport.PublishedMessages.Single();
        JObject payload = JObject.Parse(publishedMessage.Payload);
        Assert.Multiple(() =>
        {
            Assert.That(request.Left, Is.EqualTo(8));
            Assert.That(request.Right, Is.EqualTo(13));
            Assert.That(publishedMessage.Channel, Is.EqualTo("g0v0:ipc:realtime"));
            Assert.That(payload.Value<string>("type"), Is.EqualTo("notice"));
            Assert.That(payload.Value<string>("name"), Is.EqualTo("math.notice"));
            Assert.That(payload.Value<string>("source_server"), Is.EqualTo("lazer"));
        });
    }

    [Test]
    public async Task SendNoticeAsync_WithTypedSourceHandler_ShouldPassSourceServerAndConvertPayload()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, ServerIdentify.Lazer);
        InterProcessCommunicationClient receiver = new(transport, ServerIdentify.Realtime);
        TaskCompletionSource<(string Source, AddRequest Payload)> receivedNotice = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        receiver.RegisterNoticeHandler<AddRequest>("math.notice", (source, payload) =>
        {
            receivedNotice.TrySetResult((source, payload!));
            return Task.CompletedTask;
        });

        await sender.SendNoticeAsync(ServerIdentify.Realtime, "math.notice", new { left = 8, right = 13 });

        (string Source, AddRequest Payload) notice = await receivedNotice.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(notice.Source, Is.EqualTo("lazer"));
            Assert.That(notice.Payload.Left, Is.EqualTo(8));
            Assert.That(notice.Payload.Right, Is.EqualTo(13));
        });
    }

    [Test]
    public async Task SendNoticeAsync_WithRawIdentifiers_ShouldKeepIdentifiersUnchanged()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient sender = new(transport, "GatewayServer");

        await sender.SendNoticeAsync("RealtimeServer", "math.notice", new { left = 21, right = 34 });

        PublishedMessage publishedMessage = transport.PublishedMessages.Single();
        JObject payload = JObject.Parse(publishedMessage.Payload);
        Assert.Multiple(() =>
        {
            Assert.That(sender.ServerIdentifier, Is.EqualTo("GatewayServer"));
            Assert.That(transport.SubscribedChannels, Does.Contain("g0v0:ipc:GatewayServer"));
            Assert.That(publishedMessage.Channel, Is.EqualTo("g0v0:ipc:RealtimeServer"));
            Assert.That(payload.Value<string>("source_server"), Is.EqualTo("GatewayServer"));
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
    public async Task RequestAsync_WithServerIdentifyTarget_ShouldUseSnakeCaseChannelsAndRoundTrip()
    {
        InMemoryInterProcessCommunicationTransport transport = new();
        InterProcessCommunicationClient requester = new(transport, ServerIdentify.Lazer);
        InterProcessCommunicationClient responder = new(transport, ServerIdentify.Realtime);

        responder.RegisterResponder<AddRequest, AddResponse>("math.sum", payload =>
        {
            AddRequest request = payload!;
            return Task.FromResult<AddResponse?>(new AddResponse { Total = request.Left + request.Right });
        });

        AddResponse? response = await requester.RequestAsync<AddResponse>(
            ServerIdentify.Realtime,
            "math.sum",
            new { left = 13, right = 21 });

        PublishedMessage[] publishedMessages = transport.PublishedMessages.ToArray();
        JObject requestPayload = JObject.Parse(publishedMessages[0].Payload);
        JObject responsePayload = JObject.Parse(publishedMessages[1].Payload);
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Total, Is.EqualTo(34));
            Assert.That(publishedMessages[0].Channel, Is.EqualTo("g0v0:ipc:realtime"));
            Assert.That(publishedMessages[1].Channel, Is.EqualTo("g0v0:ipc:lazer"));
            Assert.That(requestPayload.Value<string>("type"), Is.EqualTo("request"));
            Assert.That(requestPayload.Value<string>("name"), Is.EqualTo("math.sum"));
            Assert.That(requestPayload.Value<string>("source_server"), Is.EqualTo("lazer"));
            Assert.That(responsePayload.Value<string>("type"), Is.EqualTo("response"));
            Assert.That(responsePayload.Value<string>("source_server"), Is.EqualTo("realtime"));
        });
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
        private readonly ConcurrentQueue<PublishedMessage> _publishedMessages = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, Task>>> _subscriptions =
            new(StringComparer.Ordinal);

        public string[] SubscribedChannels => _subscriptions.Keys.ToArray();

        public PublishedMessage[] PublishedMessages => _publishedMessages.ToArray();

        public Task PublishAsync(string channel, string payload)
        {
            _publishedMessages.Enqueue(new PublishedMessage(channel, payload));
            return _subscriptions.TryGetValue(channel, out ConcurrentBag<Func<string, Task>>? handlers)
                ? Task.WhenAll(handlers.Select(handler => Task.Run(async () =>
                {
                    try
                    {
                        await handler(payload).ConfigureAwait(false);
                    }
                    catch
                    {
                        // The client under test is responsible for surfacing request errors.
                    }
                })))
                : Task.CompletedTask;
        }

        public void Subscribe(string channel, Func<string, Task> handler)
        {
            ConcurrentBag<Func<string, Task>> handlers = _subscriptions.GetOrAdd(
                channel,
                static _ => new ConcurrentBag<Func<string, Task>>());
            handlers.Add(handler);
        }
    }

    private sealed record PublishedMessage(string Channel, string Payload);

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