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

/// <summary>
/// Interface for systems that update game logic.
/// </summary>
public interface IUpdateSystem : ISystem
{
    /// <summary>
    /// Execution order for this system (lower values execute first).
    /// </summary>
    int UpdateOrder { get; }

    /// <summary>
    /// Updates the system for the current frame.
    /// </summary>
    void Update(GameTime gameTime);
}