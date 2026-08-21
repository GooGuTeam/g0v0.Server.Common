// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.

using g0v0.Server.Common.Extensions;
using g0v0.Server.Common.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace g0v0.Server.Common.Tests.Threading;

[TestFixture]
public class BackgroundTaskRunnerTests
{
    [Test]
    public async Task RunAsync_WithAction_ShouldExecuteImmediately()
    {
        await using BackgroundTaskRunner runner = this.CreateRunner();
        TaskCompletionSource completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = runner.RunAsync(completionSource.SetResult);

        await Task.WhenAny(task, completionSource.Task);

        Assert.That(completionSource.Task.IsCompleted, Is.True);
        await task;
    }

    [Test]
    public async Task RunAsync_WithCancellationToken_ShouldPassCancellationToTask()
    {
        await using BackgroundTaskRunner runner = this.CreateRunner();
        using CancellationTokenSource cancellationTokenSource = new();
        TaskCompletionSource observedCancellation = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = runner.RunAsync(
            async cancellationToken =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    observedCancellation.SetResult();
                    throw;
                }
            },
            cancellationTokenSource.Token);

        await cancellationTokenSource.CancelAsync();

        await Task.WhenAny(task, observedCancellation.Task);

        Assert.That(observedCancellation.Task.IsCompleted, Is.True);
        Assert.That(async () => await task, Throws.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task DisposeAsync_WithRunningTask_ShouldCancelTrackedTask()
    {
        BackgroundTaskRunner runner = this.CreateRunner();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = runner.RunAsync(async cancellationToken =>
        {
            started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled.SetResult();
                throw;
            }
        });

        await started.Task;
        await runner.DisposeAsync();

        Assert.That(cancelled.Task.IsCompleted, Is.True);
        Assert.That(async () => await task, Throws.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task AddBackgroundTaskRunner_FromServiceCollection_ShouldResolveSameSingletonInstance()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddBackgroundTaskRunner();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBackgroundTaskRunner interfaceInstance = serviceProvider.GetRequiredService<IBackgroundTaskRunner>();
        BackgroundTaskRunner implementationInstance = serviceProvider.GetRequiredService<BackgroundTaskRunner>();

        Assert.That(interfaceInstance, Is.SameAs(implementationInstance));
    }

    private BackgroundTaskRunner CreateRunner()
    {
        return new BackgroundTaskRunner(
            NullLogger<BackgroundTaskRunner>.Instance,
            new ServiceCollection().BuildServiceProvider());
    }
}