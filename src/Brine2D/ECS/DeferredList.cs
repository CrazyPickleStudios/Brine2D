using System.Collections.ObjectModel;

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
/// Add() is idempotent: duplicate queuing is silently dropped to preserve the
/// _items/_itemSet mirror invariant (List allows duplicates, HashSet does not).
/// IsQueuedForRemoval() checks both the pending and active-processing buffers so
/// callers inside a removal callback cannot accidentally re-queue the same item.
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

    // Cached read-only wrapper; reflects live _items without re-allocating on each access.
    private ReadOnlyCollection<T>? _readOnlyView;

    /// <summary>Gets the current count (excluding pending changes).</summary>
    public int Count => _items.Count;

    /// <summary>Gets whether there are pending changes.</summary>
    public bool HasPendingChanges => _itemsToAdd.Count > 0 || _pending.Count > 0;

    /// <summary>
    /// Queues an item to be added. No-op if the item is already committed or already queued,
    /// preventing duplicate entries that would break the _items/_itemSet mirror invariant.
    /// The pending-add check is O(n) on _itemsToAdd; acceptable since per-frame spawn counts
    /// are small. If bulk spawning ever becomes a bottleneck, add a HashSet mirror of _itemsToAdd.
    /// </summary>
    public void Add(T item)
    {
        if (!_itemSet.Contains(item) && !_itemsToAdd.Contains(item))
            _itemsToAdd.Add(item);
    }

    /// <summary>Queues an item to be removed. O(1) amortized.</summary>
    public void Remove(T item) => _pending.Add(item);

    /// <summary>
    /// Checks if an item exists in the current committed list (not including pending adds). O(1).
    /// </summary>
    public bool Contains(T item) => _itemSet.Contains(item);

    /// <summary>
    /// Checks if an item is queued for removal or is actively being removed. O(1).
    /// Checks both the pending buffer and the active-processing buffer so callers inside
    /// a removal callback (e.g., Entity.OnDestroy) cannot accidentally re-queue the same item.
    /// </summary>
    public bool IsQueuedForRemoval(T item) => _pending.Contains(item) || _processing.Contains(item);

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

    /// <summary>
    /// Gets a read-only view of the current committed items.
    /// The wrapper is created once and cached; it reflects all live changes to the list.
    /// </summary>
    public IReadOnlyList<T> AsReadOnly() => _readOnlyView ??= _items.AsReadOnly();

    /// <summary>Clears all items and pending changes.</summary>
    public void Clear()
    {
        _items.Clear();
        _itemSet.Clear();
        _itemsToAdd.Clear();
        _pending.Clear();
        _processing.Clear();
        _readOnlyView = null; // Invalidate; next AsReadOnly() creates a fresh wrapper
    }
}