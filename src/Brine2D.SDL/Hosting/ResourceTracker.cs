using System.Diagnostics;

namespace Brine2D.SDL.Hosting;

/// <summary>
///     Tracks disposable engine resources to aid in shutdown-time leak diagnostics.
///     Register lightweight wrappers (textures, sprite renderer, etc.).
///     In DEBUG builds, reports any still undisposed resources at host shutdown.
/// </summary>
internal sealed class ResourceTracker
{
    private readonly List<object> _resources = new();

    // Historical registrations (never decreases)
    public int Count => _resources.Count;

    // Current live (not yet disposed) tracked resources
    public int LiveCount => _resources.Count(r =>
        r is ITrackedResource tr ? !tr.IsDisposed :
        r is IDisposable);

    /// <summary>
    ///     Registers a resource for lifetime tracking. Intended for IDisposable objects
    ///     (optionally implementing <see cref="ITrackedResource" /> for richer diagnostics).
    /// </summary>
    internal void Register(object resource)
    {
        if (resource == null) return;
        _resources.Add(resource);
    }

    /// <summary>
    ///     Iterates all tracked resources (read-only traversal).
    /// </summary>
    internal void ForEach(Action<object> visitor)
    {
        if (visitor == null) return;
        for (int i = 0; i < _resources.Count; i++)
            visitor(_resources[i]);
    }

    /// <summary>
    ///     Verifies all tracked resources were disposed. DEBUG only; outputs leak info.
    /// </summary>
    [Conditional("DEBUG")]
    internal void VerifyAllDisposed()
    {
        int leaks = 0;
        for (int i = 0; i < _resources.Count; i++)
        {
            var r = _resources[i];
            if (r is ITrackedResource tracked)
            {
                if (!tracked.IsDisposed)
                {
                    leaks++;
                    Debug.WriteLine($"[Leak] Undisposed resource: {tracked.DebugName ?? r.GetType().Name}");
                }
            }
            else if (r is IDisposable)
            {
                leaks++;
                Debug.WriteLine($"[Leak?] Tracked IDisposable without state: {r.GetType().Name}");
            }
        }

        Debug.WriteLine(leaks > 0
            ? $"[Leak] {leaks} resource(s) were not disposed before shutdown."
            : "[Leak] All tracked resources disposed cleanly.");
    }
}

/// <summary>
///     Optional interface for tracked resources to expose disposal state & a debug name.
/// </summary>
internal interface ITrackedResource
{
    string? DebugName { get; }
    bool IsDisposed { get; }
}