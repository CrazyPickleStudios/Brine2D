using Brine2D.Core;
using Brine2D.ECS.Components;

namespace Brine2D.ECS;

/// <summary>
/// Base class for all components in the object-based ECS.
/// Components add behavior and/or data to entities.
/// Supports both component-to-component and system-oriented patterns.
/// </summary>
public abstract class Component
{
    private bool _isEnabled = true;

    /// <summary>
    /// The entity this component is attached to.
    /// </summary>
    public Entity? Entity { get; internal set; }

    /// <summary>
    /// Gets whether this component is attached to an entity.
    /// </summary>
    public bool IsAttached => Entity != null;

    /// <summary>
    /// Whether this component is enabled and should be updated.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;

                // Fire lifecycle events
                if (_isEnabled)
                {
                    OnEnabled();
                }
                else
                {
                    OnDisabled();
                }
            }
        }
    }

    /// <summary>
    /// Gets the name of the entity this component is attached to.
    /// </summary>
    public string EntityName => Entity?.Name ?? string.Empty;

    /// <summary>
    /// Gets the tags of the entity this component is attached to.
    /// </summary>
    public HashSet<string> EntityTags => Entity?.Tags ?? new HashSet<string>();

    /// <summary>
    /// Gets the Transform component (shortcut for GetComponent&lt;TransformComponent&gt;).
    /// </summary>
    public TransformComponent? Transform => Entity?.GetComponent<TransformComponent>();

    /// <summary>
    /// Throws if this component is not attached to an entity.
    /// </summary>
    private void ThrowIfNotAttached()
    {
        if (!IsAttached)
        {
            throw new InvalidOperationException(
                $"Component {GetType().Name} is not attached to an entity.");
        }
    }

    /// <summary>
    /// Gets a sibling component (another component on the same entity).
    /// </summary>
    public T? GetComponent<T>() where T : Component
    {
        return Entity?.GetComponent<T>();
    }

    /// <summary>
    /// Gets a required sibling component, throwing if missing (ASP.NET GetRequiredService pattern).
    /// </summary>
    public T GetRequiredComponent<T>() where T : Component
    {
        ThrowIfNotAttached();
        return Entity!.GetRequiredComponent<T>();
    }

    /// <summary>
    /// Tries to get a sibling component (safe retrieval pattern).
    /// </summary>
    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = Entity?.GetComponent<T>();
        return component != null;
    }

    /// <summary>
    /// Gets a component in this entity or its children (Unity pattern).
    /// </summary>
    public T? GetComponentInChildren<T>() where T : Component
    {
        return Entity?.GetComponentInChildren<T>();
    }

    /// <summary>
    /// Gets a component in this entity or its parent (Unity pattern).
    /// </summary>
    public T? GetComponentInParent<T>() where T : Component
    {
        return Entity?.GetComponentInParent<T>();
    }

    /// <summary>
    /// Removes this component from its entity.
    /// </summary>
    public void Destroy()
    {
        ThrowIfNotAttached();
        Entity!.RemoveComponent(GetType());
    }

    /// <summary>
    /// Called when the component is added to an entity.
    /// </summary>
    protected internal virtual void OnAdded() { }

    /// <summary>
    /// Called when the component is removed from an entity.
    /// </summary>
    protected internal virtual void OnRemoved() { }

    /// <summary>
    /// Called when the component is enabled.
    /// </summary>
    protected internal virtual void OnEnabled() { }

    /// <summary>
    /// Called when the component is disabled.
    /// </summary>
    protected internal virtual void OnDisabled() { }

    /// <summary>
    /// Called every frame to update component logic.
    /// </summary>
    protected internal virtual void OnUpdate(GameTime gameTime) { }
}