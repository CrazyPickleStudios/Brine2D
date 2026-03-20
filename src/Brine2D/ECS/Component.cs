using Brine2D.Core;
using Brine2D.ECS.Components;
using Brine2D.Rendering;

namespace Brine2D.ECS;

/// <summary>
/// Base class for all components - pure data containers.
/// For logic, use EntityBehavior or Systems.
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
    /// </remarks>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            Entity?.NotifyComponentEnabledChanged();
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
}