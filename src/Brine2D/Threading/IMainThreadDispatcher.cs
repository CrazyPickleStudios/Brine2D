namespace Brine2D.Threading;

/// <summary>
/// Minimal cross-platform interface for marshaling work to the main game thread.
/// Required for GPU operations which must run on the main thread due to platform constraints.
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// Queues work to run on the main thread.
    /// If already on main thread, executes immediately.
    /// </summary>
    /// <param name="work">The work to execute on the main thread</param>
    /// <param name="waitForCompletion">If true, blocks until work completes</param>
    void RunOnMainThread(Action work, bool waitForCompletion = false);
    
    /// <summary>
    /// Processes all queued main thread work. Called by GameLoop every frame.
    /// </summary>
    void ProcessQueue();
    
    /// <summary>
    /// Returns true if called from the main game thread.
    /// </summary>
    bool IsMainThread { get; }
}