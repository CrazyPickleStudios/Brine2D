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
    /// excluded from queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is <b>non-cascading</b>: toggling a parent's
    /// <c>IsActive</c> does not automatically propagate to children.
    /// Child entities retain their own <c>IsActive</c> state and will
    /// continue to be processed even when a parent is deactivated.
    /// </para>
    /// <para>
    /// To toggle an entire hierarchy together, use
    /// <see cref="SetActiveHierarchically"/> which cascades the value
    /// down through all descendants.
    /// </para>
    /// <para>
    /// When changed, <see cref="OnActivated"/> or <see cref="OnDeactivated"/>
    /// is called on this entity, and cached queries that filter by active state
    /// are invalidated. Additionally, <see cref="Component.OnEnabled"/> /
    /// <see cref="Component.OnDisabled"/> is called on each currently-enabled component,
    /// and <see cref="Behavior.OnEnabled"/> / <see cref="Behavior.OnDisabled"/> is called
    /// on each currently-enabled behavior.
    /// </para>
    /// </remarks>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            _world?.NotifyActiveChanged();
            if (_isActive)
            {
                OnActivated();
                PropagateEnabledToComponents();
                PropagateEnabledToBehaviors();
            }
            else
            {
                OnDeactivated();
                PropagateDisabledToComponents();
                PropagateDisabledToBehaviors();
            }
        }
    }

    private void PropagateEnabledToComponents()
    {
        if (_world == null) return;
        var count = _world.GetComponentCountForEntity(Id);
        if (count == 0) return;
        var snapshot = ArrayPool<Component>.Shared.Rent(count);
        try
        {
            int i = 0;
            foreach (var component in _world.GetAllComponentsFromPool(Id))
                snapshot[i++] = component;
            for (int j = 0; j < count; j++)
                if (snapshot[j].IsEnabled) snapshot[j].OnEnabled();
        }
        finally { ArrayPool<Component>.Shared.Return(snapshot, clearArray: true); }
    }

    private void PropagateDisabledToComponents()
    {
        if (_world == null) return;
        var count = _world.GetComponentCountForEntity(Id);
        if (count == 0) return;
        var snapshot = ArrayPool<Component>.Shared.Rent(count);
        try
        {
            int i = 0;
            foreach (var component in _world.GetAllComponentsFromPool(Id))
                snapshot[i++] = component;
            for (int j = 0; j < count; j++)
                if (snapshot[j].IsEnabled) snapshot[j].OnDisabled();
        }
        finally { ArrayPool<Component>.Shared.Return(snapshot, clearArray: true); }
    }

    private void PropagateEnabledToBehaviors()
    {
        var count = _behaviors.Count;
        if (count == 0) return;
        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            _behaviors.CopyTo(snapshot, 0);
            for (int i = 0; i < count; i++)
                if (snapshot[i].IsEnabled) snapshot[i].OnEnabled();
        }
        finally { ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true); }
    }

    private void PropagateDisabledToBehaviors()
    {
        var count = _behaviors.Count;
        if (count == 0) return;
        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            _behaviors.CopyTo(snapshot, 0);
            for (int i = 0; i < count; i++)
                if (snapshot[i].IsEnabled) snapshot[i].OnDisabled();
        }
        finally { ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true); }
    }

    /// <summary>
    /// Sets <see cref="IsActive"/> without notifying the world.
    /// Used during entity destruction to avoid O(n × q) redundant
    /// query invalidations that will be superseded by pool removal.
    /// </summary>
    internal void SetActiveWithoutNotification(bool value) => _isActive = value;

    /// <summary>
    /// Forwards <see cref="Component.IsEnabled"/> and <see cref="Behavior.IsEnabled"/> change
    /// notifications to the world so cached queries built with <c>OnlyEnabled()</c> are invalidated.
    /// </summary>
    internal void NotifyEnabledChanged() => _world?.NotifyEnabledChanged();

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
    protected internal virtual void OnInitialize()
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
    protected internal virtual void OnDestroy()
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
                    behaviorSnapshot[i].OnDestroyed();
                    behaviorSnapshot[i].OnRemoved();
                    behaviorSnapshot[i].Entity = null;
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

    /// <summary>
    /// Called when the entity transitions from inactive to active
    /// (i.e., <see cref="IsActive"/> changes to <see langword="true"/>).
    /// Override to resume state or restart effects.
    /// </summary>
    protected internal virtual void OnActivated() { }

    /// <summary>
    /// Called when the entity transitions from active to inactive
    /// (i.e., <see cref="IsActive"/> changes to <see langword="false"/>).
    /// Override to pause state, stop effects, or release transient resources.
    /// </summary>
    protected internal virtual void OnDeactivated() { }

    /// <summary>
    /// Sets <see cref="IsActive"/> on this entity and all of its descendants.
    /// Use this to toggle an entire hierarchy in one call instead of walking
    /// children manually.
    /// </summary>
    /// <remarks>
    /// Query invalidation is batched: the world's active-state notification is fired
    /// once after all descendants are updated rather than once per entity, avoiding
    /// O(entities × queries) redundant invalidations.
    /// </remarks>
    /// <param name="active">The active state to apply.</param>
    /// <returns>This entity for method chaining.</returns>
    public Entity SetActiveHierarchically(bool active)
    {
        SetActiveWithoutWorldNotify(active);
        foreach (var descendant in GetDescendants())
            descendant.SetActiveWithoutWorldNotify(active);
        _world?.NotifyActiveChanged();
        return this;
    }

    private void SetActiveWithoutWorldNotify(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;
        if (_isActive)
        {
            OnActivated();
            PropagateEnabledToComponents();
            PropagateEnabledToBehaviors();
        }
        else
        {
            OnDeactivated();
            PropagateDisabledToComponents();
            PropagateDisabledToBehaviors();
        }
    }

    #endregion

    #region Hierarchy Management

    /// <summary>
    /// Sets the parent of this entity.
    /// </summary>
    /// <remarks>
    /// Self-parent and circular-hierarchy attempts are silently ignored and this entity
    /// is returned unchanged. Cross-world parenting still throws because it indicates a
    /// clear programming error with no safe fallback.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="newParent"/> belongs to a different world than this entity.
    /// </exception>
    public Entity SetParent(Entity? newParent)
    {
        if (newParent == this)
        {
            _logger?.LogWarning("SetParent ignored: cannot set entity '{Name}' ({Id}) as its own parent.", Name, Id);
            return this;
        }

        if (newParent != null && newParent.IsDescendantOf(this))
        {
            _logger?.LogWarning("SetParent ignored: setting '{NewParent}' ({NewParentId}) as parent of '{Name}' ({Id}) would create a circular hierarchy.",
                newParent.Name, newParent.Id, Name, Id);
            return this;
        }

        if (newParent != null && _world != null && newParent._world != null && _world != newParent._world)
            throw new InvalidOperationException(
                $"Cannot parent '{Name}' ({Id}) to '{newParent.Name}' ({newParent.Id}) — entities belong to different worlds.");

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
    /// <remarks>
    /// Allocates a <see cref="Stack{T}"/> and yields each entity, so calling this in a hot loop
    /// (e.g., every frame) creates per-call garbage. Use <see cref="CollectDescendants"/> instead
    /// when you need a reusable buffer with zero per-call allocation.
    /// </remarks>
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
    /// Appends all descendant entities into <paramref name="results"/> via iterative
    /// depth-first traversal. Avoids allocating the result collection; use a pre-allocated
    /// or reused <see cref="List{T}"/> to reduce per-call heap pressure.
    /// </summary>
    /// <remarks>
    /// Prefer this over <see cref="GetDescendants"/> in hot paths (e.g., per-frame scene-graph
    /// walks) to avoid allocating a new list each call. Clear and reuse <paramref name="results"/>
    /// across calls. Note that traversal still allocates a <see cref="Stack{T}"/> internally
    /// for non-trivial hierarchies, so this method is not fully allocation-free.
    /// </remarks>
    /// <param name="results">The list to append descendants into. Not cleared by this method.</param>
    public void CollectDescendants(List<Entity> results)
    {
        if (_children.Count == 0) return;

        var stack = new Stack<Entity>();
        for (int i = _children.Count - 1; i >= 0; i--)
            stack.Push(_children[i]);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            results.Add(current);
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
    /// If the entity already has this component type, the call is silently ignored,
    /// consistent with the other <c>AddComponent</c> overloads.
    /// </summary>
    /// <remarks>
    /// <paramref name="configure"/> is invoked after <see cref="Component.Entity"/> has been set
    /// to this entity, so the owning entity is accessible inside the callback. Note that the
    /// component has not been added to the world's pools yet when <paramref name="configure"/> runs,
    /// so <see cref="HasComponent{T}"/> will return <see langword="false"/> for this component type
    /// inside the callback.
    /// </remarks>
    public Entity AddComponent<T>(Action<T>? configure) where T : Component, new()
    {
        if (HasComponent<T>())
        {
            _logger?.LogDebug("Entity {Name} ({Id}) already has component {Type}, skipping",
                Name, Id, typeof(T).Name);
            return this;
        }

        var component = new T();
        component.Entity = this;
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
    /// Also thrown when <paramref name="component"/> is already attached to a different entity;
    /// a component instance may only belong to one entity at a time.
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

        if (component.Entity != null && component.Entity != this)
            throw new ArgumentException(
                $"Component '{typeof(T).Name}' is already attached to entity '{component.Entity.Name}' ({component.Entity.Id}). " +
                "A component instance may only belong to one entity at a time. " +
                $"Remove it from its current owner before adding it here, or create a new instance.",
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
        NotifyBehaviorsComponentAdded(component);

        return this;
    }

    public T? GetComponent<T>() where T : Component
        => _world?.GetComponentFromPool<T>(this.Id);

    /// <summary>
    /// Gets a component of the specified type and returns whether it was found.
    /// Performs a single pool lookup, avoiding the double-lookup of
    /// <see cref="HasComponent{T}"/> followed by <see cref="GetComponent{T}"/>.
    /// </summary>
    public bool TryGetComponent<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? component) where T : Component
    {
        component = _world?.GetComponentFromPool<T>(this.Id);
        return component != null;
    }

    /// <summary>
    /// Gets a component of the specified type attached to this entity (non-generic).
    /// Used internally by the serialization layer when the concrete type is only
    /// known at runtime. Prefer the generic overload for all other uses.
    /// </summary>
    internal Component? GetComponent(Type componentType)
        => _world?.GetComponentOfType(this.Id, componentType);

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

        NotifyBehaviorsComponentRemoved(component);
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
        try { behavior.OnAdded(); }
        catch
        {
            behavior.Entity = null;
            throw;
        }

        _behaviors.Add(behavior);
        _world.NotifyBehaviorAdded(this, behavior);
        _logger?.LogDebug("Entity {Name} added behavior {Behavior}", Name, typeof(T).Name);

        return this;
    }

    /// <summary>
    /// Creates, configures, and adds a behavior of the specified type.
    /// </summary>
    /// <remarks>
    /// <paramref name="configure"/> is invoked after the behavior is constructed (via DI)
    /// and after <see cref="Behavior.Entity"/> has been set to this entity, but before
    /// <see cref="Behavior.OnAdded"/> is called, so both the owning entity and configured
    /// values are available during attachment.
    /// </remarks>
    /// <typeparam name="T">The behavior type.</typeparam>
    /// <param name="configure">An action to configure the behavior before it is attached.</param>
    /// <returns>This entity for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if entity is not in a world.</exception>
    public Entity AddBehavior<T>(Action<T> configure) where T : Behavior
    {
        if (_world == null)
            throw new InvalidOperationException("Cannot add behavior - entity is not in a world");

        if (HasBehavior<T>())
        {
            _logger?.LogWarning("Entity {Name} already has behavior {Type}, skipping", Name, typeof(T).Name);
            return this;
        }

        var behavior = _world.CreateBehavior<T>();
        behavior.Entity = this;
        configure(behavior);

        try { behavior.OnAdded(); }
        catch
        {
            behavior.Entity = null;
            throw;
        }

        _behaviors.Add(behavior);
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
    /// Gets a behavior of the specified type and returns whether it was found.
    /// Performs a single linear scan, avoiding the double-scan of
    /// <see cref="HasBehavior{T}"/> followed by <see cref="GetBehavior{T}"/>.
    /// </summary>
    public bool TryGetBehavior<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? behavior) where T : Behavior
    {
        foreach (var b in _behaviors)
        {
            if (b is T match)
            {
                behavior = match;
                return true;
            }
        }
        behavior = null;
        return false;
    }

    /// <summary>
    /// Gets all behaviors of the specified type (or a derived type) attached to this entity.
    /// </summary>
    /// <remarks>
    /// Returns a new list on every call. Avoid on hot paths; cache the result or use
    /// <see cref="GetAllBehaviors"/> and filter manually when performance matters.
    /// </remarks>
    public List<T> GetBehaviors<T>() where T : Behavior
    {
        var result = new List<T>();
        foreach (var b in _behaviors)
            if (b is T match) result.Add(match);
        return result;
    }

    /// <summary>
    /// Gets a behavior of the specified type on this entity or any of its children
    /// (depth-first recursive search).
    /// </summary>
    /// <remarks>
    /// This is O(depth × children × behaviors). Avoid calling on hot paths or deep hierarchies.
    /// </remarks>
    public T? GetBehaviorInChildren<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior != null)
            return behavior;

        foreach (var child in _children)
        {
            behavior = child.GetBehaviorInChildren<T>();
            if (behavior != null)
                return behavior;
        }

        return null;
    }

    /// <summary>
    /// Gets a behavior of the specified type on this entity or any of its ancestors.
    /// </summary>
    /// <remarks>
    /// This is O(depth × behaviors). Avoid calling on hot paths or deep hierarchies.
    /// </remarks>
    public T? GetBehaviorInParent<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior != null)
            return behavior;

        return _parent?.GetBehaviorInParent<T>();
    }

    /// <summary>
    /// Gets a behavior of the specified type, throwing if it is not found.
    /// </summary>
    public T GetRequiredBehavior<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior == null)
        {
            throw new InvalidOperationException(
                $"Entity '{Name}' ({Id}) does not have required behavior '{typeof(T).Name}'." + Environment.NewLine +
                Environment.NewLine +
                "Fix: Add the behavior before accessing it:" + Environment.NewLine +
                $"  entity.AddBehavior<{typeof(T).Name}>();" + Environment.NewLine +
                Environment.NewLine +
                $"Available behaviors: {string.Join(", ", _behaviors.Select(b => b.GetType().Name))}");
        }
        return behavior;
    }

    /// <summary>
    /// Removes a behavior from this entity.
    /// </summary>
    public bool RemoveBehavior<T>() where T : Behavior
    {
        var behavior = GetBehavior<T>();
        if (behavior == null)
            return false;

        behavior.OnRemoved();
        _behaviors.Remove(behavior);
        behavior.Entity = null;
        _world?.NotifyBehaviorRemoved(this, behavior);
        _logger?.LogDebug("Entity {Name} removed behavior {Behavior}", Name, typeof(T).Name);

        return true;
    }

    /// <summary>
    /// Gets all behaviors attached to this entity.
    /// </summary>
    public IReadOnlyList<Behavior> GetAllBehaviors() => _readOnlyBehaviors ??= _behaviors.AsReadOnly();

    #endregion

    private void NotifyBehaviorsComponentAdded(Component component)
    {
        var count = _behaviors.Count;
        if (count == 0) return;
        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            _behaviors.CopyTo(snapshot, 0);
            for (int i = 0; i < count; i++)
                snapshot[i].OnComponentAdded(component);
        }
        finally { ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true); }
    }

    private void NotifyBehaviorsComponentRemoved(Component component)
    {
        var count = _behaviors.Count;
        if (count == 0) return;
        var snapshot = ArrayPool<Behavior>.Shared.Rent(count);
        try
        {
            _behaviors.CopyTo(snapshot, 0);
            for (int i = 0; i < count; i++)
                snapshot[i].OnComponentRemoved(component);
        }
        finally { ArrayPool<Behavior>.Shared.Return(snapshot, clearArray: true); }
    }

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