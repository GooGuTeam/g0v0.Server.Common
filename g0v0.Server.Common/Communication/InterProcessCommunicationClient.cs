// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Provides g0v0 v2 inter-process communication over Redis pub/sub channels.
/// </summary>
public sealed class InterProcessCommunicationClient
{
    /// <summary>
    /// The Redis pub/sub channel prefix used by the IPC protocol.
    /// </summary>
    public const string ChannelPrefix = "g0v0:ipc:";

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Include,
    });

    private readonly ConcurrentDictionary<string, TypedHandlerRegistration> _noticeHandlers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TypedResponderRegistration> _responders = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, PendingRequest> _pendingRequests = new();
    private readonly IInterProcessCommunicationTransport _transport;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationClient"/> class.
    /// </summary>
    /// <param name="transport">The message transport implementation.</param>
    /// <param name="serverIdentifier">The current server identifier.</param>
    public InterProcessCommunicationClient(IInterProcessCommunicationTransport transport, string serverIdentifier)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverIdentifier);

        _transport = transport;
        ServerIdentifier = serverIdentifier;

        _transport.Subscribe(GetChannelName(serverIdentifier), HandleTransportMessageAsync);
    }

    /// <summary>
    /// Gets the current server identifier.
    /// </summary>
    public string ServerIdentifier { get; }

    /// <summary>
    /// Builds the Redis pub/sub channel name for a server identifier.
    /// </summary>
    /// <param name="serverIdentifier">The target server identifier.</param>
    /// <returns>The Redis channel name.</returns>
    public static string GetChannelName(string serverIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverIdentifier);

        return $"{ChannelPrefix}{serverIdentifier}";
    }

    /// <summary>
    /// Registers a notice handler without a specific payload type.
    /// </summary>
    /// <param name="name">The notice name.</param>
    /// <param name="handler">The notice handler.</param>
    public void RegisterNoticeHandler(string name, Func<object?, Task> handler)
        => RegisterNoticeHandler<object>(name, handler);

    /// <summary>
    /// Registers a notice handler and converts the incoming payload to the supplied type before invocation.
    /// </summary>
    /// <param name="name">The notice name.</param>
    /// <param name="payloadType">The payload type to deserialize to.</param>
    /// <param name="handler">The notice handler.</param>
    public void RegisterNoticeHandler(string name, Type payloadType, Func<object?, Task> handler)
    {
        ValidateHandlerRegistration(name, payloadType, handler);

        if (!_noticeHandlers.TryAdd(name, new TypedHandlerRegistration(payloadType, handler)))
        {
            throw new InvalidOperationException($"A notice handler named '{name}' is already registered.");
        }
    }

    /// <summary>
    /// Registers a typed notice handler.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="name">The notice name.</param>
    /// <param name="handler">The notice handler.</param>
    public void RegisterNoticeHandler<TPayload>(string name, Func<TPayload?, Task> handler)
        => RegisterNoticeHandler(name, typeof(TPayload), payload => handler(CastPayload<TPayload>(payload)));

    /// <summary>
    /// Registers a typed synchronous notice handler.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="name">The notice name.</param>
    /// <param name="handler">The notice handler.</param>
    public void RegisterNoticeHandler<TPayload>(string name, Action<TPayload?> handler)
        => RegisterNoticeHandler(name, typeof(TPayload), payload =>
        {
            handler(CastPayload<TPayload>(payload));
            return Task.CompletedTask;
        });

    /// <summary>
    /// Registers a responder without a specific payload type.
    /// </summary>
    /// <param name="name">The request name.</param>
    /// <param name="responder">The responder implementation.</param>
    public void RegisterResponder(string name, Func<object?, Task<object?>> responder)
        => RegisterResponder<object>(name, responder);

    /// <summary>
    /// Registers a responder and converts the incoming payload to the supplied type before invocation.
    /// </summary>
    /// <param name="name">The request name.</param>
    /// <param name="payloadType">The payload type to deserialize to.</param>
    /// <param name="responder">The responder implementation.</param>
    public void RegisterResponder(string name, Type payloadType, Func<object?, Task<object?>> responder)
    {
        ValidateHandlerRegistration(name, payloadType, responder);

        if (!_responders.TryAdd(name, new TypedResponderRegistration(payloadType, responder)))
        {
            throw new InvalidOperationException($"A responder named '{name}' is already registered.");
        }
    }

    /// <summary>
    /// Registers a typed responder.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <param name="name">The request name.</param>
    /// <param name="responder">The responder implementation.</param>
    public void RegisterResponder<TRequest>(string name, Func<TRequest?, Task<object?>> responder)
        => RegisterResponder(name, typeof(TRequest), payload => responder(CastPayload<TRequest>(payload)));

    /// <summary>
    /// Registers a typed responder.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The response payload type.</typeparam>
    /// <param name="name">The request name.</param>
    /// <param name="responder">The responder implementation.</param>
    public void RegisterResponder<TRequest, TResponse>(string name, Func<TRequest?, Task<TResponse?>> responder)
        => RegisterResponder(name, typeof(TRequest), async payload =>
            await responder(CastPayload<TRequest>(payload)).ConfigureAwait(false));

    /// <summary>
    /// Registers a typed synchronous responder.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The response payload type.</typeparam>
    /// <param name="name">The request name.</param>
    /// <param name="responder">The responder implementation.</param>
    public void RegisterResponder<TRequest, TResponse>(string name, Func<TRequest?, TResponse?> responder)
        => RegisterResponder(name, typeof(TRequest), payload =>
            Task.FromResult<object?>(responder(CastPayload<TRequest>(payload))));

    /// <summary>
    /// Sends a one-way notice to another server.
    /// </summary>
    /// <param name="targetServerIdentifier">The target server identifier.</param>
    /// <param name="name">The notice name.</param>
    /// <param name="payload">The payload to serialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes once the notice has been published.</returns>
    public Task SendNoticeAsync(
        string targetServerIdentifier,
        string name,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        ValidateName(name);

        var envelope = CreateInvocationEnvelope(MessageTypes.Notice, Guid.NewGuid(), name, payload, sourceServer: null);
        return PublishEnvelopeAsync(targetServerIdentifier, envelope, cancellationToken);
    }

    /// <summary>
    /// Sends a request and converts the response to the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="targetServerIdentifier">The target server identifier.</param>
    /// <param name="name">The request name.</param>
    /// <param name="payload">The request payload.</param>
    /// <param name="timeout">The request timeout. Defaults to 30 seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
    public async Task<TResponse?> RequestAsync<TResponse>(
        string targetServerIdentifier,
        string name,
        object? payload = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        object? response = await RequestAsync(targetServerIdentifier, name, typeof(TResponse), payload, timeout, cancellationToken)
            .ConfigureAwait(false);
        return response is null ? default : (TResponse)response;
    }

    /// <summary>
    /// Sends a request and converts the response to the specified type.
    /// </summary>
    /// <param name="targetServerIdentifier">The target server identifier.</param>
    /// <param name="name">The request name.</param>
    /// <param name="responseType">The expected response payload type.</param>
    /// <param name="payload">The request payload.</param>
    /// <param name="timeout">The request timeout. Defaults to 30 seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response payload.</returns>
    public async Task<object?> RequestAsync(
        string targetServerIdentifier,
        string name,
        Type responseType,
        object? payload = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(responseType);
        ValidateName(name);

        Guid requestId = Guid.NewGuid();
        var pendingRequest = new PendingRequest();

        if (!_pendingRequests.TryAdd(requestId, pendingRequest))
        {
            throw new InvalidOperationException($"Failed to register the pending request '{requestId}'.");
        }

        // Updated to avoid capturing a variable that might be disposed
        using var timeoutCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = timeoutCancellationSource.Token;
        timeoutCancellationSource.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
        using var cancellationRegistration = token.Register(() => pendingRequest.TrySetCanceled(token));

        try
        {
            var envelope = CreateInvocationEnvelope(MessageTypes.Request, requestId, name, payload, ServerIdentifier);
            await PublishEnvelopeAsync(targetServerIdentifier, envelope, timeoutCancellationSource.Token).ConfigureAwait(false);

            JToken? responsePayload = await pendingRequest.Task.ConfigureAwait(false);
            return ConvertPayload(responsePayload, responseType);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"IPC request '{name}' to '{targetServerIdentifier}' timed out.");
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private static TPayload? CastPayload<TPayload>(object? payload)
        => payload is null ? default : (TPayload)payload;

    private static object? ConvertPayload(JToken? payload, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (typeof(JToken).IsAssignableFrom(targetType))
        {
            return payload?.DeepClone();
        }

        if (targetType == typeof(object))
        {
            return payload?.ToObject<object?>(Serializer);
        }

        if (payload is null || payload.Type == JTokenType.Null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
            {
                throw new JsonSerializationException(
                    $"Cannot convert a null IPC payload to '{targetType.FullName}'.");
            }

            return null;
        }

        object? convertedPayload = payload.ToObject(targetType, Serializer);
        return convertedPayload ?? throw new JsonSerializationException(
            $"Failed to convert the IPC payload to '{targetType.FullName}'.");
    }

    private static InterProcessCommunicationEnvelope CreateErrorEnvelope(Guid uuid, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new InterProcessCommunicationEnvelope
        {
            Type = MessageTypes.Error,
            Uuid = uuid,
            Data = JObject.FromObject(
                new InterProcessCommunicationErrorData
                {
                    Message = message,
                },
                Serializer),
        };
    }

    private static InterProcessCommunicationEnvelope CreateInvocationEnvelope(
        string messageType,
        Guid uuid,
        string name,
        object? payload,
        string? sourceServer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ValidateName(name);

        return new InterProcessCommunicationEnvelope
        {
            Type = messageType,
            Uuid = uuid,
            Data = JObject.FromObject(
                new InterProcessCommunicationInvocationData
                {
                    Name = name,
                    SourceServer = sourceServer,
                    Payload = ToToken(payload),
                },
                Serializer),
        };
    }

    private static InterProcessCommunicationEnvelope CreateResponseEnvelope(Guid uuid, object? payload)
    {
        return new InterProcessCommunicationEnvelope
        {
            Type = MessageTypes.Response,
            Uuid = uuid,
            Data = JObject.FromObject(
                new InterProcessCommunicationResponseData
                {
                    Payload = ToToken(payload),
                },
                Serializer),
        };
    }

    private static TData DeserializeData<TData>(JObject data)
        where TData : class
    {
        TData? deserializedData = data.ToObject<TData>(Serializer);
        return deserializedData ?? throw new JsonSerializationException(
            $"Failed to deserialize IPC data as '{typeof(TData).FullName}'.");
    }

    private static InterProcessCommunicationEnvelope DeserializeEnvelope(string rawMessage)
    {
        InterProcessCommunicationEnvelope? envelope = JsonConvert.DeserializeObject<InterProcessCommunicationEnvelope>(rawMessage);
        return envelope ?? throw new JsonSerializationException("Failed to deserialize the IPC message envelope.");
    }

    private static JToken? ToToken(object? payload)
    {
        return payload switch
        {
            null => null,
            JToken token => token.DeepClone(),
            _ => JToken.FromObject(payload, Serializer),
        };
    }

    private static void ValidateHandlerRegistration<TDelegate>(
        string name,
        Type payloadType,
        TDelegate handler)
        where TDelegate : Delegate
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(payloadType);
        ArgumentNullException.ThrowIfNull(handler);
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }

    private async Task HandleNoticeAsync(InterProcessCommunicationEnvelope envelope)
    {
        InterProcessCommunicationInvocationData noticeData = DeserializeData<InterProcessCommunicationInvocationData>(envelope.Data);
        if (!_noticeHandlers.TryGetValue(noticeData.Name, out TypedHandlerRegistration? handlerRegistration))
        {
            return;
        }

        object? payload = ConvertPayload(noticeData.Payload, handlerRegistration.PayloadType);
        await handlerRegistration.Handler(payload).ConfigureAwait(false);
    }

    private void HandleResponse(InterProcessCommunicationEnvelope envelope)
    {
        if (!_pendingRequests.TryGetValue(envelope.Uuid, out PendingRequest? pendingRequest))
        {
            return;
        }

        InterProcessCommunicationResponseData responseData = DeserializeData<InterProcessCommunicationResponseData>(envelope.Data);
        pendingRequest.TrySetResult(responseData.Payload);
    }

    private void HandleError(InterProcessCommunicationEnvelope envelope)
    {
        if (!_pendingRequests.TryGetValue(envelope.Uuid, out PendingRequest? pendingRequest))
        {
            return;
        }

        InterProcessCommunicationErrorData errorData = DeserializeData<InterProcessCommunicationErrorData>(envelope.Data);
        pendingRequest.TrySetException(new InterProcessCommunicationRemoteException(envelope.Uuid, errorData.Message));
    }

    private async Task HandleRequestAsync(InterProcessCommunicationEnvelope envelope)
    {
        InterProcessCommunicationInvocationData requestData = DeserializeData<InterProcessCommunicationInvocationData>(envelope.Data);
        if (string.IsNullOrWhiteSpace(requestData.SourceServer))
        {
            return;
        }

        if (!_responders.TryGetValue(requestData.Name, out TypedResponderRegistration? responderRegistration))
        {
            await PublishEnvelopeAsync(
                requestData.SourceServer,
                CreateErrorEnvelope(envelope.Uuid, $"No responder registered for '{requestData.Name}'."),
                CancellationToken.None).ConfigureAwait(false);
            return;
        }

        try
        {
            object? payload = ConvertPayload(requestData.Payload, responderRegistration.PayloadType);
            object? response = await responderRegistration.Responder(payload).ConfigureAwait(false);
            await PublishEnvelopeAsync(
                requestData.SourceServer,
                CreateResponseEnvelope(envelope.Uuid, response),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await PublishEnvelopeAsync(
                requestData.SourceServer,
                CreateErrorEnvelope(envelope.Uuid, ex.Message),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task HandleTransportMessageAsync(string rawMessage)
    {
        try
        {
            InterProcessCommunicationEnvelope envelope = DeserializeEnvelope(rawMessage);
            switch (envelope.Type)
            {
                case MessageTypes.Notice:
                    await HandleNoticeAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.Request:
                    await HandleRequestAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.Response:
                    HandleResponse(envelope);
                    break;

                case MessageTypes.Error:
                    HandleError(envelope);
                    break;
            }
        }
        catch
        {
            // Ignore malformed transport messages so a bad payload does not stop the subscription.
        }
    }

    private Task PublishEnvelopeAsync(
        string targetServerIdentifier,
        InterProcessCommunicationEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetServerIdentifier);
        ArgumentNullException.ThrowIfNull(envelope);

        string channel = GetChannelName(targetServerIdentifier);
        string payload = JsonConvert.SerializeObject(envelope);
        return _transport.PublishAsync(channel, payload).WaitAsync(cancellationToken);
    }

    private static class MessageTypes
    {
        public const string Error = "error";
        public const string Notice = "notice";
        public const string Request = "request";
        public const string Response = "response";
    }

    private sealed class TypedHandlerRegistration
    {
        public TypedHandlerRegistration(Type payloadType, Func<object?, Task> handler)
        {
            PayloadType = payloadType;
            Handler = handler;
        }

        public Type PayloadType { get; }

        public Func<object?, Task> Handler { get; }
    }

    private sealed class TypedResponderRegistration(Type payloadType, Func<object?, Task<object?>> responder)
    {
        public Type PayloadType { get; } = payloadType;

        public Func<object?, Task<object?>> Responder { get; } = responder;
    }

    private sealed class PendingRequest
    {
        private readonly TaskCompletionSource<JToken?> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<JToken?> Task => _completionSource.Task;

        public bool TrySetCanceled(CancellationToken cancellationToken)
            => _completionSource.TrySetCanceled(cancellationToken);

        public bool TrySetException(Exception exception)
            => _completionSource.TrySetException(exception);

        public bool TrySetResult(JToken? payload)
            => _completionSource.TrySetResult(payload);
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class InterProcessCommunicationEnvelope
    {
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("uuid", Required = Required.Always)]
        public Guid Uuid { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public JObject Data { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class InterProcessCommunicationInvocationData
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("source_server", NullValueHandling = NullValueHandling.Ignore)]
        public string? SourceServer { get; set; }

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        public JToken? Payload { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class InterProcessCommunicationResponseData
    {
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Include)]
        public JToken? Payload { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class InterProcessCommunicationErrorData
    {
        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; } = string.Empty;
    }
}