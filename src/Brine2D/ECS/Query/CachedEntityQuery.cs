namespace Brine2D.ECS.Query;

/// <summary>
/// A cached query that maintains a list of matching entities for performance.
/// Automatically invalidates when entities are added/removed or components change.
/// </summary>
public class CachedEntityQuery<T1> : ICachedQuery where T1 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    // Legacy constructor (backwards compatible)
    internal CachedEntityQuery(IEntityWorld world)
        : this(world, new List<string>(), new List<Func<Entity, bool>>(), true)
    {
    }

    // New constructor with filters
    internal CachedEntityQuery(
        IEntityWorld world, 
        List<string> tags, 
        List<Func<Entity, bool>> predicates,
        bool onlyActive)
    {
        _world = world;
        _tags = tags;
        _predicates = predicates;
        _onlyActive = onlyActive;
        
        // Register with EntityWorld for automatic invalidation
        if (world is EntityWorld entityWorld)
        {
            entityWorld.RegisterCachedQuery(this);
        }
    }

    /// <summary>
    /// Executes the query, using cached results if available.
    /// </summary>
    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            var query = _world.Entities.AsEnumerable();
            
            // Active filter
            if (_onlyActive)
            {
                query = query.Where(e => e.IsActive);
            }
            
            // Component filter
            query = query.Where(e => e.HasComponent<T1>());
            
            // Tag filters
            foreach (var tag in _tags)
            {
                query = query.Where(e => e.HasTag(tag));
            }
            
            // Custom predicates
            foreach (var predicate in _predicates)
            {
                query = query.Where(predicate);
            }
            
            _cachedResults = query.ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    /// <summary>
    /// Forces the cache to refresh on the next execution.
    /// Normally not needed - cache auto-invalidates when world changes.
    /// </summary>
    public void Invalidate()
    {
        _isDirty = true;
    }
    
    /// <summary>
    /// Gets the current cached count without re-querying.
    /// Returns 0 if cache is dirty.
    /// </summary>
    public int Count()
    {
        return _isDirty ? 0 : _cachedResults?.Count ?? 0;
    }
}

/// <summary>
/// Cached query for entities with two components.
/// </summary>
public class CachedEntityQuery<T1, T2> : ICachedQuery
    where T1 : Component 
    where T2 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    // Legacy constructor
    internal CachedEntityQuery(IEntityWorld world)
        : this(world, new List<string>(), new List<Func<Entity, bool>>(), true)
    {
    }

    // New constructor with filters
    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        bool onlyActive)
    {
        _world = world;
        _tags = tags;
        _predicates = predicates;
        _onlyActive = onlyActive;
        
        if (world is EntityWorld entityWorld)
        {
            entityWorld.RegisterCachedQuery(this);
        }
    }

    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            var query = _world.Entities.AsEnumerable();
            
            if (_onlyActive)
            {
                query = query.Where(e => e.IsActive);
            }
            
            query = query.Where(e => e.HasComponent<T1>() && e.HasComponent<T2>());
            
            foreach (var tag in _tags)
            {
                query = query.Where(e => e.HasTag(tag));
            }
            
            foreach (var predicate in _predicates)
            {
                query = query.Where(predicate);
            }
            
            _cachedResults = query.ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    public void Invalidate()
    {
        _isDirty = true;
    }
    
    public int Count()
    {
        return _isDirty ? 0 : _cachedResults?.Count ?? 0;
    }
}

/// <summary>
/// Cached query for entities with three components.
/// </summary>
public class CachedEntityQuery<T1, T2, T3> : ICachedQuery
    where T1 : Component 
    where T2 : Component 
    where T3 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags;
    private readonly List<Func<Entity, bool>> _predicates;
    private readonly bool _onlyActive;
    private List<Entity>? _cachedResults;
    private bool _isDirty = true;

    // Legacy constructor
    internal CachedEntityQuery(IEntityWorld world)
        : this(world, new List<string>(), new List<Func<Entity, bool>>(), true)
    {
    }

    // New constructor with filters
    internal CachedEntityQuery(
        IEntityWorld world,
        List<string> tags,
        List<Func<Entity, bool>> predicates,
        bool onlyActive)
    {
        _world = world;
        _tags = tags;
        _predicates = predicates;
        _onlyActive = onlyActive;
        
        if (world is EntityWorld entityWorld)
        {
            entityWorld.RegisterCachedQuery(this);
        }
    }

    public IEnumerable<Entity> Execute()
    {
        if (_isDirty)
        {
            var query = _world.Entities.AsEnumerable();
            
            if (_onlyActive)
            {
                query = query.Where(e => e.IsActive);
            }
            
            query = query.Where(e => 
                e.HasComponent<T1>() && 
                e.HasComponent<T2>() && 
                e.HasComponent<T3>());
            
            foreach (var tag in _tags)
            {
                query = query.Where(e => e.HasTag(tag));
            }
            
            foreach (var predicate in _predicates)
            {
                query = query.Where(predicate);
            }
            
            _cachedResults = query.ToList();
            _isDirty = false;
        }

        return _cachedResults!;
    }

    public void Invalidate()
    {
        _isDirty = true;
    }
    
    public int Count()
    {
        return _isDirty ? 0 : _cachedResults?.Count ?? 0;
    }
}