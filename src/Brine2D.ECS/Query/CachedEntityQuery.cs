namespace Brine2D.ECS.Query;

/// <summary>
/// A cached query that maintains a list of matching entities for performance.
/// Automatically updates when entities are added/removed or components change.
/// </summary>
public class CachedEntityQuery<T1> where T1 : Component
{
    private readonly IEntityWorld _world;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    internal CachedEntityQuery(IEntityWorld world)
    {
        _world = world;

        // Subscribe to world events to invalidate cache
        _world.OnEntityCreated += OnEntityChanged;
        _world.OnEntityDestroyed += OnEntityChanged;
        _world.OnComponentAdded += OnComponentChanged;
        _world.OnComponentRemoved += OnComponentChanged;
    }

    /// <summary>
    /// Executes the query, using cached results if available.
    /// </summary>
    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            _cachedResults = _world.Entities
                .Where(e => e.HasComponent<T1>())
                .ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    /// <summary>
    /// Forces the cache to refresh on the next execution.
    /// </summary>
    public void Invalidate()
    {
        _isDirty = true;
    }

    private void OnEntityChanged(Entity entity)
    {
        _isDirty = true;
    }

    private void OnComponentChanged(Entity entity, Component component)
    {
        _isDirty = true;
    }
}

/// <summary>
/// Cached query for entities with two components.
/// </summary>
public class CachedEntityQuery<T1, T2> 
    where T1 : Component 
    where T2 : Component
{
    private readonly IEntityWorld _world;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    internal CachedEntityQuery(IEntityWorld world)
    {
        _world = world;

        _world.OnEntityCreated += OnEntityChanged;
        _world.OnEntityDestroyed += OnEntityChanged;
        _world.OnComponentAdded += OnComponentChanged;
        _world.OnComponentRemoved += OnComponentChanged;
    }

    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            _cachedResults = _world.Entities
                .Where(e => e.HasComponent<T1>() && e.HasComponent<T2>())
                .ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    public void Invalidate()
    {
        _isDirty = true;
    }

    private void OnEntityChanged(Entity entity)
    {
        _isDirty = true;
    }

    private void OnComponentChanged(Entity entity, Component component)
    {
        _isDirty = true;
    }
}

/// <summary>
/// Cached query for entities with three components.
/// </summary>
public class CachedEntityQuery<T1, T2, T3> 
    where T1 : Component 
    where T2 : Component 
    where T3 : Component
{
    private readonly IEntityWorld _world;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    internal CachedEntityQuery(IEntityWorld world)
    {
        _world = world;

        _world.OnEntityCreated += OnEntityChanged;
        _world.OnEntityDestroyed += OnEntityChanged;
        _world.OnComponentAdded += OnComponentChanged;
        _world.OnComponentRemoved += OnComponentChanged;
    }

    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            _cachedResults = _world.Entities
                .Where(e => e.HasComponent<T1>() && 
                           e.HasComponent<T2>() && 
                           e.HasComponent<T3>())
                .ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    public void Invalidate()
    {
        _isDirty = true;
    }

    private void OnEntityChanged(Entity entity)
    {
        _isDirty = true;
    }

    private void OnComponentChanged(Entity entity, Component component)
    {
        _isDirty = true;
    }
}