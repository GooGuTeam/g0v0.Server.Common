// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Communication;

/// <summary>
/// Represents an error returned by a remote IPC responder.
/// </summary>
public sealed class InterProcessCommunicationRemoteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationRemoteException"/> class.
    /// </summary>
    public InterProcessCommunicationRemoteException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationRemoteException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InterProcessCommunicationRemoteException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationRemoteException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InterProcessCommunicationRemoteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationRemoteException"/> class.
    /// </summary>
    /// <param name="requestId">The request identifier that caused the remote error.</param>
    /// <param name="message">The remote error message.</param>
    public InterProcessCommunicationRemoteException(Guid requestId, string message)
        : base(message)
    {
        RequestId = requestId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterProcessCommunicationRemoteException"/> class.
    /// </summary>
    /// <param name="requestId">The request identifier that caused the remote error.</param>
    /// <param name="code">The remote error code.</param>
    /// <param name="message">The remote error message.</param>
    public InterProcessCommunicationRemoteException(Guid requestId, int code, string message)
        : base(message)
    {
        RequestId = requestId;
        Code = code;
    }

    /// <summary>
    /// Gets the request identifier associated with the remote error.
    /// </summary>
    public Guid? RequestId { get; }

    /// <summary>
    /// Gets the remote error code.
    /// </summary>
    public int Code { get; }
}
