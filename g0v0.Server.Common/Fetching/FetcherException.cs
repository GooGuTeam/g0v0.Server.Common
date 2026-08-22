// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Fetching;

/// <summary>
/// Represents an error raised by the osu! Fetcher service.
/// </summary>
public class FetcherException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FetcherException"/> class.
    /// </summary>
    public FetcherException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FetcherException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FetcherException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FetcherException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FetcherException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}