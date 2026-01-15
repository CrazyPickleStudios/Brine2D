using Microsoft.Extensions.Logging;

namespace Brine2D.Core;

/// <summary>
/// Global event bus for publish/subscribe pattern.
/// Allows decoupled communication between systems and components.
/// Follows ASP.NET's event notification patterns.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus>? _logger;

    public EventBus(ILogger<EventBus>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);

        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Delegate>();
        }

        _subscribers[eventType].Add(handler);
        _logger?.LogDebug("Subscribed to event: {EventType}", eventType.Name);
    }

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);

        if (_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType].Remove(handler);
            _logger?.LogDebug("Unsubscribed from event: {EventType}", eventType.Name);
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    public void Publish<T>(T eventData) where T : class
    {
        var eventType = typeof(T);

        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            _logger?.LogTrace("Publishing event: {EventType} to {Count} subscribers", eventType.Name, handlers.Count);

            foreach (var handler in handlers.ToList())
            {
                try
                {
                    ((Action<T>)handler).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling event {EventType}", eventType.Name);
                }
            }
        }
    }

    /// <summary>
    /// Clears all subscribers for a specific event type.
    /// </summary>
    public void ClearSubscribers<T>() where T : class
    {
        var eventType = typeof(T);
        _subscribers.Remove(eventType);
    }

    /// <summary>
    /// Clears all subscribers.
    /// </summary>
    public void ClearAll()
    {
        _subscribers.Clear();
    }
}