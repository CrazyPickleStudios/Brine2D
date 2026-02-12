namespace Brine2D.ECS.Query;

/// <summary>
/// Internal interface for cached queries to receive invalidation notifications.
/// </summary>
internal interface ICachedQuery
{
    void Invalidate();
}