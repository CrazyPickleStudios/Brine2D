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
    /// <param name="gameTime">Time information for the current frame.</param>
    /// <param name="world">The entity world to operate on.</param>
    void Update(GameTime gameTime, IEntityWorld world);
}