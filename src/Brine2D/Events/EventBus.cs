using Microsoft.Extensions.Logging;

namespace Brine2D.Events;

/// <summary>
/// Singleton event bus for publish/subscribe messaging between systems and components.
/// Thread-safe: Subscribe, Unsubscribe, and ClearAll may be called from any thread.
/// Publish is allocation-free on the hot path; it reads a pre-committed handler array
/// without holding the lock during invocation.
/// </summary>
internal sealed class EventBus : IEventBus
{
    // Copy-on-write: each subscribe/unsubscribe replaces the array atomically under _lock.
    // Publish reads the current array reference under the lock, then invokes outside it —
    // no allocation in the common case, and handlers may subscribe/unsubscribe re-entrantly
    // without deadlocking.
    private readonly Dictionary<Type, Delegate[]> _subscribers = new();
    private readonly Lock _lock = new();
    private readonly ILogger<EventBus>? _logger;

    public EventBus(ILogger<EventBus>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to events of type <typeparamref name="T"/> and returns a disposal token
    /// that unsubscribes automatically when disposed. Prefer this overload to avoid
    /// stale subscriptions when scenes are unloaded:
    /// <code>
    /// // In OnEnter:
    /// _subscription = Game.EventBus.Subscribe&lt;EnemyDiedEvent&gt;(OnEnemyDied);
    ///
    /// // In OnExit:
    /// _subscription.Dispose();
    /// </code>
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        var type = typeof(T);
        lock (_lock)
        {
            _subscribers[type] = _subscribers.TryGetValue(type, out var existing)
                ? [..existing, handler]
                : [handler];
        }
        _logger?.LogDebug("Subscribed to event: {EventType}", type.Name);
        return new Subscription(() => Unsubscribe(handler));
    }

    /// <summary>
    /// Manually unsubscribes a handler. Prefer disposing the token returned by
    /// <see cref="Subscribe{T}"/> — it calls this method and is idempotent.
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        var type = typeof(T);
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(type, out var existing)) return;

            // Find and remove the first matching occurrence (matches List.Remove semantics).
            int idx = Array.IndexOf(existing, (Delegate)handler);
            if (idx < 0) return;

            if (existing.Length == 1)
            {
                _subscribers.Remove(type);
            }
            else
            {
                var updated = new Delegate[existing.Length - 1];
                existing.AsSpan(0, idx).CopyTo(updated);
                existing.AsSpan(idx + 1).CopyTo(updated.AsSpan(idx));
                _subscribers[type] = updated;
            }
        }
        _logger?.LogDebug("Unsubscribed from event: {EventType}", type.Name);
    }

    /// <summary>
    /// Publishes an event to all current subscribers of type <typeparamref name="T"/>.
    /// Exceptions thrown by individual handlers are caught and logged; remaining handlers
    /// still execute. Allocation-free on the hot path.
    /// </summary>
    public void Publish<T>(T eventData) where T : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // Read the committed array reference under the lock, then release it before invoking.
        // Handlers may call Subscribe/Unsubscribe without deadlocking because the lock is
        // not held during invocation.
        Delegate[]? handlers;
        lock (_lock)
            _subscribers.TryGetValue(typeof(T), out handlers);

        if (handlers is not { Length: > 0 }) return;

        _logger?.LogTrace("Publishing event: {EventType} to {Count} subscribers",
            typeof(T).Name, handlers.Length);

        foreach (var handler in handlers) // Iterating a pre-committed array: zero allocation
        {
            try
            {
                ((Action<T>)handler).Invoke(eventData);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling event {EventType}", typeof(T).Name);
            }
        }
    }

    /// <summary>Clears all subscribers for a specific event type.</summary>
    public void ClearSubscribers<T>() where T : class
    {
        lock (_lock)
            _subscribers.Remove(typeof(T));
    }

    /// <summary>Clears all subscribers for all event types.</summary>
    public void ClearAll()
    {
        lock (_lock)
            _subscribers.Clear();
    }

    /// <summary>
    /// Returned by <see cref="Subscribe{T}"/> — disposes the subscription idempotently.
    /// </summary>
    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            // Interlocked ensures exactly-once unsubscribe even if Dispose is called
            // concurrently from two threads.
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                unsubscribe();
        }
    }
}