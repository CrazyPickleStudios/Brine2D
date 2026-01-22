using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
///     Interface for systems that update game logic.
/// </summary>
public interface IUpdateSystem : ISystem
{
    /// <summary>
    ///     Execution order for this system (lower values execute first).
    /// </summary>
    int UpdateOrder { get; }

    /// <summary>
    ///     Updates the system for the current frame.
    /// </summary>
    void Update(GameTime gameTime);
}