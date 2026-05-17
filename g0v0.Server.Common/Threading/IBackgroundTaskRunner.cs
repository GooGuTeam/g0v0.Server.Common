// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

namespace g0v0.Server.Common.Threading;

/// <summary>
/// Runs delegated work on the thread pool immediately as a background task.
/// </summary>
public interface IBackgroundTaskRunner
{
    /// <summary>
    /// Starts the specified synchronous action immediately on a background thread.
    /// </summary>
    /// <param name="task">The action to execute.</param>
    /// <param name="cancellationToken">A token used to cancel the background task.</param>
    /// <returns>The running background task.</returns>
    Task RunAsync(Action task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the specified asynchronous operation immediately on a background thread.
    /// </summary>
    /// <param name="task">The asynchronous operation to execute.</param>
    /// <param name="cancellationToken">A token used to cancel the background task.</param>
    /// <returns>The running background task.</returns>
    Task RunAsync(Func<Task> task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the specified asynchronous operation immediately on a background thread.
    /// </summary>
    /// <param name="task">The asynchronous operation to execute.</param>
    /// <param name="cancellationToken">A token passed to the delegated work.</param>
    /// <returns>The running background task.</returns>
    Task RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default);
}
