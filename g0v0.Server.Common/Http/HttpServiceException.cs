// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Http;

/// <summary>
/// Represents an error raised by the <see cref="HttpService"/>.
/// </summary>
public class HttpServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceException"/> class.
    /// </summary>
    public HttpServiceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public HttpServiceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code of the failed response.</param>
    public HttpServiceException(string message, int? statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public HttpServiceException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the HTTP status code of the failed response, when available.
    /// </summary>
    public int? StatusCode { get; }
}