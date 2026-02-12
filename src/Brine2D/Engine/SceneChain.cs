namespace Brine2D.Engine;

/// <summary>
/// Represents a sequence of scene transitions.
/// </summary>
/// <example>
/// <code>
/// // Define a scene chain
/// var chain = new SceneChain()
///     .Then&lt;CutsceneScene&gt;()
///     .Then&lt;GameScene&gt;()
///     .Then&lt;GameOverScene&gt;();
/// 
/// // Execute the chain
/// await sceneManager.LoadSceneChainAsync(chain);
/// </code>
/// </example>
public class SceneChain
{
    private readonly List<(Type SceneType, ISceneTransition? Transition)> _scenes = new();
    
    /// <summary>
    /// Adds a scene to the chain.
    /// </summary>
    public SceneChain Then<TScene>(ISceneTransition? transition = null) where TScene : Scene
    {
        _scenes.Add((typeof(TScene), transition));
        return this;
    }
    
    /// <summary>
    /// Gets the scene types in this chain.
    /// </summary>
    internal IReadOnlyList<(Type SceneType, ISceneTransition? Transition)> Scenes => _scenes;
}