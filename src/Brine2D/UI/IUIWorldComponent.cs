using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Extends <see cref="IUIComponent"/> for components positioned in world space.
/// <see cref="UICanvas"/> projects <see cref="WorldPosition"/> to screen coordinates
/// each frame via <see cref="UICanvas.WorldCamera"/> and writes the result into
/// <see cref="IUIComponent.Position"/> before rendering.
/// </summary>
/// <remarks>
/// World components do not participate in tab-focus, drag-and-drop, or normal
/// mouse-input dispatch. They render as overlays on top of all regular components.
/// </remarks>
public interface IUIWorldComponent : IUIComponent
{
    /// <summary>
    /// Position in world space. Projected to screen coordinates via
    /// <see cref="UICanvas.WorldCamera"/> before each render pass.
    /// </summary>
    Vector2 WorldPosition { get; set; }

    /// <summary>
    /// Pixel offset applied after world-to-screen projection. Use to nudge the component
    /// relative to its anchor — e.g. <c>new Vector2(-Size.X / 2, -40)</c> to centre it
    /// 40 px above the world point.
    /// </summary>
    Vector2 ScreenOffset { get; set; }

    /// <summary>
    /// Hides the component when its projected position falls outside the viewport.
    /// Defaults to <c>true</c>.
    /// </summary>
    bool CullWhenOffScreen { get; set; }
}
