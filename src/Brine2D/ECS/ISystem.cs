using Brine2D.Core;

namespace Brine2D.ECS;

/// <summary>
/// Interface for systems that process entities in the ECS.
/// Systems run before behaviors during EntityWorld.Update().
/// </summary>
/// <remarks>
/// Systems are scene-scoped and automatically cleaned up when the scene unloads.
/// Use systems for batch processing of many entities (physics, collision, etc.).
/// For entity-specific logic, use EntityBehavior instead.
/// </remarks>
public interface ISystem
{
    /// <summary>
    /// Whether this system is currently enabled.
    /// Disabled systems are skipped during Update().
    /// </summary>
    bool IsEnabled { get; set; }
}
