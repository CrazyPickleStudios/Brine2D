using Brine2D.ECS;

namespace Brine2D.Engine;

/// <summary>
/// Base class for game scenes. Override OnLoadAsync, OnEnter, OnUpdate, OnRender,
/// OnExit, and OnUnloadAsync to implement scene logic.
/// Framework properties (Logger, World, Renderer, Input, Audio, Game)
/// are set automatically before OnLoadAsync is called.
/// </summary>
/// <example>
/// <code>
/// protected override void OnEnter()
/// {
///     var player = World.CreateEntity("Player")
///         .AddComponent&lt;TransformComponent&gt;()
///         .AddBehavior&lt;PlayerMovementBehavior&gt;();
///
///     Audio.PlayMusic("theme.ogg");
///
///     World.GetSystem&lt;ParticleSystem&gt;()!.IsEnabled = false;
/// }
/// </code>
/// </example>
public abstract class Scene : SceneBase
{
    private IEntityWorld? _world;

    /// <summary>
    /// Entity world for this scene. Available from OnLoadAsync onwards.
    /// Virtual to allow test doubles to substitute a lightweight world without a real ECS setup.
    /// </summary>
    protected internal virtual IEntityWorld World
    {
        get => _world ?? throw NotReady(nameof(World));
        internal set => _world = value;
    }

    protected Scene() { }
}