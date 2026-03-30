namespace Brine2D.ECS.Query;

/// <summary>
/// Abstract base for cached query builders. Holds shared filter state and provides
/// fluent methods that are identical across all arities. Subclasses only add
/// <c>Build()</c> with the correct return type.
/// </summary>
public abstract class CachedEntityQueryBuilderBase<TSelf>
    where TSelf : CachedEntityQueryBuilderBase<TSelf>
{
    private protected readonly IEntityWorld _world;
    private protected readonly ECSOptions? _options;
    private protected readonly List<string> _tags = new();
    private protected readonly List<string> _withoutTags = new();
    private protected readonly List<string> _withAnyTags = new();
    private protected readonly List<Func<Entity, bool>> _predicates = new();
    private protected readonly List<Type> _withoutTypes = new();
    private protected readonly List<Type> _withBehaviorTypes = new();
    private protected readonly List<Type> _withoutBehaviorTypes = new();
    private protected bool _onlyActive = true;
    private protected bool _onlyEnabled;

    internal CachedEntityQueryBuilderBase(IEntityWorld world, ECSOptions? options)
    {
        _world = world;
        _options = options;
    }

    /// <summary>
    /// Requires entities to have the specified tag.
    /// </summary>
    public TSelf WithTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _tags.Add(tag);
        return (TSelf)this;
    }

    /// <summary>
    /// Requires entities to have ALL of the specified tags.
    /// </summary>
    public TSelf WithAllTags(params string[] tags)
    {
        foreach (var tag in tags)
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _tags.AddRange(tags);
        return (TSelf)this;
    }

    /// <summary>
    /// Requires entities to NOT have the specified tag.
    /// </summary>
    public TSelf WithoutTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withoutTags.Add(tag);
        return (TSelf)this;
    }

    /// <summary>
    /// Requires entities to have AT LEAST ONE of the specified tags.
    /// </summary>
    public TSelf WithAnyTag(params string[] tags)
    {
        foreach (var tag in tags)
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _withAnyTags.AddRange(tags);
        return (TSelf)this;
    }

    /// <summary>
    /// Excludes entities that have the specified component.
    /// The query is automatically invalidated when the excluded component type
    /// is added to or removed from any entity.
    /// </summary>
    public TSelf Without<TExclude>() where TExclude : Component
    {
        _withoutTypes.Add(typeof(TExclude));
        return (TSelf)this;
    }

    /// <summary>
    /// Requires entities to have the specified behavior.
    /// The query is automatically invalidated when behaviors are added or removed.
    /// </summary>
    public TSelf WithBehavior<TBehavior>() where TBehavior : Behavior
    {
        _withBehaviorTypes.Add(typeof(TBehavior));
        return (TSelf)this;
    }

    /// <summary>
    /// Excludes entities that have the specified behavior.
    /// The query is automatically invalidated when behaviors are added or removed.
    /// </summary>
    public TSelf WithoutBehavior<TBehavior>() where TBehavior : Behavior
    {
        _withoutBehaviorTypes.Add(typeof(TBehavior));
        return (TSelf)this;
    }

    /// <summary>
    /// Adds a custom filter predicate.
    /// </summary>
    public TSelf Where(Func<Entity, bool> predicate)
    {
        _predicates.Add(predicate);
        return (TSelf)this;
    }

    /// <summary>
    /// Includes both active and inactive entities.
    /// </summary>
    public TSelf IncludeInactive()
    {
        _onlyActive = false;
        return (TSelf)this;
    }

    /// <summary>
    /// Filters to entities where all required components have <see cref="Component.IsEnabled"/>
    /// set to <see langword="true"/>. Components are already pre-resolved during cache rebuild,
    /// so this check adds negligible overhead.
    /// </summary>
    /// <remarks>
    /// This is distinct from active-state filtering: <c>IncludeInactive()</c> controls
    /// <see cref="Entity.IsActive"/> (entity-level), while <c>OnlyEnabled()</c> filters by
    /// <see cref="Component.IsEnabled"/> (component-level).
    /// </remarks>
    public TSelf OnlyEnabled()
    {
        _onlyEnabled = true;
        return (TSelf)this;
    }
}

/// <summary>
/// Builder for creating cached entity queries with filtering criteria.
/// </summary>
public class CachedEntityQueryBuilder<T1>
    : CachedEntityQueryBuilderBase<CachedEntityQueryBuilder<T1>>
    where T1 : Component
{
    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
        : base(world, options) { }

    /// <summary>
    /// Builds and returns the cached query.
    /// </summary>
    public CachedEntityQuery<T1> Build()
        => new(_world, _tags, _predicates, _withoutTypes, _withBehaviorTypes, _withoutBehaviorTypes, _withoutTags, _withAnyTags, _onlyActive, _onlyEnabled, _options);
}

/// <summary>
/// Builder for creating cached entity queries with two components.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2>
    : CachedEntityQueryBuilderBase<CachedEntityQueryBuilder<T1, T2>>
    where T1 : Component
    where T2 : Component
{
    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
        : base(world, options) { }

    public CachedEntityQuery<T1, T2> Build()
        => new(_world, _tags, _predicates, _withoutTypes, _withBehaviorTypes, _withoutBehaviorTypes, _withoutTags, _withAnyTags, _onlyActive, _onlyEnabled, _options);
}

/// <summary>
/// Builder for creating cached entity queries with three components.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2, T3>
    : CachedEntityQueryBuilderBase<CachedEntityQueryBuilder<T1, T2, T3>>
    where T1 : Component
    where T2 : Component
    where T3 : Component
{
    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
        : base(world, options) { }

    public CachedEntityQuery<T1, T2, T3> Build()
        => new(_world, _tags, _predicates, _withoutTypes, _withBehaviorTypes, _withoutBehaviorTypes, _withoutTags, _withAnyTags, _onlyActive, _onlyEnabled, _options);
}

/// <summary>
/// Builder for creating cached entity queries with four components.
/// </summary>
public class CachedEntityQueryBuilder<T1, T2, T3, T4>
    : CachedEntityQueryBuilderBase<CachedEntityQueryBuilder<T1, T2, T3, T4>>
    where T1 : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
{
    internal CachedEntityQueryBuilder(IEntityWorld world, ECSOptions? options = null)
        : base(world, options) { }

    public CachedEntityQuery<T1, T2, T3, T4> Build()
        => new(_world, _tags, _predicates, _withoutTypes, _withBehaviorTypes, _withoutBehaviorTypes, _withoutTags, _withAnyTags, _onlyActive, _onlyEnabled, _options);
}