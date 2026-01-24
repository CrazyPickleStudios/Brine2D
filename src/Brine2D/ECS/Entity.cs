using System.Buffers;
using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS.Components;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS;

/// <summary>
/// Base class for game entities in the object-based ECS.
/// Entities are containers for components and can have custom behavior.
/// </summary>
public class Entity
{
    private readonly List<Component> _components = new();
    private readonly ILogger? _logger;
    private bool _wasActive;

    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of this entity (optional, for debugging/queries).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this entity is active and should be updated.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;

                // Fire lifecycle events on components
                if (_isActive && !_wasActive)
                {
                    OnActivated();
                }
                else if (!_isActive && _wasActive)
                {
                    OnDeactivated();
                }

                _wasActive = _isActive;
            }
        }
    }
    private bool _isActive = true;

    /// <summary>
    /// The world this entity belongs to.
    /// </summary>
    public IEntityWorld? World { get; internal set; }

    /// <summary>
    /// Gets whether this entity is still valid (not destroyed).
    /// </summary>
    public bool IsValid => World != null;

    /// <summary>
    /// Tags for grouping/querying entities.
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// Event fired when this entity is destroyed.
    /// </summary>
    public event Action<Entity>? OnDestroyed;

    /// <summary>
    /// Event fired when a component is added to this entity.
    /// </summary>
    public event Action<Entity, Component>? OnComponentAdded;

    /// <summary>
    /// Event fired when a component is removed from this entity.
    /// </summary>
    public event Action<Entity, Component>? OnComponentRemoved;

    internal Entity(ILogger? logger = null)
    {
        _logger = logger;
        _wasActive = _isActive;
    }

    /// <summary>
    /// Throws if this entity has been destroyed.
    /// </summary>
    private void ThrowIfDestroyed()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' ({Id}) has been destroyed and cannot be used.");
        }
    }

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    public T AddComponent<T>() where T : Component, new()
    {
        ThrowIfDestroyed();

        var existing = GetComponent<T>();
        if (existing != null)
        {
            _logger?.LogWarning("Component {Type} already exists on entity {Name}", typeof(T).Name, Name);
            return existing;
        }

        var component = new T();
        component.Entity = this;
        _components.Add(component);

        component.OnAdded();

        // Fire event
        OnComponentAdded?.Invoke(this, component);
        World?.NotifyComponentAdded(this, component);

        _logger?.LogDebug("Added component {Type} to entity {Name}", typeof(T).Name, Name);
        return component;
    }

    /// <summary>
    /// Adds a pre-configured component instance to this entity.
    /// Useful for complex components that require configuration before adding.
    /// </summary>
    /// <param name="component">The component instance to add.</param>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The added component (or existing if already present).</returns>
    public T AddComponent<T>(T component) where T : Component
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        ThrowIfDestroyed();

        var type = typeof(T);

        var existing = _components.FirstOrDefault(c => c.GetType() == type) as T;

        if (existing != null)
        {
            _logger?.LogWarning("Entity {EntityId} already has component {ComponentType}. Returning existing component.", Id, type.Name);
            return existing;
        }

        // Add the new component
        component.Entity = this;
        _components.Add(component);
        component.OnAdded();
        OnComponentAdded?.Invoke(this, component);
        World?.NotifyComponentAdded(this, component);
        _logger?.LogDebug("Added component {ComponentType} to entity {EntityId} (pre-configured instance)", type.Name, Id);

        return component;
    }

    /// <summary>
    /// Adds a component and returns the entity for method chaining (fluent API).
    /// </summary>
    public Entity WithComponent<T>() where T : Component, new()
    {
        AddComponent<T>();
        return this;
    }

    /// <summary>
    /// Adds a pre-configured component and returns the entity for method chaining (fluent API).
    /// </summary>
    public Entity WithComponent<T>(T component) where T : Component
    {
        AddComponent(component);
        return this;
    }

    /// <summary>
    /// Gets a component of the specified type.
    /// </summary>
    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets a required component, throwing if it doesn't exist (ASP.NET GetRequiredService pattern).
    /// </summary>
    public T GetRequiredComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' ({Id}) does not have required component {typeof(T).Name}");
        }
        return component;
    }

    /// <summary>
    /// Tries to get a component (safe retrieval pattern).
    /// </summary>
    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = GetComponent<T>();
        return component != null;
    }

    /// <summary>
    /// Gets a component in this entity or its children (Unity pattern).
    /// </summary>
    public T? GetComponentInChildren<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component != null) return component;

        var transform = GetComponent<TransformComponent>();
        if (transform == null) return null;

        foreach (var child in transform.Children)
        {
            var childComponent = child.Entity?.GetComponentInChildren<T>();
            if (childComponent != null) return childComponent;
        }

        return null;
    }

    /// <summary>
    /// Gets a component in this entity or its parent (Unity pattern).
    /// </summary>
    public T? GetComponentInParent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component != null) return component;

        var transform = GetComponent<TransformComponent>();
        return transform?.Parent?.Entity?.GetComponentInParent<T>();
    }

    /// <summary>
    /// Gets all components of the specified type.
    /// </summary>
    public IEnumerable<T> GetComponents<T>() where T : Component
    {
        return _components.OfType<T>();
    }

    /// <summary>
    /// Checks if this entity has a component of the specified type.
    /// </summary>
    public bool HasComponent<T>() where T : Component
    {
        return _components.Any(c => c is T);
    }

    /// <summary>
    /// Checks if the entity has a component of the specified type.
    /// </summary>
    public bool HasComponent(Type componentType)
    {
        return _components.Any(componentType.IsInstanceOfType);
    }

    /// <summary>
    /// Checks if this entity has the specified tag.
    /// </summary>
    public bool HasTag(string tag) => Tags.Contains(tag);

    /// <summary>
    /// Adds a tag to this entity (fluent).
    /// </summary>
    public Entity AddTag(string tag)
    {
        Tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Removes a tag from this entity (fluent).
    /// </summary>
    public Entity RemoveTag(string tag)
    {
        Tags.Remove(tag);
        return this;
    }

    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    public bool RemoveComponent<T>() where T : Component
    {
        ThrowIfDestroyed();

        var component = GetComponent<T>();
        if (component == null)
            return false;

        component.OnRemoved();
        component.Entity = null;
        _components.Remove(component);

        // Fire event
        OnComponentRemoved?.Invoke(this, component);
        World?.NotifyComponentRemoved(this, component);

        _logger?.LogDebug("Removed component {Type} from entity {Name}", typeof(T).Name, Name);
        return true;
    }

    /// <summary>
    /// Removes a component by type.
    /// </summary>
    public bool RemoveComponent(Type componentType)
    {
        var component = _components.FirstOrDefault(c => c.GetType() == componentType);
        if (component == null) return false;

        component.OnRemoved();
        component.Entity = null;
        _components.Remove(component);
        OnComponentRemoved?.Invoke(this, component);
        World?.NotifyComponentRemoved(this, component);
        return true;
    }

    /// <summary>
    /// Removes all components from this entity.
    /// </summary>
    public void RemoveAllComponents()
    {
        ThrowIfDestroyed();

        foreach (var component in _components.ToList())
        {
            RemoveComponent(component.GetType());
        }
    }

    /// <summary>
    /// Gets all components on this entity.
    /// </summary>
    public IReadOnlyList<Component> GetAllComponents() => _components.AsReadOnly();

    /// <summary>
    /// Called during initialization. Override to set up components.
    /// </summary>
    protected internal virtual void OnInitialize() { }

    /// <summary>
    /// Called every frame to update entity logic.
    /// Uses ArrayPool to avoid allocations while safely handling component modifications.
    /// </summary>
    protected internal virtual void OnUpdate(GameTime gameTime)
    {
        if (!IsActive) return;

        var count = _components.Count;
        if (count == 0) return;

        // Rent array from pool (reuses existing arrays, minimal allocation)
        var componentsArray = ArrayPool<Component>.Shared.Rent(count);

        try
        {
            // Create snapshot of components
            _components.CopyTo(componentsArray, 0);

            // Iterate snapshot (safe from add/remove during OnUpdate)
            for (int i = 0; i < count; i++)
            {
                var component = componentsArray[i];
                if (component.IsEnabled)
                {
                    component.OnUpdate(gameTime);
                }
            }
        }
        finally
        {
            // Return to pool for reuse (clearArray ensures no memory leaks)
            ArrayPool<Component>.Shared.Return(componentsArray, clearArray: true);
        }
    }

    /// <summary>
    /// Called when entity is activated.
    /// </summary>
    private void OnActivated()
    {
        var count = _components.Count;
        if (count == 0) return;

        var componentsArray = ArrayPool<Component>.Shared.Rent(count);

        try
        {
            _components.CopyTo(componentsArray, 0);

            for (int i = 0; i < count; i++)
            {
                var component = componentsArray[i];
                if (component.IsEnabled)
                {
                    component.OnEnabled();
                }
            }
        }
        finally
        {
            ArrayPool<Component>.Shared.Return(componentsArray, clearArray: true);
        }
    }

    /// <summary>
    /// Called when entity is deactivated.
    /// </summary>
    private void OnDeactivated()
    {
        var count = _components.Count;
        if (count == 0) return;

        var componentsArray = ArrayPool<Component>.Shared.Rent(count);

        try
        {
            _components.CopyTo(componentsArray, 0);

            for (int i = 0; i < count; i++)
            {
                var component = componentsArray[i];
                if (component.IsEnabled)
                {
                    component.OnDisabled();
                }
            }
        }
        finally
        {
            ArrayPool<Component>.Shared.Return(componentsArray, clearArray: true);
        }
    }

    /// <summary>
    /// Called when entity is destroyed.
    /// Recursively destroys all child entities before destroying this entity.
    /// </summary>
    protected internal virtual void OnDestroy()
    {
        var transform = GetComponent<TransformComponent>();
        if (transform != null)
        {
            var children = transform.Children.ToList(); // Snapshot to avoid modification during iteration
            foreach (var childTransform in children)
            {
                if (childTransform.Entity != null && childTransform.Entity != this)
                {
                    childTransform.Entity.Destroy(); // Recursive!
                }
            }
        }

        OnDestroyed?.Invoke(this);
        World?.NotifyEntityDestroyed(this);

        foreach (var component in _components.ToList())
        {
            component.OnRemoved();
        }
        _components.Clear();
    }

    /// <summary>
    /// Destroys this entity (removes from world).
    /// </summary>
    public void Destroy()
    {
        World?.DestroyEntity(this);
    }

    public override string ToString() =>
        string.IsNullOrEmpty(Name) ? $"Entity {Id}" : $"{Name} ({Id})";
}