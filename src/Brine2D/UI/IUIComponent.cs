using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Optional name used to look up this component by <see cref="UICanvas.FindByName"/>.
    /// </summary>
    string? Name => null;
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
    /// Tab-focus order. Lower values receive focus first; equal values fall back to
    /// canvas add-order (last-added first). Defaults to <see cref="int.MaxValue"/>.
    /// </summary>
    int TabIndex { get; set; }

    /// <summary>
    /// Visual and input stacking order. Higher values render on top; equal values fall back
    /// to canvas add-order (last-added on top). Defaults to <c>0</c>.
    /// </summary>
    int ZOrder { get; set; }

    /// <summary>
    /// Optional tooltip for this component.
    /// </summary>
    UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Called each frame to advance component logic.
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Renders the component.
    /// </summary>
    void Render(IRenderer renderer);

    /// <summary>
    /// Returns true if <paramref name="screenPosition"/> is within this component's bounds.
    /// </summary>
    bool Contains(Vector2 screenPosition);
}