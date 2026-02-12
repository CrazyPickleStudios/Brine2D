using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Rendering;

/// <summary>
/// Extension methods for IRenderer to simplify common operations.
/// </summary>
public static class RendererExtensions
{
    /// <summary>
    /// Set a scissor rectangle from position and size vectors.
    /// </summary>
    public static void SetScissorRect(this IRenderer renderer, Vector2 position, Vector2 size)
    {
        renderer.SetScissorRect(new Rectangle(
            (int)position.X, 
            (int)position.Y, 
            (int)size.X, 
            (int)size.Y));
    }
    
    /// <summary>
    /// Execute an action with a temporary scissor rect.
    /// The previous scissor rect is automatically restored after the action.
    /// </summary>
    /// <example>
    /// <code>
    /// renderer.WithScissorRect(panelBounds, () =>
    /// {
    ///     // All rendering here is clipped to panelBounds
    ///     RenderPanelContent();
    /// });
    /// // Scissor rect automatically restored here
    /// </code>
    /// </example>
    public static void WithScissorRect(this IRenderer renderer, Rectangle? rect, Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        
        renderer.PushScissorRect(rect);
        try
        {
            action();
        }
        finally
        {
            renderer.PopScissorRect();
        }
    }
    
    /// <summary>
    /// Intersect the current scissor rect with a new rect (nested clipping).
    /// </summary>
    /// <remarks>
    /// This is useful for nested UI elements where child clipping should
    /// be constrained by parent clipping regions.
    /// </remarks>
    public static void PushIntersectedScissorRect(this IRenderer renderer, Rectangle rect)
    {
        var current = renderer.GetScissorRect();
        
        Rectangle? intersected;
        if (current.HasValue)
        {
            // Calculate intersection
            var c = current.Value;
            var left = Math.Max(c.X, rect.X);
            var top = Math.Max(c.Y, rect.Y);
            var right = Math.Min(c.X + c.Width, rect.X + rect.Width);
            var bottom = Math.Min(c.Y + c.Height, rect.Y + rect.Height);
            
            if (right > left && bottom > top)
            {
                intersected = new Rectangle(left, top, right - left, bottom - top);
            }
            else
            {
                // No intersection - use empty rect
                intersected = new Rectangle(0, 0, 0, 0);
            }
        }
        else
        {
            intersected = rect;
        }
        
        renderer.PushScissorRect(intersected);
    }
}