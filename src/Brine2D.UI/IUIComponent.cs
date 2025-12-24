using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Position in screen space (pixels).
    /// </summary>
    Vector2 Position { get; set; }

    /// <summary>
    /// Size in pixels.
    /// </summary>
    Vector2 Size { get; set; }

    /// <summary>
    /// Whether this component is visible.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Whether this component is enabled (can receive input).
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Optional tooltip for this component.
    /// </summary>
    UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Update component logic (animations, hover states, etc.).
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Render the component.
    /// </summary>
    void Render(IRenderer renderer);

    /// <summary>
    /// Check if a screen position is within this component's bounds.
    /// </summary>
    bool Contains(Vector2 screenPosition);
}