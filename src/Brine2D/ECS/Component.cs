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
    public Entity? Entity { get; internal set; }

    /// <summary>
    /// Whether this component is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Called when the component is added to an entity.
    /// </summary>
    protected internal virtual void OnAdded() { }

    /// <summary>
    /// Called when the component is removed from an entity.
    /// </summary>
    protected internal virtual void OnRemoved() { }
}