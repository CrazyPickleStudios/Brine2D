using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Runtime;

/// <summary>
///     Defines the core lifecycle contract for a Brine2D game.
///     The engine invokes <see cref="Initialize(IEngineContext)" />, then repeatedly calls
///     <see cref="Update(GameTime)" /> followed by <see cref="Draw(GameTime)" /> each frame.
/// </summary>
/// <remarks>
///     <para>Threading:</para>
///     <list type="bullet">
///         <item><description>Methods are expected to be called on the engine's main thread.</description></item>
///     </list>
///     <para>Responsibilities:</para>
///     <list type="bullet">
///         <item><description><b>Initialize</b>: acquire engine services, load content, set initial state.</description></item>
///         <item><description><b>Update</b>: advance simulation using <see cref="GameTime.DeltaSeconds" /> and query input via <see cref="IEngineContext.Input" />, <see cref="IEngineContext.Gamepads" />, and <see cref="IEngineContext.Mouse" />.</description></item>
///         <item><description><b>Draw</b>: submit rendering commands using <see cref="IEngineContext.Renderer" /> and <see cref="IEngineContext.Sprites" />.</description></item>
///     </list>
///     <para>Performance:</para>
///     <list type="bullet">
///         <item><description>Avoid long-running or blocking work in <see cref="Update(GameTime)" /> and <see cref="Draw(GameTime)" />.</description></item>
///         <item><description>Prefer loading/caching assets via <see cref="IEngineContext.Content" /> during initialization.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     public sealed class BasicGame : IGame
///     {
///         private IEngineContext _ctx = default!;
///
///         public void Initialize(IEngineContext context)
///         {
///             _ctx = context;
///             _ctx.Window.Title = "Brine2D - Basic Game";
///             // Load content:
///             // var texture = _ctx.Content.Load&lt;ITexture2D&gt;("textures/player.png");
///         }
///
///         public void Update(GameTime time)
///         {
///             // Read input and advance simulation using time.DeltaSeconds
///         }
///
///         public void Draw(GameTime time)
///         {
///             _ctx.Renderer.Clear(Color.Black);
///             // _ctx.Sprites.Begin();
///             // _ctx.Sprites.Draw(texture, null, new Rectangle(100, 100, 64, 64), Color.White);
///             // _ctx.Sprites.End();
///             _ctx.Renderer.Present();
///         }
///     }
///     </code>
/// </example>
public interface IGame
{
    /// <summary>
    ///     Renders the current frame. Issue draw calls using <see cref="IEngineContext.Renderer" /> and
    ///     <see cref="IEngineContext.Sprites" />. Avoid heavy allocations or blocking I/O.
    /// </summary>
    /// <param name="time">
    ///     Time information for the current frame. Useful for time-based visual effects.
    /// </param>
    void Draw(GameTime time);

    /// <summary>
    ///     Called once before the first frame to set up the game.
    ///     Use this to capture the <see cref="IEngineContext" /> and load required content.
    /// </summary>
    /// <param name="context">
    ///     Engine context providing access to content, input, rendering, and window services.
    /// </param>
    void Initialize(IEngineContext context);

    /// <summary>
    ///     Advances game simulation and processes input for the current frame.
    /// </summary>
    /// <param name="time">
    ///     Time information for the current frame. Use <see cref="GameTime.DeltaSeconds" /> for frame-step logic
    ///     and <see cref="GameTime.TotalSeconds" /> for absolute timeline queries.
    /// </param>
    void Update(GameTime time);
}