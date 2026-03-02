namespace Brine2D.Threading;

/// <summary>
/// Minimal cross-platform interface for marshaling work to the main game thread.
/// Required for GPU operations which must run on the main thread due to platform constraints.
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// Queues work to run on the main thread, or executes it immediately if already on
    /// the main thread, before the game loop starts, or after it has shut down.
    /// </summary>
    /// <param name="work">The work to execute on the main thread.</param>
    /// <param name="waitForCompletion">
    /// If <see langword="true"/>, blocks the calling thread until the work completes.
    /// Prefer <see cref="RunOnMainThreadAsync"/> for cancellable async callers.
    /// </param>
    void RunOnMainThread(Action work, bool waitForCompletion = false);

    /// <summary>
    /// Queues work to run on the main thread and returns a <see cref="Task"/> that completes
    /// when the work finishes. Executes inline if already on the main thread, before the
    /// game loop starts, or after <see cref="SignalShutdown"/> has been called.
    /// </summary>
    /// <remarks>
    /// Cancellation only abandons the <em>wait</em> — the work item remains in the queue
    /// and will still execute on the next <see cref="ProcessQueue"/> drain. Do not pass a
    /// cancellation token when the work must always run (e.g., final state swaps).
    /// </remarks>
    Task RunOnMainThreadAsync(Action work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all queued main thread work. Called by <see cref="GameLoop"/> every frame.
    /// Also registers the calling thread as the main thread on the first invocation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called from any thread other than the registered main thread.
    /// </exception>
    void ProcessQueue();

    /// <summary>
    /// Signals that the game loop has exited. After this call, <see cref="RunOnMainThreadAsync"/>
    /// and <see cref="RunOnMainThread"/> execute inline on the calling thread rather than
    /// enqueuing, preventing post-shutdown deadlocks in background cleanup continuations.
    /// Should only be called by the engine's game host infrastructure.
    /// </summary>
    void SignalShutdown();

    /// <summary>
    /// Returns <see langword="true"/> if called from the main game thread.
    /// Always returns <see langword="false"/> until <see cref="ProcessQueue"/> has been
    /// called at least once.
    /// </summary>
    bool IsMainThread { get; }
}