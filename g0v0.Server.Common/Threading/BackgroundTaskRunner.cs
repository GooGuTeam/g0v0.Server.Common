// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.Threading;

/// <summary>
/// Runs delegated work immediately on the thread pool and tracks active executions.
/// </summary>
public class BackgroundTaskRunner : IBackgroundTaskRunner, IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, Task> _runningTasks = new();
    private readonly ILogger<BackgroundTaskRunner> _logger;
    private readonly CancellationTokenSource _stoppingSource = new();
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundTaskRunner"/> class.
    /// </summary>
    /// <param name="logger">The logger used for background task failures.</param>
    /// <param name="serviceProvider">The root service provider for optional host lifetime integration.</param>
    public BackgroundTaskRunner(ILogger<BackgroundTaskRunner> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;

        var applicationLifetime = serviceProvider.GetService<IHostApplicationLifetime>();
        applicationLifetime?.ApplicationStopping.Register(_stoppingSource.Cancel);
    }

    /// <inheritdoc/>
    public Task RunAsync(Action task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        return this.RunAsync(
            _ =>
            {
                task();
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RunAsync(Func<Task> task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        return this.RunAsync(_ => task(), cancellationToken);
    }

    /// <inheritdoc/>
    public Task RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_stoppingSource.Token, cancellationToken);

        if (linkedSource.IsCancellationRequested)
        {
            linkedSource.Dispose();
            return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : _stoppingSource.Token);
        }

        var backgroundTask = Task.Run(() => task(linkedSource.Token), CancellationToken.None);
        var taskId = backgroundTask.Id;

        _runningTasks.TryAdd(taskId, backgroundTask);
        this.ObserveTaskCompletion(backgroundTask, taskId, linkedSource);

        return backgroundTask;
    }

    /// <summary>
    /// Cancels the runner and waits for currently tracked tasks to complete.
    /// </summary>
    /// <returns>A task that completes when shutdown has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _stoppingSource.Cancel();

        var runningTasks = _runningTasks.Values.ToArray();

        if (runningTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(runningTasks);
            }
            catch
            {
                // Exceptions are observed and logged by task continuations.
            }
        }

        _stoppingSource.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ObserveTaskCompletion(Task task, int taskId, CancellationTokenSource linkedSource)
    {
        _ = task.ContinueWith(
            static (completedTask, state) =>
            {
                var (runningTasks, logger, currentTaskId, currentLinkedSource) =
                    ((ConcurrentDictionary<int, Task>, ILogger<BackgroundTaskRunner>, int, CancellationTokenSource))state!;

                runningTasks.TryRemove(currentTaskId, out _);

                try
                {
                    if (completedTask.Exception is not null)
                    {
                        logger.LogError(completedTask.Exception.Flatten(), "A background task failed.");
                    }
                }
                finally
                {
                    currentLinkedSource.Dispose();
                }
            },
            (_runningTasks, _logger, taskId, linkedSource),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
