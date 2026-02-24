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
}