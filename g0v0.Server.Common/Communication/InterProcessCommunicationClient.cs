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

        var message = new IpcMessage(IpcMessageType.Notice, name, Guid.NewGuid(), payload);
        return PublishMessageAsync(targetServerIdentifier, message, cancellationToken);
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
            var message = new IpcMessage(IpcMessageType.Request, name, requestId, payload);
            await PublishMessageAsync(targetServerIdentifier, message, timeoutCancellationSource.Token).ConfigureAwait(false);

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

    private static IpcMessage DeserializeMessage(string rawMessage)
    {
        IpcMessage? message = JsonConvert.DeserializeObject<IpcMessage>(rawMessage);
        return message ?? throw new JsonSerializationException("Failed to deserialize the IPC message.");
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

    private static IpcMessage CreateErrorIpcMessage(Guid uuid, int code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new IpcMessage(IpcMessageType.Error, string.Empty, uuid, new IpcErrorBody(code, message));
    }

    private async Task HandleNoticeAsync(IpcMessage message)
    {
        if (!_noticeHandlers.TryGetValue(message.Name, out TypedHandlerRegistration? handlerRegistration))
        {
            return;
        }

        object? payload = ConvertPayload(message.Data as JToken, handlerRegistration.PayloadType);
        await handlerRegistration.Handler(payload).ConfigureAwait(false);
    }

    private void HandleResponse(IpcMessage message)
    {
        if (!_pendingRequests.TryGetValue(message.Uuid, out PendingRequest? pendingRequest))
        {
            return;
        }

        pendingRequest.TrySetResult(message.Data as JToken);
    }

    private void HandleError(IpcMessage message)
    {
        if (!_pendingRequests.TryGetValue(message.Uuid, out PendingRequest? pendingRequest))
        {
            return;
        }

        // Error data is always a structured IpcErrorBody
        IpcErrorBody? errorBody = null;
        if (message.Data is JObject jObject)
        {
            errorBody = jObject.ToObject<IpcErrorBody>(Serializer);
        }

        int code = errorBody?.Code ?? 0;
        string msg = errorBody?.Message ?? "Unknown IPC error";
        pendingRequest.TrySetException(new InterProcessCommunicationRemoteException(message.Uuid, code, msg));
    }

    private async Task HandleRequestAsync(IpcMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.SourceServer))
        {
            return;
        }

        if (!_responders.TryGetValue(message.Name, out TypedResponderRegistration? responderRegistration))
        {
            await PublishMessageAsync(
                message.SourceServer,
                CreateErrorIpcMessage(message.Uuid, 404, $"No responder registered for '{message.Name}'."),
                CancellationToken.None).ConfigureAwait(false);
            return;
        }

        try
        {
            object? payload = ConvertPayload(message.Data as JToken, responderRegistration.PayloadType);
            object? response = await responderRegistration.Responder(payload).ConfigureAwait(false);
            await PublishMessageAsync(
                message.SourceServer,
                new IpcMessage(IpcMessageType.Response, string.Empty, message.Uuid, ToToken(response)),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await PublishMessageAsync(
                message.SourceServer,
                CreateErrorIpcMessage(message.Uuid, 500, ex.Message),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task HandleTransportMessageAsync(string rawMessage)
    {
        try
        {
            IpcMessage message = DeserializeMessage(rawMessage);
            switch (message.Type)
            {
                case IpcMessageType.Notice:
                    await HandleNoticeAsync(message).ConfigureAwait(false);
                    break;

                case IpcMessageType.Request:
                    await HandleRequestAsync(message).ConfigureAwait(false);
                    break;

                case IpcMessageType.Response:
                    HandleResponse(message);
                    break;

                case IpcMessageType.Error:
                    HandleError(message);
                    break;
            }
        }
        catch
        {
            // Ignore malformed transport messages so a bad payload does not stop the subscription.
        }
    }

    private Task PublishMessageAsync(
        string targetServerIdentifier,
        IpcMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetServerIdentifier);
        ArgumentNullException.ThrowIfNull(message);

        message.SourceServer = ServerIdentifier;

        string channel = GetChannelName(targetServerIdentifier);
        string payload = JsonConvert.SerializeObject(message);
        return _transport.PublishAsync(channel, payload).WaitAsync(cancellationToken);
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
}