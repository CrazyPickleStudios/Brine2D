using Brine2D.Rendering;
using Brine2D.Core;

namespace Brine2D.Engine;

/// <summary>
/// Represents a transition effect between scenes.
/// Transitions run during scene loading to provide visual feedback.
/// </summary>
public interface ISceneTransition
{
    /// <summary>
    /// Gets the duration of the transition in seconds.
    /// </summary>
    float Duration { get; }
    
    /// <summary>
    /// Gets whether the transition is complete.
    /// </summary>
    bool IsComplete { get; }
    
    /// <summary>
    /// Gets the current progress (0.0 to 1.0).
    /// </summary>
    float Progress { get; }
    
    /// <summary>
    /// Called when the transition starts.
    /// </summary>
    void Begin();
    
    /// <summary>
    /// Updates the transition.
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds.</param>
    void Update(GameTime gameTime);
    
    /// <summary>
    /// Renders the transition effect.
    /// Renderer may be null in headless scenarios.
    /// </summary>
    void Render(IRenderer? renderer);
}