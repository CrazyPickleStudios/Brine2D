namespace Brine2D.ECS;

/// <summary>
/// Queue for deferring operations until safe to apply.
/// Used internally by EntityWorld to batch structural changes.
/// </summary>
internal class DeferredOperationQueue<T>
{
    private readonly List<T> _queue = new();
    private readonly Action<T> _applyOperation;

    public DeferredOperationQueue(Action<T> applyOperation)
    {
        _applyOperation = applyOperation ?? throw new ArgumentNullException(nameof(applyOperation));
    }

    public bool HasPending => _queue.Count > 0;

    /// <summary>
    /// Checks if an item is already queued.
    /// </summary>
    public bool Contains(T item) => _queue.Contains(item);

    public void Enqueue(T item)
    {
        _queue.Add(item);
    }

    public void ProcessAll()
    {
        if (_queue.Count == 0) return;

        foreach (var item in _queue)
        {
            _applyOperation(item);
        }

        _queue.Clear();
    }

    public void Clear()
    {
        _queue.Clear();
    }
}