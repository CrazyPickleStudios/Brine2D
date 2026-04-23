using System.Buffers;
using Brine2D.Core;
using Brine2D.ECS.Components;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Threading;

namespace Brine2D.ECS;

/// <summary>
/// Base class for entities in the ECS.
/// Supports hybrid approach: can be used as a component container (data-oriented)
/// or as a behavior host (object-oriented) by overriding lifecycle methods.
/// </summary>
public class Entity
{
    private ILogger<Entity>? _logger;
    private readonly HashSet<string> _tags = new();

    private Entity? _parent;
    private readonly List<Entity> _children = new();
    private ReadOnlyCollection<Entity>? _readOnlyChildren;
    private readonly List<Behavior> _behaviors = new();
    private ReadOnlyCollection<Behavior>? _readOnlyBehaviors;

    private EntityWorld? _world;
    
    // Global counter across all EntityWorld instances — IDs are intentionally unique
    // process-wide, not per-world. In tests, avoid asserting specific ID values.
    private static long _nextId;

    /// <summary>
    /// Unique ID for this entity. Assigned atomically at creation.
    /// IDs start at 1; 0 is reserved as an invalid/null sentinel.
    /// </summary>
    public long Id { get; } = Interlocked.Increment(ref _nextId);
    public string Name { get; set; } = string.Empty;

    private bool _isActive = true;

    /// <summary>
    /// Indicates whether this entity is active in the world.
    /// Inactive entities are skipped by the ECS processing and
    /// excluded from queries. Set this to false to deactivate
    /// the entity without destroying it.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            _world?.NotifyActiveChanged();
        }
    }

    /// <summary>
    /// Sets <see cref="IsActive"/> without notifying the world.
    /// Used during entity destruction to avoid O(n × q) redundant
    /// query invalidations that will be superseded by pool removal.
    /// </summary>
    internal void SetActiveWithoutNotification(bool value) => _isActive = value;

    /// <summary>
    /// Forwards <see cref="Component.IsEnabled"/> change notifications to the world
    /// so cached queries built with <c>OnlyEnabled()</c> are invalidated.
    /// </summary>
    internal void NotifyComponentEnabledChanged() => _world?.NotifyComponentEnabledChanged();

    /// <summary>
    /// True after <see cref="OnDestroy"/> has removed this entity's components from all pools.
    /// Used by <see cref="EntityWorld"/> to skip the redundant safety-net cleanup pass.
    /// </summary>
    internal bool PoolsCleared { get; private set; }

    /// <summary>
    /// The entity world this entity belongs to, or null if not yet added to a world.
    /// </summary>
    public IEntityWorld? World => _world;

    /// <summary>
    /// Sets the world this entity belongs to.
    /// Internal - only called by EntityWorld and SceneManager.
    /// </summary>
    internal void SetWorld(EntityWorld? world)
    {
        _world = world;
    }

    /// <summary>
    /// Sets the logger for this entity.
    /// Internal — called by <see cref="EntityWorld.CreateEntity{T}"/> after construction
    /// when the <c>new()</c> constraint prevents constructor injection.
    /// </summary>
    internal void SetLogger(ILogger<Entity>? logger) => _logger = logger;

    /// <summary>
    /// Gets the tags for this entity as a read-only set.
    /// Use <see cref="AddTag"/>, <see cref="RemoveTag"/>, and <see cref="ClearTags"/>
    /// to modify tags so that validation and logging are applied consistently.
    /// </summary>
    public IReadOnlySet<string> Tags => _tags;

    /// <summary>
    /// Gets the parent entity, or null if this is a root entity.
    /// </summary>
    public Entity? Parent => _parent;

    /// <summary>
    /// Gets the read-only collection of child entities.
    /// The wrapper is created once and cached; it reflects live changes to the child list.
    /// </summary>
    public IReadOnlyList<Entity> Children => _readOnlyChildren ??= _children.AsReadOnly();

    /// <summary>
    /// Gets whether this entity is a root entity (has no parent).
    /// </summary>
    public bool IsRoot => _parent == null;

    #region Lifecycle Methods (Override for object-oriented ECS)

    /// <summary>
    /// Called once when the entity is created and added to the world.
    /// Override to implement initialization logic.
    /// </summary>
    public virtual void OnInitialize()
    {
    }

    /// <summary>
    /// Called once when the entity is destroyed and removed from the world.
    /// Override to implement custom cleanup logic.
    /// </summary>
    /// <remarks>
    /// Components receive <see cref="Component.OnRemoved"/> automatically during
    /// destruction. Children are destroyed synchronously so their OnDestroy can safely
    /// access parent state (components, behaviors, etc.). Each child is then
    /// queued for deferred removal from the world so that the entity lookup
    /// and tag index are properly cleaned up.
    /// </remarks>
    public virtual void OnDestroy()
    {
        if (_children.Count > 0)
        {
            var count = _children.Count;
            var childSnapshot = ArrayPool<Entity>.Shared.Rent(count);
            try
            {
                _children.CopyTo(childSnapshot, 0);
                for (int i = 0; i < count; i++)
                {
                    var child = childSnapshot[i];
                    if (!child.PoolsCleared)
                    {
                        child.SetActiveWithoutNotification(false);
                        child.OnDestroy();
                    }

                    // Queues deferred removal so the entity lookup and tag index are cleaned up.
                    // The removal callback in ProcessEntityRemovals will skip OnDestroy because
                    // PoolsCleared is already set by the synchronous call above.
                    _world?.DestroyEntity(child);
                }
            }
            finally
            {
                ArrayPool<Entity>.Shared.Return(childSnapshot, clearArray: true);
            }
        }

        if (_parent != null)
        {
            _parent._children.Remove(this);
            _parent = null;
        }

        var behaviorCount = _behaviors.Count;
        if (behaviorCount > 0)
        {
            var behaviorSnapshot = ArrayPool<Behavior>.Shared.Rent(behaviorCount);
            try
            {
                _behaviors.CopyTo(behaviorSnapshot, 0);
                for (int i = 0; i < behaviorCount; i++)
                {
                    behaviorSnapshot[i].OnDetached();
                    _world?.NotifyBehaviorRemoved(this, behaviorSnapshot[i]);
                }
            }
            finally
            {
                ArrayPool<Behavior>.Shared.Return(behaviorSnapshot, clearArray: true);
            }
            _behaviors.Clear();
        }

        _world?.RemoveEntityFromAllPools(Id);
        PoolsCleared = true;

        _children.Clear();
    }

    #endregion

    #region Hierarchy Management

    /// <summary>
    /// Sets the parent of this entity.
    /// </summary>
    public Entity SetParent(Entity? newParent)
    {
        if (newParent == this)
        {
            _logger?.LogWarning("Cannot set entity {Name} as its own parent", Name);
            return this;
        }

        if (newParent != null && newParent.IsDescendantOf(this))
        {
            _logger?.LogWarning("Cannot set parent - would create circular reference");
            return this;
        }

        if (newParent != null && _world != null && newParent._world != null && _world != newParent._world)
        {
            _logger?.LogWarning("Cannot parent entities across different worlds");
            return this;
        }

        if (_parent != null)
            _parent._children.Remove(this);

        _parent = newParent;

        if (_parent != null)
            _parent._children.Add(this);

        return this;
    }

    /// <summary>
    /// Adds a child entity to this entity.
    /// </summary>
    public Entity AddChild(Entity child)
    {
        child.SetParent(this);
        return this;
    }

    /// <summary>
    /// Removes a child entity from this entity (makes it a root entity).
    /// Delegates to <see cref="SetParent"/> so any future side effects added there
    /// (events, transform propagation, etc.) apply through this path too.
    /// </summary>
    public bool RemoveChild(Entity child)
    {
        if (!_children.Contains(child)) return false;
        child.SetParent(null);
        return true;
    }

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
    /// Gets all descendant entities (children, grandchildren, etc.) via iterative depth-first traversal.
    /// </summary>
    public IEnumerable<Entity> GetDescendants()
    {
        if (_children.Count == 0) yield break;

        var stack = new Stack<Entity>();
        for (int i = _children.Count - 1; i >= 0; i--)
            stack.Push(_children[i]);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            for (int i = current._children.Count - 1; i >= 0; i--)
                stack.Push(current._children[i]);
        }
    }

    /// <summary>
    /// Finds the first descendant entity with the specified name.
    /// </summary>
    public Entity? FindDescendant(string name)
    {
        foreach (var descendant in GetDescendants())
            if (descendant.Name == name)
                return descendant;
        return null;
    }

    /// <summary>
    /// Gets all descendants with the specified tag.
    /// </summary>
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
    public Entity DetachFromParent() => SetParent(null);

    /// <summary>
    /// Gets the root entity of this hierarchy.
    /// </summary>
    public Entity GetRoot()
    {
        var current = this;
        while (current._parent != null)
            current = current._parent;
        return current;
    }

    #endregion

    #region Component Management

    /// <summary>
    /// Creates and adds a component of the specified type.
    /// </summary>
    public Entity AddComponent<T>() where T : Component, new()
    {
        AddComponent(new T());
        return this;
    }

    /// <summary>
    /// Creates and adds a component with inline configuration.
    /// If the entity already has this component type, the configure action is applied to the existing one.
    /// </summary>
    /// <remarks>
    /// For new components, <paramref name="configure"/> runs before the component is attached.
    /// <see cref="Component.Entity"/> is <see langword="null"/> during configuration and
    /// <see cref="Component.OnAdded"/> has not yet been called.
    /// For duplicate components, <paramref name="configure"/> is applied to the existing
    /// instance which is already attached.
    /// </remarks>
    public Entity AddComponent<T>(Action<T>? configure) where T : Component, new()
    {
        if (HasComponent<T>())
        {
            _logger?.LogDebug("Entity {Name} ({Id}) already has component {Type}, applying configuration",
                Name, Id, typeof(T).Name);
            configure?.Invoke(GetComponent<T>()!);
            return this;
        }

        var component = new T();
        configure?.Invoke(component);
        AddComponent(component);
        return this;
    }

    /// <summary>
    /// Adds an existing component instance to this entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity has been destroyed or is not attached to a world.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the runtime type of <paramref name="component"/> does not match <typeparamref name="T"/>,
    /// which would cause the component to be stored under the wrong pool key.
    /// </exception>
    public Entity AddComponent<T>(T component) where T : Component
    {
        if (PoolsCleared)
            throw new InvalidOperationException(
                $"Cannot add component to entity '{Name}' ({Id}) - it has been destroyed");

        if (_world == null)
            throw new InvalidOperationException(
                "Cannot add component - entity is not in a world");

        if (typeof(T) != component.GetType())
            throw new ArgumentException(
                $"Component runtime type '{component.GetType().Name}' does not match generic parameter '{typeof(T).Name}'. " +
                $"Use AddComponent<{component.GetType().Name}>() to ensure correct pool registration.",
                nameof(component));

        if (HasComponent<T>())
        {
            _logger?.LogDebug("Entity {Name} ({Id}) already has component {Type}, skipping",
                Name, Id, typeof(T).Name);
            return this;
        }

        component.Entity = this;
        _world.AddComponentToPool(this.Id, component);
        _world.NotifyComponentAdded(this, component);
        component.OnAdded();

        return this;
    }

    public T? GetComponent<T>() where T : Component
        => _world?.GetComponentFromPool<T>(this.Id);

    public bool HasComponent<T>() where T : Component
        => _world?.HasComponentInPool<T>(this.Id) ?? false;

    /// <summary>
    /// Checks if this entity has a component of the specified type (non-generic version).
    /// Used internally by EntityQuery for dynamic type checking.
    /// </summary>
    internal bool HasComponent(Type componentType)
        => _world?.HasComponentOfType(Id, componentType) ?? false;

    public bool RemoveComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
            return false;

        component.OnRemoved();
        component.Entity = null;

        var removed = _world?.RemoveComponentFromPool<T>(this.Id) ?? false;
        if (removed)
            _world?.NotifyComponentRemoved(this, component);

        return removed;
    }

    public T GetRequiredComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' ({Id}) does not have required component '{typeof(T).Name}'." + Environment.NewLine +
                Environment.NewLine +
                "Fix: Add the component before accessing it:" + Environment.NewLine +
                $"  entity.AddComponent<{typeof(T).Name}>();" + Environment.NewLine +
                Environment.NewLine +
                $"Available components: {string.Join(", ", GetAllComponents().Select(c => c.GetType().Name))}");
        }
        return component;
    }

    public IEnumerable<Component> GetAllComponents()
        => _world?.GetAllComponentsFromPool(this.Id) ?? Enumerable.Empty<Component>();

    /// <summary>
    /// Gets a component on this entity or any of its children (depth-first recursive search).
    /// </summary>
    /// <remarks>
    /// Useful for equipment hierarchies, scene graphs, and bone structures.
    /// This is O(depth x children). Avoid calling on hot paths or deep hierarchies.
    /// </remarks>
    public T? GetComponentInChildren<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component != null)
            return component;

        foreach (var child in _children)
        {
            component = child.GetComponentInChildren<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    /// <summary>
    /// Gets a component on this entity or any of its ancestors.
    /// </summary>
    /// <remarks>
    /// This is O(depth). Avoid calling on hot paths or deep hierarchies.
    /// </remarks>
    public T? GetComponentInParent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component != null)
            return component;

        return _parent?.GetComponentInParent<T>();
    }

    /// <summary>
    /// Destroys this entity, removing it from the world.
    /// </summary>
    public void Destroy()
    {
        if (_world == null)
        {
            _logger?.LogWarning("Cannot destroy entity {Id} - not attached to a world", Id);
            return;
        }
        _world.DestroyEntity(this);
    }

    #endregion

    #region Tag Management

    public Entity AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            _logger?.LogWarning("Attempted to add null or empty tag to entity {Name}", Name);
            return this;
        }
        if (_tags.Add(tag))
            _world?.NotifyTagAdded(this, tag);
        return this;
    }

    public Entity AddTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag) && _tags.Add(tag))
                _world?.NotifyTagAdded(this, tag);
        }
        return this;
    }

    public Entity RemoveTag(string tag)
    {
        if (_tags.Remove(tag))
            _world?.NotifyTagRemoved(this, tag);
        return this;
    }

    public bool HasTag(string tag) => _tags.Contains(tag);

    public bool HasAllTags(params string[] tags)
    {
        foreach (var tag in tags)
            if (!_tags.Contains(tag)) return false;
        return true;
    }

    public bool HasAnyTag(params string[] tags)
    {
        foreach (var tag in tags)
            if (_tags.Contains(tag)) return true;
        return false;
    }

    public Entity ClearTags()
    {
        if (_tags.Count > 0)
        {
            _world?.NotifyTagsCleared(this, _tags);
            _tags.Clear();
        }
        return this;
    }

    #endregion

    #region Behaviors

    /// <summary>
    /// Adds a behavior to this entity with automatic dependency injection.
    /// </summary>
    /// <typeparam name="T">The behavior type.</typeparam>
    /// <returns>This entity for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if entity is not in a world.</exception>
    /// <example>
    /// <code>
    /// entity.AddBehavior&lt;PlayerMovementBehavior&gt;()
    ///       .AddBehavior&lt;PlayerShootingBehavior&gt;();
    /// </code>
    /// </example>
    public Entity AddBehavior<T>() where T : Behavior
    {
        if (_world == null)
            throw new InvalidOperationException("Cannot add behavior - entity is not in a world");

        // Duplicate guard, matches AddSystem<T>() pattern
        if (HasBehavior<T>())
        {
            _logger?.LogWarning("Entity {Name} already has behavior {Type}, skipping", Name, typeof(T).Name);
            return this;
        }

        // Behavior creation is the world's responsibility; keeps IServiceProvider private to EntityWorld
        var behavior = _world.CreateBehavior<T>();

        behavior.Entity = this;
        _behaviors.Add(behavior);
        behavior.OnAttached();

        _world.NotifyBehaviorAdded(this, behavior);
        _logger?.LogDebug("Entity {Name} added behavior {Behavior}", Name, typeof(T).Name);

        return this;
    }

    /// <summary>
    /// Checks if this entity has a behavior of the specified type.
    /// </summary>
    public bool HasBehavior<T>() where T : Behavior
    {
        foreach (var b in _behaviors)
            if (b is T) return true;
        return false;
    }

    /// <summary>
    /// Checks if this entity has a behavior of the specified type (non-generic version).
    /// Used internally by query infrastructure for dynamic type checking.
    /// </summary>
    internal bool HasBehavior(Type behaviorType)
    {
        foreach (var b in _behaviors)
            if (behaviorType.IsInstanceOfType(b)) return true;
        return false;
    }

    /// <summary>
    /// Gets a behavior of the specified type attached to this entity.
    /// </summary>
    public T? GetBehavior<T>() where T : Behavior
    {
        foreach (var b in _behaviors)
            if (b is T match) return match;
        return null;
    }

    /// <summary>
    /// Removes a behavior from this entity.
    /// </summary>
    public bool RemoveBehavior<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior == null)
            return false;

        _behaviors.Remove(behavior);
        behavior.OnDetached();
        _world?.NotifyBehaviorRemoved(this, behavior);
        _logger?.LogDebug("Entity {Name} removed behavior {Behavior}", Name, typeof(T).Name);

        return true;
    }

    /// <summary>
    /// Gets all behaviors attached to this entity.
    /// </summary>
    public IReadOnlyList<Behavior> GetAllBehaviors() => _readOnlyBehaviors ??= _behaviors.AsReadOnly();

    #endregion

    public override string ToString()
    {
        var parentInfo = _parent != null ? $", Parent: {_parent.Name}" : "";
        var childrenInfo = _children.Count > 0 ? $", Children: {_children.Count}" : "";
        var componentCount = _world?.GetComponentCountForEntity(Id) ?? 0;
        var componentsInfo = componentCount > 0 ? $", Components: {componentCount}" : "";
        var behaviorsInfo = _behaviors.Count > 0 ? $", Behaviors: {_behaviors.Count}" : "";
        return $"Entity {Name} ({Id}) - Active: {IsActive}, Tags: {_tags.Count}{componentsInfo}{behaviorsInfo}{parentInfo}{childrenInfo}";
    }
}