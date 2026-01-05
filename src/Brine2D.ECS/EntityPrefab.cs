using System.Numerics;
using Brine2D.ECS.Components;

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

    public EntityPrefab(string name)
    {
        Name = name;
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
        _componentConfigurators.Add(entity =>
        {
            var component = entity.AddComponent<T>();
            configure?.Invoke(component);
        });
        return this;
    }

    /// <summary>
    /// Instantiates this prefab as a new entity.
    /// </summary>
    public Entity Instantiate(IEntityWorld world, Vector2? position = null)
    {
        var entity = world.CreateEntity(Name);

        // Apply tags
        foreach (var tag in Tags)
        {
            entity.Tags.Add(tag);
        }

        // Add and configure components
        foreach (var configurator in _componentConfigurators)
        {
            configurator(entity);
        }

        // Set position if provided (overrides any position set by configurators)
        if (position.HasValue)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                transform.Position = position.Value;
            }
        }

        return entity;
    }
}