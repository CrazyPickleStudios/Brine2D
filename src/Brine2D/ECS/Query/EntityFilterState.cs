namespace Brine2D.ECS.Query;

/// <summary>
/// Bundles the filter lists shared between <see cref="EntityQuery"/> and
/// <see cref="CachedEntityQueryBase"/> so matching logic is written once.
/// Lightweight struct wrapping existing list references; zero allocation.
/// </summary>
internal readonly struct EntityFilterState(
    List<Type> withoutComponents,
    List<Type> withBehaviors,
    List<Type> withoutBehaviors,
    List<string> withAllTags,
    List<string> withoutTags,
    List<string> withAnyTags)
{
    internal bool Matches(Entity entity)
    {
        for (int i = 0; i < withoutComponents.Count; i++)
            if (entity.HasComponent(withoutComponents[i])) return false;

        for (int i = 0; i < withBehaviors.Count; i++)
            if (!entity.HasBehavior(withBehaviors[i])) return false;

        for (int i = 0; i < withoutBehaviors.Count; i++)
            if (entity.HasBehavior(withoutBehaviors[i])) return false;

        for (int i = 0; i < withAllTags.Count; i++)
            if (!entity.Tags.Contains(withAllTags[i])) return false;

        for (int i = 0; i < withoutTags.Count; i++)
            if (entity.Tags.Contains(withoutTags[i])) return false;

        if (withAnyTags.Count > 0)
        {
            bool found = false;
            for (int i = 0; i < withAnyTags.Count; i++)
            {
                if (entity.Tags.Contains(withAnyTags[i]))
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }

        return true;
    }
}