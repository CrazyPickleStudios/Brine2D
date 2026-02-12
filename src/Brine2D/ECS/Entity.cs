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
    
    // Hierarchy support
    private Entity? _parent;
    private readonly List<Entity> _children = new();

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public IEntityWorld? World { get; internal set; }
    
    /// <summary>
    /// Gets the tags collection for this entity.
    /// </summary>
    public HashSet<string> Tags => _tags;
    
    /// <summary>
    /// Gets the parent entity, or null if this is a root entity.
    /// </summary>
    public Entity? Parent => _parent;
    
    /// <summary>
    /// Gets the read-only collection of child entities.
    /// </summary>
    public IReadOnlyList<Entity> Children => _children.AsReadOnly();
    
    /// <summary>
    /// Gets whether this entity is a root entity (has no parent).
    /// </summary>
    public bool IsRoot => _parent == null;

    /// <summary>
    /// Internal constructor. Entities should be created via World.CreateEntity().
    /// </summary>
    internal Entity(ILogger<Entity>? logger = null)
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
        
        // Render all enabled components
        foreach (var component in _components.Values)
        {
            if (component.IsEnabled)
            {
                component.OnRender(renderer);
            }
        }
    }

    /// <summary>
    /// Called once when the entity is destroyed and removed from the world.
    /// Override to implement cleanup logic.
    /// </summary>
    public virtual void OnDestroy()
    {
        // Override in derived classes
        
        // Destroy all children first (cascade destruction)
        foreach (var child in _children.ToList()) // ToList to avoid modification during iteration
        {
            World?.DestroyEntity(child);
        }
        
        // Remove from parent
        if (_parent != null)
        {
            _parent._children.Remove(this);
            _parent = null;
        }
        
        // Notify all components they're being removed
        foreach (var component in _components.Values.ToList())
        {
            component.OnRemoved();
        }
        
        _components.Clear();
        _children.Clear();
    }

    #endregion

    #region Hierarchy Management

    /// <summary>
    /// Sets the parent of this entity.
    /// </summary>
    /// <param name="newParent">The new parent entity, or null to make this a root entity.</param>
    /// <returns>This entity for method chaining.</returns>
    /// <remarks>
    /// Changing parent affects transform hierarchy if TransformComponent is present.
    /// </remarks>
    /// <example>
    /// <code>
    /// var weapon = World.CreateEntity("Sword");
    /// weapon.SetParent(player); // Weapon follows player
    /// </code>
    /// </example>
    public Entity SetParent(Entity? newParent)
    {
        // Don't parent to self
        if (newParent == this)
        {
            _logger?.LogWarning("Cannot set entity {Name} as its own parent", Name);
            return this;
        }
        
        // Don't create circular references
        if (newParent != null && newParent.IsDescendantOf(this))
        {
            _logger?.LogWarning("Cannot set parent - would create circular reference");
            return this;
        }
        
        // Remove from old parent
        if (_parent != null)
        {
            _parent._children.Remove(this);
        }
        
        // Set new parent
        _parent = newParent;
        
        // Add to new parent's children
        if (_parent != null)
        {
            _parent._children.Add(this);
        }
        
        return this;
    }
    
    /// <summary>
    /// Adds a child entity to this entity.
    /// </summary>
    /// <param name="child">The entity to add as a child.</param>
    /// <returns>This entity for method chaining.</returns>
    public Entity AddChild(Entity child)
    {
        child.SetParent(this);
        return this;
    }
    
    /// <summary>
    /// Removes a child entity from this entity (makes it a root entity).
    /// </summary>
    /// <param name="child">The child entity to remove.</param>
    /// <returns>True if the child was removed, false if it wasn't a child of this entity.</returns>
    public bool RemoveChild(Entity child)
    {
        if (_children.Remove(child))
        {
            child._parent = null;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks if this entity is a descendant of the specified entity.
    /// </summary>
    private bool IsDescendantOf(Entity potentialAncestor)
    {
        var current = _parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current._parent;
        }
        return false;
    }

    /// <summary>
    /// Gets all descendant entities (children, grandchildren, etc.) recursively.
    /// </summary>
    /// <returns>All descendants of this entity.</returns>
    /// <example>
    /// <code>
    /// // Disable all descendants
    /// foreach (var descendant in entity.GetDescendants())
    /// {
    ///     descendant.IsActive = false;
    /// }
    /// </code>
    /// </example>
    public IEnumerable<Entity> GetDescendants()
    {
        foreach (var child in _children)
        {
            yield return child;

            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Finds the first descendant entity with the specified name.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <returns>The first descendant with matching name, or null if not found.</returns>
    /// <example>
    /// <code>
    /// var leftWheel = car.FindDescendant("LeftWheel");
    /// </code>
    /// </example>
    public Entity? FindDescendant(string name)
    {
        foreach (var child in _children)
        {
            if (child.Name == name)
                return child;
            
            var found = child.FindDescendant(name);
            if (found != null)
                return found;
        }
        
        return null;
    }

    /// <summary>
    /// Gets all descendants with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>All descendant entities with the specified tag.</returns>
    /// <example>
    /// <code>
    /// // Find all "Weapon" tagged children of player
    /// var weapons = player.GetDescendantsWithTag("Weapon");
    /// </code>
    /// </example>
    public IEnumerable<Entity> GetDescendantsWithTag(string tag)
    {
        foreach (var descendant in GetDescendants())
        {
            if (descendant.HasTag(tag))
                yield return descendant;
        }
    }

    /// <summary>
    /// Gets the depth of this entity in the hierarchy (0 = root).
    /// </summary>
    /// <returns>The depth level, where 0 is a root entity.</returns>
    public int GetDepth()
    {
        int depth = 0;
        var current = _parent;
        while (current != null)
        {
            depth++;
            current = current._parent;
        }
        return depth;
    }

    /// <summary>
    /// Detaches this entity from its parent, making it a root entity.
    /// </summary>
    /// <returns>This entity for method chaining.</returns>
    public Entity DetachFromParent()
    {
        return SetParent(null);
    }

    /// <summary>
    /// Gets the root entity of this hierarchy.
    /// </summary>
    /// <returns>The root entity (entity with no parent).</returns>
    public Entity GetRoot()
    {
        var current = this;
        while (current._parent != null)
        {
            current = current._parent;
        }
        return current;
    }

    #endregion

    #region Component Management

    /// <summary>
    /// Creates and adds a component of the specified type.
    /// If a component of this type already exists, does nothing.
    /// </summary>
    /// <returns>This entity for method chaining.</returns>
    /// <example>
    /// <code>
    /// var player = World.CreateEntity("Player")
    ///     .AddComponent&lt;TransformComponent&gt;()
    ///     .AddComponent&lt;SpriteRenderer&gt;()
    ///     .AddComponent&lt;PlayerController&gt;()
    ///     .AddTag("Player");
    /// </code>
    /// </example>
    public Entity AddComponent<T>() where T : Component, new()
    {
        AddComponent(new T());
        return this;
    }

    /// <summary>
    /// Adds an existing component instance to this entity.
    /// If a component of this type already exists, does nothing.
    /// </summary>
    /// <returns>This entity for method chaining.</returns>
    public Entity AddComponent<T>(T component) where T : Component
    {
        var type = typeof(T);

        // Check for existing component
        if (_components.TryGetValue(type, out var existing))
        {
            _logger?.LogDebug("Entity {Name} ({Id}) already has component {Type}, skipping", 
                Name, Id, type.Name);
            return this;
        }

        // Add new component
        _components[type] = component;
        component.Entity = this;
        
        // Notify world (for cached queries, etc.)
        if (World is EntityWorld world)
        {
            world.NotifyComponentAdded(this, component);
        }
        
        // Trigger lifecycle
        component.OnAdded();

        return this;
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

    /// <summary>
    /// Removes a component of the specified type from this entity.
    /// </summary>
    /// <returns>True if a component was removed, false if no component of that type existed.</returns>
    public bool RemoveComponent<T>() where T : Component
    {
        return RemoveComponent(typeof(T));
    }

    /// <summary>
    /// Removes a component by type.
    /// </summary>
    /// <returns>True if a component was removed, false if no component of that type existed.</returns>
    public bool RemoveComponent(Type componentType)
    {
        if (_components.TryGetValue(componentType, out var component))
        {
            _components.Remove(componentType);
            
            // Trigger lifecycle
            component.OnRemoved();
            component.Entity = null;
            
            // Notify world (for cached queries, etc.)
            if (World is EntityWorld world)
            {
                world.NotifyComponentRemoved(this, component);
            }
            
            return true;
        }

        return false;
    }

    public IEnumerable<Component> GetAllComponents()
    {
        return _components.Values;
    }

    /// <summary>
    /// Gets a component in this entity or any of its children (recursive search).
    /// </summary>
    /// <returns>The first matching component found, or null if none found.</returns>
    /// <example>
    /// <code>
    /// // Find a Rigidbody component in player or any child (like equipped weapon)
    /// var rb = player.GetComponentInChildren&lt;Rigidbody&gt;();
    /// </code>
    /// </example>
    public T? GetComponentInChildren<T>() where T : Component
    {
        // Check this entity first
        var component = GetComponent<T>();
        if (component != null)
            return component;

        // Search children recursively (depth-first)
        foreach (var child in _children)
        {
            component = child.GetComponentInChildren<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    /// <summary>
    /// Gets a component in this entity or any of its ancestors.
    /// </summary>
    /// <returns>The first matching component found walking up the hierarchy, or null if none found.</returns>
    /// <example>
    /// <code>
    /// // Find a Canvas component in this UI element or any parent
    /// var canvas = uiElement.GetComponentInParent&lt;Canvas&gt;();
    /// </code>
    /// </example>
    public T? GetComponentInParent<T>() where T : Component
    {
        // Check this entity first
        var component = GetComponent<T>();
        if (component != null)
            return component;

        // Walk up parent chain
        return _parent?.GetComponentInParent<T>();
    }

    /// <summary>
    /// Destroys this entity, removing it from the world.
    /// The destruction will be deferred if called during frame processing.
    /// Children are also destroyed (cascade destruction).
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

    #endregion

    #region Tag Management

    /// <summary>
    /// Adds a tag to this entity.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <returns>This entity for method chaining.</returns>
    /// <example>
    /// <code>
    /// var enemy = World.CreateEntity("Goblin")
    ///     .AddComponent&lt;TransformComponent&gt;()
    ///     .AddTag("Enemy")
    ///     .AddTag("Melee");
    /// </code>
    /// </example>
    public Entity AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            _logger?.LogWarning("Attempted to add null or empty tag to entity {Name}", Name);
            return this;
        }
        
        _tags.Add(tag);
        return this;
    }

    /// <summary>
    /// Adds multiple tags to this entity.
    /// </summary>
    /// <param name="tags">The tags to add.</param>
    /// <returns>This entity for method chaining.</returns>
    /// <example>
    /// <code>
    /// var boss = World.CreateEntity("Dragon")
    ///     .AddTags("Enemy", "Boss", "Flying", "FireBreathing");
    /// </code>
    /// </example>
    public Entity AddTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                _tags.Add(tag);
            }
        }
        return this;
    }

    /// <summary>
    /// Removes a tag from this entity.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>This entity for method chaining.</returns>
    public Entity RemoveTag(string tag)
    {
        _tags.Remove(tag);
        return this;
    }

    /// <summary>
    /// Checks if this entity has the specified tag.
    /// </summary>
    /// <param name="tag">The tag to check for.</param>
    /// <returns>True if the entity has the tag, false otherwise.</returns>
    public bool HasTag(string tag)
    {
        return _tags.Contains(tag);
    }

    /// <summary>
    /// Checks if this entity has all of the specified tags.
    /// </summary>
    /// <param name="tags">Tags to check for.</param>
    /// <returns>True if the entity has all tags, false otherwise.</returns>
    public bool HasAllTags(params string[] tags)
    {
        return tags.All(tag => _tags.Contains(tag));
    }

    /// <summary>
    /// Checks if this entity has any of the specified tags.
    /// </summary>
    /// <param name="tags">Tags to check for.</param>
    /// <returns>True if the entity has at least one tag, false otherwise.</returns>
    public bool HasAnyTag(params string[] tags)
    {
        return tags.Any(tag => _tags.Contains(tag));
    }

    /// <summary>
    /// Clears all tags from this entity.
    /// </summary>
    /// <returns>This entity for method chaining.</returns>
    public Entity ClearTags()
    {
        _tags.Clear();
        return this;
    }

    #endregion

    public override string ToString()
    {
        var parentInfo = _parent != null ? $", Parent: {_parent.Name}" : "";
        var childrenInfo = _children.Count > 0 ? $", Children: {_children.Count}" : "";
        return $"Entity {Name} ({Id}) - Active: {IsActive}, Components: {_components.Count}, Tags: {_tags.Count}{parentInfo}{childrenInfo}";
    }
}