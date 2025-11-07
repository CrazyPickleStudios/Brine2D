using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Scene;

/// <summary>
///     Represents a game object within a <see cref="Scene" /> that aggregates a set of <see cref="Component" />s.
///     Delegates lifecycle operations (Initialize/Update/Draw) to its components and tracks enabled state.
/// </summary>
public sealed class Entity
{
    /// <summary>
    ///     Backing store for all components attached to this entity.
    ///     Order of insertion is preserved and used for iteration.
    /// </summary>
    private readonly List<Component> _components = new();

    /// <summary>
    ///     Creates a new <see cref="Entity" /> with the specified display <paramref name="name" />.
    /// </summary>
    /// <param name="name">Optional name for debugging/identification.</param>
    public Entity(string name = "Entity")
    {
        Name = name;
    }

    /// <summary>
    ///     When false, suppresses per-frame <see cref="UpdateAll(GameTime)" /> and <see cref="DrawAll(GameTime)" /> calls
    ///     for all components on this entity. Does not affect add/remove/initialize semantics.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Display name used for identification and debugging.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Parent <see cref="Scene" />. Set internally by the scene when the entity is created/added.
    /// </summary>
    internal Scene Scene { get; set; } = null!;

    /// <summary>
    ///     Adds a <see cref="Component" /> instance to this entity.
    ///     Sets <see cref="Component.Entity" /> and, if the owning <see cref="Scene" /> is already initialized,
    ///     immediately invokes <see cref="Component.Initialize(IEngineContext)" /> followed by
    ///     <see cref="Component.OnAdded()" />.
    /// </summary>
    /// <typeparam name="T">Concrete component type.</typeparam>
    /// <param name="component">Component instance to attach. Must not be null.</param>
    /// <returns>The same <paramref name="component" /> for fluent usage.</returns>
    public T Add<T>(T component) where T : Component
    {
        // Establish back-reference to owning entity.
        component.Entity = this;
        _components.Add(component);

        // If the scene is live, run component initialization and added hook immediately.
        if (Scene.IsInitialized)
        {
            component.Initialize(Scene.Engine);
            component.OnAdded();
        }

        return component;
    }

    /// <summary>
    ///     Retrieves the first component of type <typeparamref name="T" /> attached to this entity.
    /// </summary>
    /// <typeparam name="T">Component type to search for.</typeparam>
    /// <returns>The first matching component, or null if none are found.</returns>
    public T? GetComponent<T>() where T : Component
    {
        for (var i = 0; i < _components.Count; i++)
        {
            if (_components[i] is T t)
            {
                return t;
            }
        }

        return null;
    }

    /// <summary>
    ///     Removes a previously added <see cref="Component" /> from this entity.
    ///     If the component existed and the entity is in a live scene, <see cref="Component.OnRemoved()" /> is invoked.
    /// </summary>
    /// <param name="component">The component to remove.</param>
    /// <returns>True if the component was present and removed; otherwise, false.</returns>
    public bool Remove(Component component)
    {
        if (!_components.Remove(component))
        {
            return false;
        }

        // Notify the component it has been detached from a live entity.
        component.OnRemoved();
        return true;
    }

    /// <summary>
    ///     Forwards per-frame draw to all components when <see cref="Enabled" /> is true.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    internal void DrawAll(GameTime time)
    {
        if (!Enabled)
        {
            return;
        }

        for (var i = 0; i < _components.Count; i++)
        {
            _components[i].Draw(time);
        }
    }

    /// <summary>
    ///     Initializes all attached components and then invokes their added notifications.
    ///     Called by the owning <see cref="Scene" /> during scene initialization.
    /// </summary>
    /// <param name="engine">Engine context providing shared services.</param>
    internal void InitializeAll(IEngineContext engine)
    {
        // Initialize first so components can rely on engine services.
        foreach (var c in _components)
        {
            c.Initialize(engine);
        }

        // Then notify that the component is now part of a live entity/scene.
        foreach (var c in _components)
        {
            c.OnAdded();
        }
    }

    /// <summary>
    ///     Forwards per-frame update to all components when <see cref="Enabled" /> is true.
    /// </summary>
    /// <param name="time">Frame timing information.</param>
    internal void UpdateAll(GameTime time)
    {
        if (!Enabled)
        {
            return;
        }

        for (var i = 0; i < _components.Count; i++)
        {
            _components[i].Update(time);
        }
    }
}