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
        await using var runner = this.CreateRunner();
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task task = runner.RunAsync(() => completionSource.SetResult());

        await Task.WhenAny(task, completionSource.Task);

        Assert.That(completionSource.Task.IsCompleted, Is.True);
        await task;
    }

    [Test]
    public async Task RunAsync_WithCancellationToken_ShouldPassCancellationToTask()
    {
        await using var runner = this.CreateRunner();
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedCancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

        cancellationTokenSource.Cancel();

        await Task.WhenAny(task, observedCancellation.Task);

        Assert.That(observedCancellation.Task.IsCompleted, Is.True);
        Assert.That(async () => await task, Throws.TypeOf<TaskCanceledException>());
    }

    [Test]
    public async Task DisposeAsync_WithRunningTask_ShouldCancelTrackedTask()
    {
        var runner = this.CreateRunner();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundTaskRunner();

        await using var serviceProvider = services.BuildServiceProvider();
        var interfaceInstance = serviceProvider.GetRequiredService<IBackgroundTaskRunner>();
        var implementationInstance = serviceProvider.GetRequiredService<BackgroundTaskRunner>();

        Assert.That(interfaceInstance, Is.SameAs(implementationInstance));
    }

    private BackgroundTaskRunner CreateRunner()
    {
        return new BackgroundTaskRunner(
            NullLogger<BackgroundTaskRunner>.Instance,
            new ServiceCollection().BuildServiceProvider());
    }
}