namespace Brine2D.Events;

/// <summary>
/// Abstraction for the engine's pub/sub event bus.
/// Inject this interface (rather than the concrete <see cref="EventBus"/>) in scenes and
/// systems to keep them testable and decoupled from the implementation.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to events of type <typeparamref name="T"/> and returns a disposal token
    /// that unsubscribes automatically when disposed. Prefer this overload to avoid
    /// stale subscriptions when scenes are unloaded.
    /// </summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : class;

    /// <summary>
    /// Manually unsubscribes a handler. Prefer disposing the token returned by
    /// <see cref="Subscribe{T}"/> — it calls this method and is idempotent.
    /// </summary>
    void Unsubscribe<T>(Action<T> handler) where T : class;

    /// <summary>
    /// Publishes an event to all current subscribers of type <typeparamref name="T"/>.
    /// </summary>
    void Publish<T>(T eventData) where T : class;

    /// <summary>Clears all subscribers for a specific event type.</summary>
    void ClearSubscribers<T>() where T : class;

    /// <summary>Clears all subscribers for all event types.</summary>
    void ClearAll();
}