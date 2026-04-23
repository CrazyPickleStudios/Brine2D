namespace Brine2D.ECS;

/// <summary>
///     Interface for component pools (type-erased for storage in dictionary).
/// </summary>
internal interface IComponentPool
{
    Type ComponentType { get; }

    int Count { get; }

    void Add(long entityId, Component component);

    bool Contains(long entityId);

    Component? Get(long entityId);

    (long[] EntityIds, int Length) GetEntityIdSnapshot();

    bool Remove(long entityId);

    void ReturnEntityIdSnapshot(long[] snapshot);
}