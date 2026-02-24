namespace Brine2D.ECS;

/// <summary>
/// A list that defers add/remove operations until ProcessChanges() is called.
/// Safe for queueing structural changes during iteration.
/// </summary>
/// <remarks>
/// Removal uses a double-buffer HashSet pattern:
/// - O(1) amortized queuing per removal
/// - O(n) single-pass application via RemoveAll (vs O(n×m) previously)
/// - New removals queued inside callbacks go to the next ProcessChanges call,
///   preserving deferred semantics and preventing infinite recursion
///
/// Contains() is O(1) via a HashSet mirror of the committed list.
/// </remarks>
/// <typeparam name="T">The type of items in the list.</typeparam>
internal class DeferredList<T>
{
    private readonly List<T> _items = new();
    private readonly HashSet<T> _itemSet = new(); // O(1) mirror of _items for Contains()
    private readonly List<T> _itemsToAdd = new();

    // Double-buffer: _pending accepts new removals; _processing is swapped in during apply
    private HashSet<T> _pending = new();
    private HashSet<T> _processing = new();

    /// <summary>Gets the current count (excluding pending changes).</summary>
    public int Count => _items.Count;

    /// <summary>Gets whether there are pending changes.</summary>
    public bool HasPendingChanges => _itemsToAdd.Count > 0 || _pending.Count > 0;

    /// <summary>Queues an item to be added.</summary>
    public void Add(T item) => _itemsToAdd.Add(item);

    /// <summary>Queues an item to be removed. O(1) amortized.</summary>
    public void Remove(T item) => _pending.Add(item);

    /// <summary>
    /// Checks if an item exists in the current committed list (not including pending adds). O(1).
    /// </summary>
    public bool Contains(T item) => _itemSet.Contains(item);

    /// <summary>Checks if an item is queued for removal. O(1).</summary>
    public bool IsQueuedForRemoval(T item) => _pending.Contains(item);

    /// <summary>
    /// Processes all queued changes: adds first, then removes.
    /// </summary>
    public void ProcessChanges()
    {
        if (_itemsToAdd.Count > 0)
        {
            foreach (var item in _itemsToAdd)
            {
                _items.Add(item);
                _itemSet.Add(item);
            }
            _itemsToAdd.Clear();
        }

        if (_pending.Count == 0) return;

        // Swap buffers; any new removals queued during RemoveAll go to _pending (now empty)
        (_pending, _processing) = (_processing, _pending);
        _items.RemoveAll(item =>
        {
            if (!_processing.Contains(item)) return false;
            _itemSet.Remove(item);
            return true;
        });
        _processing.Clear();
    }

    /// <summary>
    /// Processes pending additions with a callback for each item added.
    /// </summary>
    public void ProcessAdds(Action<T> onAdd)
    {
        if (_itemsToAdd.Count == 0) return;

        foreach (var item in _itemsToAdd)
        {
            _items.Add(item);
            _itemSet.Add(item);
            onAdd(item);
        }
        _itemsToAdd.Clear();
    }

    /// <summary>
    /// Processes pending removals with a callback for each item removed.
    /// New removals queued inside the callback are deferred to the next call.
    /// </summary>
    public void ProcessRemovals(Action<T> onRemove)
    {
        if (_pending.Count == 0) return;

        // Swap buffers so callbacks can safely queue new removals without affecting this pass
        (_pending, _processing) = (_processing, _pending);

        _items.RemoveAll(item =>
        {
            if (!_processing.Contains(item)) return false;
            _itemSet.Remove(item);
            onRemove(item);
            return true;
        });

        _processing.Clear();
    }

    /// <summary>
    /// Gets an enumerator for the current committed items (safe for read-only iteration).
    /// </summary>
    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    /// <summary>Gets a read-only view of the current committed items.</summary>
    public IReadOnlyList<T> AsReadOnly() => _items.AsReadOnly();

    /// <summary>Clears all items and pending changes.</summary>
    public void Clear()
    {
        _items.Clear();
        _itemSet.Clear();
        _itemsToAdd.Clear();
        _pending.Clear();
        _processing.Clear();
    }
}