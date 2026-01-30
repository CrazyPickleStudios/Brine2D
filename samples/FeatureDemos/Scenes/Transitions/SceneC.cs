using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Transitions;

/// <summary>
/// Scene variant C - inherits all behavior from TransitionDemoScene
/// This one has a simulated heavy load in OnLoadAsync
/// </summary>
public class SceneC : TransitionDemoScene
{
    public SceneC(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext) 
        : base(input, sceneManager, gameContext)
    {
    }
    
    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // Simulate heavy asset loading (for demo purposes)
        await Task.Delay(2000, cancellationToken);
        await base.OnLoadAsync(cancellationToken);
    }
}