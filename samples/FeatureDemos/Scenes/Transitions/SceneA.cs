using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Scene variant A - inherits all behavior from TransitionDemoScene
/// </summary>
public class SceneA : TransitionDemoScene
{
    public SceneA(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<SceneA> logger) 
        : base(renderer, input, sceneManager, gameContext, logger)
    {
    }
}