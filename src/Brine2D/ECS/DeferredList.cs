namespace Brine2D.ECS;

/// <summary>
/// A list that defers add/remove operations until ProcessChanges() is called.
/// Safe for queueing during iteration.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
internal class DeferredList<T>
{
    private readonly List<T> _items = new();
    private readonly List<T> _itemsToAdd = new();
    private readonly List<T> _itemsToRemove = new();

    /// <summary>
    /// Gets the current count (excluding pending changes).
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets whether there are pending changes.
    /// </summary>
    public bool HasPendingChanges => _itemsToAdd.Count > 0 || _itemsToRemove.Count > 0;

    /// <summary>
    /// Queues an item to be added.
    /// </summary>
    public void Add(T item)
    {
        _itemsToAdd.Add(item);
    }

    /// <summary>
    /// Queues an item to be removed.
    /// </summary>
    public void Remove(T item)
    {
        _itemsToRemove.Add(item);
    }

    /// <summary>
    /// Checks if an item exists in the current list (not including pending adds).
    /// </summary>
    public bool Contains(T item)
    {
        return _items.Contains(item);
    }

    /// <summary>
    /// Checks if an item is queued for removal.
    /// </summary>
    public bool IsQueuedForRemoval(T item)
    {
        return _itemsToRemove.Contains(item);
    }

    /// <summary>
    /// Processes all queued changes (adds then removes).
    /// </summary>
    public void ProcessChanges()
    {
        // Apply adds first
        if (_itemsToAdd.Count > 0)
        {
            _items.AddRange(_itemsToAdd);
            _itemsToAdd.Clear();
        }

        // Then removes
        if (_itemsToRemove.Count > 0)
        {
            foreach (var item in _itemsToRemove)
            {
                _items.Remove(item);
            }
            _itemsToRemove.Clear();
        }
    }

    /// <summary>
    /// Processes pending additions with a callback for each item.
    /// Useful when you need to perform additional actions during addition.
    /// </summary>
    /// <param name="onAdd">Callback invoked for each item being added.</param>
    public void ProcessAdds(Action<T> onAdd)
    {
        if (_itemsToAdd.Count == 0) return;

        foreach (var item in _itemsToAdd)
        {
            _items.Add(item);
            onAdd?.Invoke(item);
        }
        _itemsToAdd.Clear();
    }

    /// <summary>
    /// Processes pending removals with a callback for each item.
    /// Useful when you need to perform additional actions during removal.
    /// </summary>
    /// <param name="onRemove">Callback invoked for each item being removed.</param>
    public void ProcessRemovals(Action<T> onRemove)
    {
        if (_itemsToRemove.Count == 0) return;

        foreach (var item in _itemsToRemove)
        {
            if (_items.Remove(item))
            {
                onRemove?.Invoke(item);
            }
        }
        _itemsToRemove.Clear();
    }

    /// <summary>
    /// Gets an enumerator for safe iteration (doesn't include pending changes).
    /// </summary>
    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Clears all items and pending changes.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _itemsToAdd.Clear();
        _itemsToRemove.Clear();
    }

    /// <summary>
    /// Gets a read-only view of the current items (for exposing to public API).
    /// </summary>
    public IReadOnlyList<T> AsReadOnly() => _items.AsReadOnly();
}