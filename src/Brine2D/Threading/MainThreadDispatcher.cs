using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Brine2D.Threading;

/// <summary>
/// Simple cross-platform dispatcher for marshaling GPU operations to the main thread.
/// Uses only standard .NET primitives (ConcurrentQueue, Thread) for maximum compatibility.
/// </summary>
public class MainThreadDispatcher : IMainThreadDispatcher
{
    private readonly ILogger<MainThreadDispatcher> _logger;
    private Thread? _mainThread;
    private readonly ConcurrentQueue<WorkItem> _queue = new();
    
    public MainThreadDispatcher(ILogger<MainThreadDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public bool IsMainThread
    {
        get
        {
            // Lazy initialize main thread on first access
            if (_mainThread == null)
            {
                Interlocked.CompareExchange(ref _mainThread, Thread.CurrentThread, null);
                _logger.LogInformation("Main game thread initialized: Thread {ThreadId}", 
                    _mainThread.ManagedThreadId);
            }
            
            return Thread.CurrentThread == _mainThread;
        }
    }
    
    public void RunOnMainThread(Action work, bool waitForCompletion = false)
    {
        if (work == null) throw new ArgumentNullException(nameof(work));
        
        // Already on main thread - execute immediately
        if (IsMainThread)
        {
            work();
            return;
        }
        
        // Queue for main thread execution
        var item = new WorkItem 
        { 
            Work = work,
            CompletionSource = waitForCompletion ? new TaskCompletionSource<bool>() : null
        };
        
        _queue.Enqueue(item);
        
        // Block if requested
        if (waitForCompletion)
        {
            item.CompletionSource!.Task.Wait();
        }
    }
    
    public void ProcessQueue()
    {
        if (!IsMainThread)
        {
            _logger.LogWarning("ProcessQueue called from non-main thread");
            return;
        }
        
        // Process all pending work without blocking
        while (_queue.TryDequeue(out var item))
        {
            try
            {
                item.Work();
                item.CompletionSource?.SetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Main thread work item failed");
                item.CompletionSource?.SetException(ex);
            }
        }
    }
    
    private class WorkItem
    {
        public required Action Work { get; init; }
        public TaskCompletionSource<bool>? CompletionSource { get; init; }
    }
}