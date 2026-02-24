namespace Brine2D.ECS.Query;

/// <summary>
/// Builder for creating cached entity queries with filtering criteria.
/// </summary>
public class CachedEntityQueryBuilder<T1> where T1 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions? _options;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;

    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world;
        _options = options; // Store options to pass to query!
    }

    /// <summary>
    /// Requires entities to have the specified tag.
    /// </summary>
    public CachedEntityQueryBuilder<T1> WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Adds a custom filter predicate.
    /// </summary>
    public CachedEntityQueryBuilder<T1> Where(Func<Entity, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// Includes both active and inactive entities.
    /// </summary>
    public CachedEntityQueryBuilder<T1> IncludeInactive()
    {
        _onlyActive = false;
        return this;
    }

    /// <summary>
    /// Builds and returns the cached query.
    /// </summary>
    public CachedEntityQuery<T1> Build()
    {
        return new CachedEntityQuery<T1>(
            _world, 
            _tags, 
            _predicates, 
            _onlyActive,
            _options); // Pass options!
    }
}

/// <summary>
/// Builder for creating cached entity queries with two components.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2> 
    where T1 : Component 
    where T2 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions? _options;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;

    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world;
        _options = options;
    }

    public CachedEntityQueryBuilder<T1, T2> WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public CachedEntityQueryBuilder<T1, T2> Where(Func<Entity, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public CachedEntityQueryBuilder<T1, T2> IncludeInactive()
    {
        _onlyActive = false;
        return this;
    }

    public CachedEntityQuery<T1, T2> Build()
    {
        return new CachedEntityQuery<T1, T2>(
            _world,
            _tags,
            _predicates,
            _onlyActive,
            _options);
    }
}

/// <summary>
/// Builder for creating cached entity queries with three components.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2, T3> 
    where T1 : Component 
    where T2 : Component 
    where T3 : Component
{
    private readonly IEntityWorld _world;
    private readonly ECSOptions? _options;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;

    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
    {
        _world = world;
        _options = options;
    }

    public CachedEntityQueryBuilder<T1, T2, T3> WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public CachedEntityQueryBuilder<T1, T2, T3> Where(Func<Entity, bool> predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    public CachedEntityQueryBuilder<T1, T2, T3> IncludeInactive()
    {
        _onlyActive = false;
        return this;
    }

    public CachedEntityQuery<T1, T2, T3> Build()
    {
        return new CachedEntityQuery<T1, T2, T3>(
            _world,
            _tags,
            _predicates,
            _onlyActive,
            _options);
    }
}