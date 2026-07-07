using Brine2D.Core;
using Brine2D.ECS.Components;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for all components - pure data containers.
/// For logic, use Behavior or Systems.
/// </summary>
public abstract class Component
{
    /// <summary>
    /// The entity this component is attached to.
    /// </summary>
    /// <remarks>
    /// Set to <c>null</c> when the component is removed from its entity via
    /// <see cref="Entity.RemoveComponent{T}"/> or entity destruction.
    /// If you cache a component reference beyond the current frame, check this
    /// property before accessing entity state through it.
    /// </remarks>
    public Entity? Entity { get; internal set; }

    private bool _isEnabled = true;

    /// <summary>
    /// Whether this component is enabled. Checked by built-in systems
    /// (rendering, physics, collision, audio, AI, particles) to skip
    /// processing of individual components without removing them.
    /// Custom systems should check this property when processing components.
    /// </summary>
    /// <remarks>
    /// This is distinct from <see cref="Entity.IsActive"/>: toggling
    /// <c>IsActive</c> disables the entire entity and all its components;
    /// toggling <c>IsEnabled</c> disables a single component while the
    /// entity and its other components continue processing.
    /// Cached queries built with <c>OnlyEnabled()</c> are automatically
    /// invalidated when this value changes.
    /// When changed, <see cref="OnEnabled"/> or <see cref="OnDisabled"/> is called
    /// so that subclasses can react without overriding the property.
    /// </remarks>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            Entity?.NotifyEnabledChanged();
            if (_isEnabled)
                OnEnabled();
            else
                OnDisabled();
        }
    }

    /// <summary>
    /// Called when this component transitions from disabled to enabled
    /// (i.e., <see cref="IsEnabled"/> changes to <see langword="true"/>).
    /// Also called when the owning entity transitions from inactive to active
    /// (<see cref="Entity.IsActive"/> changes to <see langword="true"/>) and this
    /// component's <see cref="IsEnabled"/> is already <see langword="true"/>.
    /// Override to resume state or restart effects.
    /// </summary>
    protected internal virtual void OnEnabled() { }

    /// <summary>
    /// Called when this component transitions from enabled to disabled
    /// (i.e., <see cref="IsEnabled"/> changes to <see langword="false"/>).
    /// Also called when the owning entity transitions from active to inactive
    /// (<see cref="Entity.IsActive"/> changes to <see langword="false"/>) and this
    /// component's <see cref="IsEnabled"/> is already <see langword="true"/>.
    /// Override to pause state, stop effects, or clear accumulators.
    /// </summary>
    protected internal virtual void OnDisabled() { }

    /// <summary>
    /// Called when the component is added to an entity.
    /// </summary>
    protected internal virtual void OnAdded() { }

    /// <summary>
    /// Called when the component is removed from an entity.
    /// </summary>
    protected internal virtual void OnRemoved() { }
}