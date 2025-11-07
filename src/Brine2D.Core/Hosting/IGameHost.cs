using Brine2D.Core.Runtime;

namespace Brine2D.Core.Hosting;

/// <summary>
///     Hosts and runs a game on the engine's main thread.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Creates or acquires the <see cref="IEngineContext" /> needed by the game.</description>
///         </item>
///         <item>
///             <description>Invokes <see cref="IGame.Initialize(IEngineContext)" /> exactly once before the main loop.</description>
///         </item>
///         <item>
///             <description>
///                 Drives the main loop that computes <see cref="GameTime" />, calls
///                 <see cref="IGame.Update(GameTime)" />, and <see cref="IGame.Draw(GameTime)" /> each frame.
///             </description>
///         </item>
///         <item>
///             <description>Pumps OS/window messages and presents frames through the renderer.</description>
///         </item>
///     </list>
///     <para>
///         The <see cref="Run(IGame)" /> call is typically blocking and should be invoked from the thread that owns the
///         window/rendering context.
///     </para>
/// </remarks>
public interface IGameHost
{
    /// <summary>
    ///     Starts the game loop for the specified <paramref name="game" /> using this host's engine context.
    /// </summary>
    /// <param name="game">The game that will be initialized and driven by the host.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game" /> is <c>null</c>.</exception>
    /// <remarks>
    ///     Typical call sequence:
    ///     <list type="number">
    ///         <item>
    ///             <description>Create or acquire an <see cref="IEngineContext" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>Call <see cref="IGame.Initialize(IEngineContext)" /> once.</description>
    ///         </item>
    ///         <item>
    ///             <description>Enter main loop:</description>
    ///         </item>
    ///     </list>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Compute <see cref="GameTime" /> (total and delta seconds).</description>
    ///         </item>
    ///         <item>
    ///             <description>Poll/update input and window state.</description>
    ///         </item>
    ///         <item>
    ///             <description>Call <see cref="IGame.Update(GameTime)" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>Begin a frame on the renderer, call <see cref="IGame.Draw(GameTime)" />, then present.</description>
    ///         </item>
    ///         <item>
    ///             <description>Exit when the window requests close or the host is signaled to stop.</description>
    ///         </item>
    ///     </list>
    ///     <list type="number">
    ///         <item>
    ///             <description>Perform shutdown and resource cleanup.</description>
    ///         </item>
    ///     </list>
    ///     <para>This method usually blocks until the game exits and should be called on the engine's main thread.</para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     IGameHost host = new DesktopGameHost();
    ///     host.Run(new MyGame());
    ///     </code>
    /// </example>
    void Run(IGame game);
}