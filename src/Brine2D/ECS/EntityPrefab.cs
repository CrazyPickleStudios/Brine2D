using System.Numerics;
using Brine2D.ECS.Components;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
/// A reusable entity template that can be instantiated multiple times.
/// Think of it like a "class" for entities.
/// </summary>
public class EntityPrefab
{
    /// <summary>
    /// Name of this prefab (e.g., "Player", "Enemy", "Coin").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tags to apply to instantiated entities.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Component configuration actions (called after component is added).
    /// </summary>
    private readonly List<Action<Entity>> _componentConfigurators = new();

    private readonly List<(EntityPrefab Prefab, Action<Entity>? Configure)> _childPrefabs = new();

    public EntityPrefab(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name;
    }

    /// <summary>
    /// Adds a tag to be applied to instantiated entities.
    /// </summary>
    /// <example>
    /// <code>
    /// var prefab = new EntityPrefab("Enemy")
    ///     .AddComponent&lt;TransformComponent&gt;()
    ///     .AddTag("Enemy")
    ///     .AddTag("Hostile");
    /// </code>
    /// </example>
    public EntityPrefab AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
            Tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Adds multiple tags to be applied to instantiated entities.
    /// </summary>
    /// <example>
    /// <code>
    /// var prefab = new EntityPrefab("Enemy")
    ///     .AddComponent&lt;TransformComponent&gt;()
    ///     .AddTags("Enemy", "Hostile");
    /// </code>
    /// </example>
    public EntityPrefab AddTags(params string[] tags)
    {
        foreach (var tag in tags)
            if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
                Tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Adds a component with a configuration action.
    /// The action is called after the component is added to configure its properties.
    /// </summary>
    /// <example>
    /// <code>
    /// prefab.AddComponent&lt;TransformComponent&gt;(t => t.Position = new Vector2(100, 200));
    /// </code>
    /// </example>
    public EntityPrefab AddComponent<T>(Action<T>? configure = null) where T : Component, new()
    {
        _componentConfigurators.Add(entity => entity.AddComponent<T>(configure));
        return this;
    }

    /// <summary>
    /// Adds a behavior of the specified type.
    /// The behavior is created with dependency injection when the prefab is instantiated.
    /// </summary>
    /// <param name="configure">
    /// Optional action invoked after the behavior is constructed but before
    /// <see cref="Behavior.OnAdded"/> is called, so configured values are
    /// available during attachment. Pass <see langword="null"/> to skip configuration.
    /// </param>
    /// <example>
    /// <code>
    /// prefab.AddBehavior&lt;EnemyAIBehavior&gt;(ai => ai.PatrolRadius = 200f);
    /// </code>
    /// </example>
    public EntityPrefab AddBehavior<T>(Action<T>? configure = null) where T : Behavior
    {
        _componentConfigurators.Add(entity =>
        {
            if (configure != null)
                entity.AddBehavior<T>(configure);
            else
                entity.AddBehavior<T>();
        });
        return this;
    }

    /// <summary>
    /// Adds a child prefab that will be instantiated and parented to the root entity
    /// each time this prefab is instantiated.
    /// </summary>
    /// <param name="childPrefab">The prefab to instantiate as a child.</param>
    /// <param name="configure">
    /// Optional action invoked on the instantiated child entity after all its components
    /// and behaviors have been applied, allowing per-instantiation overrides (e.g., local offset).
    /// </param>
    /// <example>
    /// <code>
    /// var enemyPrefab = new EntityPrefab("Enemy")
    ///     .AddComponent&lt;TransformComponent&gt;()
    ///     .AddChildPrefab(shadowPrefab)
    ///     .AddChildPrefab(weaponPrefab, child =&gt; child.GetComponent&lt;TransformComponent&gt;()!.LocalPosition = new Vector2(16, 0));
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="childPrefab"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when adding <paramref name="childPrefab"/> would create a circular reference
    /// (e.g., a prefab referencing itself, directly or transitively).
    /// </exception>
    public EntityPrefab AddChildPrefab(EntityPrefab childPrefab, Action<Entity>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(childPrefab);

        if (childPrefab == this || ContainsPrefab(childPrefab, this))
            throw new ArgumentException(
                $"Adding prefab '{childPrefab.Name}' as a child of '{Name}' would create a circular reference.",
                nameof(childPrefab));

        _childPrefabs.Add((childPrefab, configure));
        return this;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="target"/> appears anywhere in
    /// the child-prefab graph rooted at <paramref name="root"/> (depth-first, cycle-safe).
    /// </summary>
    private static bool ContainsPrefab(EntityPrefab root, EntityPrefab target)
    {
        var visited = new HashSet<EntityPrefab>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<EntityPrefab>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current)) continue;

            foreach (var (child, _) in current._childPrefabs)
            {
                if (child == target) return true;
                stack.Push(child);
            }
        }

        return false;
    }

    /// <summary>
    /// Instantiates this prefab as a new entity.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="position">
    /// Optional spawn position applied to the entity's <see cref="TransformComponent"/>.
    /// Ignored with a warning if the prefab has no <see cref="TransformComponent"/>.
    /// </param>
    /// <param name="rotation">
    /// Optional spawn rotation (in radians) applied to the entity's <see cref="TransformComponent"/>.
    /// Ignored with a warning if the prefab has no <see cref="TransformComponent"/>.
    /// </param>
    /// <param name="scale">
    /// Optional spawn scale applied to the entity's <see cref="TransformComponent"/>.
    /// Ignored with a warning if the prefab has no <see cref="TransformComponent"/>.
    /// </param>
    /// <param name="logger">Optional logger for diagnostic warnings.</param>
    /// <param name="name">
    /// Optional override for the instantiated entity's name. When <see langword="null"/>
    /// (default), the prefab's own <see cref="Name"/> is used. Pass an explicit name when
    /// spawning multiple instances so each entity can be distinguished by
    /// <see cref="IEntityWorld.GetEntityByName"/>.
    /// </param>
    public Entity Instantiate(IEntityWorld world, Vector2? position = null, float? rotation = null, Vector2? scale = null, ILogger? logger = null, string? name = null)
    {
        var entity = world.CreateEntity(name ?? Name);

        foreach (var tag in Tags)
            entity.AddTag(tag);

        foreach (var configurator in _componentConfigurators)
            configurator(entity);

        if (position.HasValue || rotation.HasValue || scale.HasValue)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                if (position.HasValue)
                    transform.Position = position.Value;
                if (rotation.HasValue)
                    transform.Rotation = rotation.Value;
                if (scale.HasValue)
                    transform.LocalScale = scale.Value;
            }
            else
            {
                if (position.HasValue)
                    logger?.LogWarning(
                        "Prefab '{PrefabName}': position {Position} was ignored because the entity has no TransformComponent. " +
                        "Add a TransformComponent to the prefab or omit the position argument.",
                        Name, position.Value);
                if (rotation.HasValue)
                    logger?.LogWarning(
                        "Prefab '{PrefabName}': rotation {Rotation} was ignored because the entity has no TransformComponent. " +
                        "Add a TransformComponent to the prefab or omit the rotation argument.",
                        Name, rotation.Value);
                if (scale.HasValue)
                    logger?.LogWarning(
                        "Prefab '{PrefabName}': scale {Scale} was ignored because the entity has no TransformComponent. " +
                        "Add a TransformComponent to the prefab or omit the scale argument.",
                        Name, scale.Value);
            }
        }

        foreach (var (childPrefab, configure) in _childPrefabs)
        {
            var child = childPrefab.Instantiate(world, logger: logger);
            child.SetParent(entity);
            configure?.Invoke(child);
        }

        return entity;
    }
}