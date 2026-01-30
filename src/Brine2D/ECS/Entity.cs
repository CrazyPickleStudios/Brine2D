using System.Buffers;
using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS.Components;
using Microsoft.Extensions.Logging;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for entities in the ECS.
/// Supports hybrid approach: can be used as a component container (data-oriented)
/// or as a behavior host (object-oriented) by overriding lifecycle methods.
/// </summary>
public class Entity
{
    private readonly ILogger<Entity>? _logger;
    private readonly Dictionary<Type, Component> _components = new();
    private readonly HashSet<string> _tags = new();

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public IEntityWorld? World { get; internal set; }
    
    /// <summary>
    /// Gets the tags collection for this entity.
    /// </summary>
    public HashSet<string> Tags => _tags;

    public Entity(ILogger<Entity>? logger = null)
    {
        _logger = logger;
    }

    #region Lifecycle Methods (Override for object-oriented ECS)

    /// <summary>
    /// Called once when the entity is created and added to the world.
    /// Override to implement initialization logic.
    /// </summary>
    public virtual void OnInitialize()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called every frame during the update phase.
    /// Override to implement per-frame logic.
    /// </summary>
    public virtual void OnUpdate(GameTime gameTime)
    {
        // Override in derived classes
        
        // Update all enabled components
        foreach (var component in _components.Values)
        {
            if (component.IsEnabled)
            {
                component.OnUpdate(gameTime);
            }
        }
    }

    /// <summary>
    /// Called every frame during the render phase.
    /// Override to implement custom rendering logic.
    /// </summary>
    public virtual void OnRender(IRenderer renderer)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called once when the entity is destroyed and removed from the world.
    /// Override to implement cleanup logic.
    /// </summary>
    public virtual void OnDestroy()
    {
        // Override in derived classes
        
        // Notify all components they're being removed
        foreach (var component in _components.Values.ToList())
        {
            component.OnRemoved();
        }
        
        _components.Clear();
    }

    #endregion

    #region Component Management

    /// <summary>
    /// Creates and adds a component of the specified type.
    /// </summary>
    public T AddComponent<T>() where T : Component, new()
    {
        var component = new T();
        return AddComponent(component);
    }

    /// <summary>
    /// Adds an existing component instance to this entity.
    /// If a component of this type already exists, returns the existing component instead.
    /// </summary>
    public T AddComponent<T>(T component) where T : Component
    {
        var type = typeof(T);

        if (_components.TryGetValue(type, out var existing))
        {
            _logger?.LogDebug("Entity {Id} already has component {Type}, returning existing", Id, type.Name);
            return (T)existing; // Return the attached component, not the new one
        }

        _components[type] = component;
        component.Entity = this;

        // Call lifecycle method
        component.OnAdded();

        World?.NotifyComponentAdded(this, component);

        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        var type = typeof(T);
        return _components.TryGetValue(type, out var component) ? component as T : null;
    }

    /// <summary>
    /// Gets a required component, throwing if not present (ASP.NET GetRequiredService pattern).
    /// </summary>
    public T GetRequiredComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' ({Id}) does not have required component '{typeof(T).Name}'. " +
                $"Did you forget to add it?");
        }
        return component;
    }

    public bool HasComponent<T>() where T : Component
    {
        return _components.ContainsKey(typeof(T));
    }

    public bool HasComponent(Type componentType)
    {
        return _components.ContainsKey(componentType);
    }

    public bool RemoveComponent<T>() where T : Component
    {
        return RemoveComponent(typeof(T));
    }

    /// <summary>
    /// Removes a component by type.
    /// </summary>
    public bool RemoveComponent(Type componentType)
    {
        if (_components.TryGetValue(componentType, out var component))
        {
            _components.Remove(componentType);
            
            // Call lifecycle method
            component.OnRemoved();
            component.Entity = null;
            
            World?.NotifyComponentRemoved(this, component);
            
            return true;
        }

        return false;
    }

    public IEnumerable<Component> GetAllComponents()
    {
        return _components.Values;
    }

    /// <summary>
    /// Gets a component in this entity or its children.
    /// Note: Requires hierarchical entity support (parent/child relationships).
    /// Returns null if hierarchy is not set up.
    /// </summary>
    public T? GetComponentInChildren<T>() where T : Component
    {
        // First check this entity
        var component = GetComponent<T>();
        if (component != null)
            return component;

        // TODO: If you add parent/child relationships, search children here
        // For now, just return null
        return null;
    }

    /// <summary>
    /// Destroys this entity, removing it from the world.
    /// The destruction will be deferred if called during frame processing.
    /// </summary>
    public void Destroy()
    {
        if (World == null)
        {
            _logger?.LogWarning("Cannot destroy entity {Id} - not attached to a world", Id);
            return;
        }

        World.DestroyEntity(this);
    }

    /// <summary>
    /// Gets a component in this entity or its parent.
    /// Note: Requires hierarchical entity support (parent/child relationships).
    /// Returns null if hierarchy is not set up.
    /// </summary>
    public T? GetComponentInParent<T>() where T : Component
    {
        // First check this entity
        var component = GetComponent<T>();
        if (component != null)
            return component;

        // TODO: If you add parent/child relationships, search parent here
        // For now, just return null
        return null;
    }

    #endregion

    #region Tag Management

    public void AddTag(string tag)
    {
        _tags.Add(tag);
    }

    public void RemoveTag(string tag)
    {
        _tags.Remove(tag);
    }

    public bool HasTag(string tag)
    {
        return _tags.Contains(tag);
    }

    #endregion

    public override string ToString()
    {
        return $"Entity {Name} ({Id}) - Active: {IsActive}, Components: {_components.Count}, Tags: {_tags.Count}";
    }
}