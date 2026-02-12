namespace Brine2D.ECS.Query;

/// <summary>
/// Builder for creating cached queries with filters.
/// </summary>
public class CachedEntityQueryBuilder<T1> where T1 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;
    
    internal CachedEntityQueryBuilder(IEntityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
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
    /// Requires entities to match a custom predicate.
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
    /// Builds the cached query with the configured filters.
    /// </summary>
    public CachedEntityQuery<T1> Build()
    {
        return new CachedEntityQuery<T1>(_world, _tags, _predicates, _onlyActive);
    }
}

/// <summary>
/// Builder for creating cached queries with two component filters.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2> 
    where T1 : Component 
    where T2 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;
    
    internal CachedEntityQueryBuilder(IEntityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
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
        return new CachedEntityQuery<T1, T2>(_world, _tags, _predicates, _onlyActive);
    }
}

/// <summary>
/// Builder for creating cached queries with three component filters.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2, T3> 
    where T1 : Component 
    where T2 : Component
    where T3 : Component
{
    private readonly IEntityWorld _world;
    private readonly List<string> _tags = new();
    private readonly List<Func<Entity, bool>> _predicates = new();
    private bool _onlyActive = true;
    
    internal CachedEntityQueryBuilder(IEntityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
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
        return new CachedEntityQuery<T1, T2, T3>(_world, _tags, _predicates, _onlyActive);
    }
}