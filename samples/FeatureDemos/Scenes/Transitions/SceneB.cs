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
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext) 
        : base(input, sceneManager, gameContext)
    {
    }
}