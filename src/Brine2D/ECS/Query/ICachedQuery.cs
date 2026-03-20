namespace Brine2D.ECS.Query;

/// <summary>
/// Internal interface for cached queries to receive invalidation notifications.
/// </summary>
internal interface ICachedQuery
{
    void Invalidate();

    /// <summary>
    /// The component types this query is interested in.
    /// Used for targeted invalidation; only queries that care about
    /// a changed component type will be invalidated.
    /// </summary>
    IReadOnlyCollection<Type> ComponentTypes { get; }

    /// <summary>
    /// The specific tags this query filters by.
    /// Used by <see cref="EntityWorld"/> to target tag-change invalidation
    /// to only the queries that actually filter on the changed tag.
    /// </summary>
    IReadOnlyCollection<string> TagFilters { get; }

    /// <summary>
    /// Whether this query filters by entity tags.
    /// Used by <see cref="EntityWorld"/> to skip tag invalidation entirely
    /// when no queries care about tags.
    /// </summary>
    bool HasTagFilters { get; }

    /// <summary>
    /// Whether this query filters by entity behaviors.
    /// Used by <see cref="EntityWorld"/> to target behavior-change invalidation.
    /// </summary>
    bool HasBehaviorFilters { get; }

    /// <summary>
    /// Whether this query filters by entity active state.
    /// Used by <see cref="EntityWorld"/> to skip invalidation for queries
    /// built with <c>IncludeInactive()</c> when only active state changes.
    /// </summary>
    bool FiltersActiveState { get; }

    /// <summary>
    /// Whether this query filters by component enabled state.
    /// Used by <see cref="EntityWorld"/> to skip invalidation for queries
    /// not built with <c>OnlyEnabled()</c> when only enabled state changes.
    /// </summary>
    bool FiltersEnabledState { get; }
}