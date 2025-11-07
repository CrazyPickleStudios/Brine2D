using Brine2D.Core.Hosting;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Scene;

/// <summary>
///     Central coordinator for scene lifecycle and per-frame delegation.
///     Ensures clean transitions by unloading the previous scene before initializing the next.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Call <see cref="Load(Scene, IEngineContext)" /> to switch scenes.</description></item>
///         <item><description>Invoke <see cref="Update(GameTime)" /> and <see cref="Draw(GameTime)" /> once per frame from the main loop.</description></item>
///         <item><description>Thread-safety is not guaranteed; intended for main-thread usage.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     var scenes = new SceneManager();
///     var mainMenu = new Scene();
///     scenes.Load(mainMenu, engine);
///
///     while (!engine.Window.IsClosing)
///     {
///         var time = /* produce GameTime */;
///         scenes.Update(time);
///         scenes.Draw(time);
///     }
///     </code>
/// </example>
public sealed class SceneManager
{
    /// <summary>
    ///     Currently active scene or null if none has been loaded.
    /// </summary>
    private Scene? _current;

    /// <summary>
    ///     Gets the currently active scene.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no scene has been loaded.</exception>
    public Scene Current => _current ?? throw new InvalidOperationException("No scene loaded.");

    /// <summary>
    ///     Forwards per-frame draw to the active scene, if any.
    /// </summary>
    /// <param name="time">Per-frame timing data.</param>
    public void Draw(GameTime time)
    {
        _current?.Draw(time);
    }

    /// <summary>
    ///     Loads the specified <paramref name="scene" />, unloading any previously active scene,
    ///     and initializes it using the provided <paramref name="engine" /> context.
    /// </summary>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="engine">The engine context providing shared services.</param>
    /// <remarks>
    ///     <para>Order of operations:</para>
    ///     <list type="number">
    ///         <item><description>Unload the existing scene (if any).</description></item>
    ///         <item><description>Assign the new scene as current.</description></item>
    ///         <item><description>Initialize the new scene with the engine context.</description></item>
    ///     </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scene" /> or <paramref name="engine" /> is null.</exception>
    public void Load(Scene scene, IEngineContext engine)
    {
        if (scene is null) throw new ArgumentNullException(nameof(scene));
        if (engine is null) throw new ArgumentNullException(nameof(engine));

        _current?.Unload();
        _current = scene;
        _current.Initialize(engine);
    }

    /// <summary>
    ///     Forwards per-frame update to the active scene, if any.
    /// </summary>
    /// <param name="time">Per-frame timing data.</param>
    public void Update(GameTime time)
    {
        _current?.Update(time);
    }
}