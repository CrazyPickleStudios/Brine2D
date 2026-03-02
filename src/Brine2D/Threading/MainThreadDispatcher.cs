using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Brine2D.Threading;

/// <summary>
/// Simple cross-platform dispatcher for marshaling GPU operations to the main thread.
/// Uses only standard .NET primitives (ConcurrentQueue, Thread) for maximum compatibility.
/// </summary>
internal sealed class MainThreadDispatcher : IMainThreadDispatcher
{
    private readonly ILogger<MainThreadDispatcher> _logger;
    // volatile ensures reads on background threads always see the value written by
    // Interlocked.CompareExchange on the game thread, without the JIT caching a stale null.
    private volatile Thread? _mainThread;
    // volatile ensures background threads immediately see the shutdown signal written by
    // the game thread's finally block, without waiting for a memory fence.
    private volatile bool _shuttingDown;
    private readonly ConcurrentQueue<WorkItem> _queue = new();

    public MainThreadDispatcher(ILogger<MainThreadDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Pure read — returns false for all threads until ProcessQueue runs on the game thread.
    // This is intentional: nothing should execute as "main thread" before the game loop
    // has started draining the queue.
    public bool IsMainThread => _mainThread != null && Thread.CurrentThread == _mainThread;

    /// <inheritdoc/>
    public void SignalShutdown()
    {
        _shuttingDown = true;
        _logger.LogDebug("Main thread dispatcher shutdown signaled — queued work will execute inline");
    }

    public void RunOnMainThread(Action work, bool waitForCompletion = false)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Execute inline when:
        //   (a) already on the registered main thread, OR
        //   (b) the game loop has not started yet (_mainThread == null): no drainer exists yet,
        //       enqueuing would deadlock if waitForCompletion == true or if the caller awaits.
        //   (c) the game loop has shut down (_shuttingDown): the drainer is gone, same risk.
        if (IsMainThread || _mainThread == null || _shuttingDown)
        {
            work();
            return;
        }

        var item = new WorkItem
        {
            Work = work,
            CompletionSource = waitForCompletion ? new TaskCompletionSource() : null
        };

        _queue.Enqueue(item);

        if (waitForCompletion)
        {
            item.CompletionSource!.Task.GetAwaiter().GetResult();
        }
    }

    public Task RunOnMainThreadAsync(Action work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Execute inline when on the main thread, before the loop starts, or after it shuts down.
        // See RunOnMainThread for the full rationale on each condition.
        if (IsMainThread || _mainThread == null || _shuttingDown)
        {
            work();
            return Task.CompletedTask;
        }

        var item = new WorkItem
        {
            Work = work,
            CompletionSource = new TaskCompletionSource()
        };

        _queue.Enqueue(item);

        return cancellationToken.CanBeCanceled
            ? item.CompletionSource.Task.WaitAsync(cancellationToken)
            : item.CompletionSource.Task;
    }

    public void ProcessQueue()
    {
        // Lazily capture the game thread on first drain — the only safe registration point.
        // Registering here guarantees it is always the game loop thread, never a background
        // thread that happens to call IsMainThread or RunOnMainThread early.
        if (_mainThread == null)
        {
            var previous = Interlocked.CompareExchange(ref _mainThread, Thread.CurrentThread, null);
            if (previous == null)
                _logger.LogInformation("Main game thread registered: Thread {ThreadId}",
                    _mainThread!.ManagedThreadId);
        }

        // A wrong-thread call is a programming error: only GameLoop should call ProcessQueue.
        // Throwing rather than silently returning makes the contract violation immediately
        // visible instead of silently dropping all queued work (e.g., scene state swaps).
        if (!IsMainThread)
            throw new InvalidOperationException(
                $"ProcessQueue must be called from the registered main thread " +
                $"(thread {_mainThread!.ManagedThreadId}), " +
                $"but was called from thread {Thread.CurrentThread.ManagedThreadId}. " +
                "Only GameLoop should call this method.");

        while (_queue.TryDequeue(out var item))
        {
            try
            {
                item.Work();
                item.CompletionSource?.SetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Main thread work item failed");
                item.CompletionSource?.SetException(ex);
            }
        }
    }

    private sealed class WorkItem
    {
        public required Action Work { get; init; }
        public TaskCompletionSource? CompletionSource { get; init; }
    }
}