// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using g0v0.Server.Common.Database.Models;
using g0v0.Server.Common.Database.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace g0v0.Server.Common.Scoring;

/// <summary>
/// A reusable, independently owned score queue with durable recovery and scoped processing callbacks.
/// </summary>
/// <param name="scopes">
/// Creates independent database scopes for background work.
/// </param>
/// <param name="logger">
/// The worker logger.
/// </param>
/// <param name="scoreType">
/// The client source type owned by this queue instance.
/// </param>
/// <param name="process">
/// The client processing callback, invoked in a fresh dependency injection scope.
/// </param>
/// <param name="canProcess">
/// An optional readiness predicate, such as requiring a completed replay upload.
/// </param>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This service implements a durable score queue.")]
public sealed class ScoreProcessingQueue(
    IServiceScopeFactory scopes,
    ILogger<ScoreProcessingQueue> logger,
    string scoreType,
    Func<IServiceProvider, Score, CancellationToken, Task> process,
    Func<Score, bool>? canProcess = null) : BackgroundService
{
    private readonly SemaphoreSlim _pending = new(0, 1);

    /// <summary>
    /// Wakes the worker after a submission has been persisted.
    /// </summary>
    public void NotifyPendingScore()
    {
        if (_pending.CurrentCount != 0)
        {
            // Another request has already signalled the worker.
            return;
        }

        try
        {
            _pending.Release();
        }
        catch (SemaphoreFullException)
        {
            // A racing request released the semaphore between the check and the release.
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();
        _pending.Dispose();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                long cursor = 0;
                while (!stoppingToken.IsCancellationRequested)
                {
                    await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                    IReadOnlyList<Score> batch = await scope.ServiceProvider.GetRequiredService<IScoreRepository>().GetUnprocessedScoresAsync(cursor, 100, scoreType);
                    if (batch.Count == 0)
                    {
                        break;
                    }

                    foreach (Score score in batch)
                    {
                        cursor = score.Id;
                        if (canProcess != null && !canProcess(score))
                        {
                            continue;
                        }

                        try
                        {
                            await using AsyncServiceScope itemScope = scopes.CreateAsyncScope();
                            await process(itemScope.ServiceProvider, score, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            logger.LogWarning(exception, "{ScoreType} score {ScoreId} remains queued for retry", scoreType, score.Id);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to read the {ScoreType} score queue", scoreType);
            }

            try
            {
                await _pending.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}