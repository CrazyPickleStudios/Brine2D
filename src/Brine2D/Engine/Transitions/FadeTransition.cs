using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Engine.Transitions;

/// <summary>
/// A simple fade transition between scenes.
/// Fades to a solid color and back.
/// </summary>
public class FadeTransition : ISceneTransition
{
    private readonly Color _color;
    private float _elapsed;
    
    public float Duration { get; }
    public bool IsComplete => _elapsed >= Duration;
    public float Progress => Math.Clamp(_elapsed / Duration, 0f, 1f);
    
    /// <summary>
    /// Creates a new fade transition.
    /// </summary>
    /// <param name="duration">Duration in seconds (half for fade out, half for fade in).</param>
    /// <param name="color">Color to fade to/from.</param>
    public FadeTransition(float duration = 1f, Color? color = null)
    {
        Duration = duration;
        _color = color ?? Color.Black;
    }
    
    public void Begin()
    {
        _elapsed = 0f;
    }
    
    public void Update(GameTime gameTime)
    {
        _elapsed += (float)gameTime.DeltaTime;
    }
    
    public void Render(IRenderer? renderer)
    {
        if (renderer == null || IsComplete) return;
        
        // Calculate alpha based on progress
        // 0.0 -> 0.5: Stay opaque (alpha 255) while scene loads
        // 0.5 -> 1.0: Fade in (alpha 255 -> 0) to reveal new scene
        float normalizedProgress = Progress;
        byte alpha;
        
        if (normalizedProgress < 0.5f)
        {
            // Stay fully opaque during first half (scene loading)
            alpha = 255;
        }
        else
        {
            // Fade in during second half (reveal new scene)
            alpha = (byte)((1f - (normalizedProgress - 0.5f) * 2f) * 255f);
        }
        
        // Draw fullscreen overlay
        var fadeColor = new Color(_color.R, _color.G, _color.B, alpha);
        
        var viewportWidth = renderer.Camera?.ViewportWidth ?? 1280;
        var viewportHeight = renderer.Camera?.ViewportHeight ?? 720;
        
        renderer.DrawRectangleFilled(0, 0, viewportWidth, viewportHeight, fadeColor);
    }
}