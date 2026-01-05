using Brine2D.Core;

namespace Brine2D.ECS;

/// <summary>
/// Base class for all components in the object-based ECS.
/// Components add behavior and data to entities.
/// </summary>
public abstract class Component
{
    private bool _isEnabled = true;

    /// <summary>
    /// The entity this component is attached to.
    /// </summary>
    public Entity? Entity { get; internal set; }

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