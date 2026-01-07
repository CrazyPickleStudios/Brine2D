using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Scene variant B - inherits all behavior from TransitionDemoScene
/// </summary>
public class SceneB : TransitionDemoScene
{
    public SceneB(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<SceneB> logger) 
        : base(renderer, input, sceneManager, gameContext, logger)
    {
    }
}