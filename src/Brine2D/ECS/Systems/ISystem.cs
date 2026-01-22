using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Base interface for all ECS systems.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Gets the name of this system (for debugging/logging).
    /// </summary>
    string Name => GetType().Name;
}