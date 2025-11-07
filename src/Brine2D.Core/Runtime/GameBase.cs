using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Runtime;

/// <summary>
///     Minimal base implementation of <see cref="IGame" />.
///     Provides a stored <see cref="IEngineContext" /> after <see cref="Initialize(IEngineContext)" />
///     and no-op <see cref="Update(GameTime)" /> / <see cref="Draw(GameTime)" /> methods for derived games to override.
/// </summary>
/// <remarks>
///     Derive from this class to build your game loop. Override <see cref="Initialize(IEngineContext)" />,
///     <see cref="Update(GameTime)" />, and/or <see cref="Draw(GameTime)" /> as needed.
///     When overriding <see cref="Initialize(IEngineContext)" />, call <c>base.Initialize(context)</c>
///     to ensure <see cref="Engine" /> is assigned.
/// </remarks>
public abstract class GameBase : IGame
{
    /// <summary>
    ///     Aggregated engine services for the current game instance.
    ///     Set during <see cref="Initialize(IEngineContext)" /> and non-null afterwards.
    ///     Accessible to derived classes for content, input, rendering, and window access.
    /// </summary>
    protected IEngineContext Engine { get; private set; } = default!;

    /// <summary>
    ///     Per-frame render hook. Override to submit draw commands using <see cref="IEngineContext.Renderer" />
    ///     and/or <see cref="IEngineContext.Sprites" />.
    /// </summary>
    /// <param name="time">
    ///     Timing information for the frame, typically mirrored from <see cref="Update(GameTime)" /> for consistency.
    /// </param>
    public virtual void Draw(GameTime time)
    {
    }

    /// <summary>
    ///     Assigns the engine <paramref name="context" /> for later use by the game.
    ///     Override to perform game-specific initialization and call <c>base.Initialize(context)</c>.
    /// </summary>
    /// <param name="context">Engine context providing content, input, rendering, and window services.</param>
    public virtual void Initialize(IEngineContext context)
    {
        Engine = context;
    }

    /// <summary>
    ///     Per-frame update hook. Override to advance simulation, process input, and update game state.
    /// </summary>
    /// <param name="time">
    ///     Timing information for the frame, including <see cref="GameTime.DeltaSeconds" />
    ///     and <see cref="GameTime.TotalSeconds" />.
    /// </param>
    public virtual void Update(GameTime time)
    {
    }
}