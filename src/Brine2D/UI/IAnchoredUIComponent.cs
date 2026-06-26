using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Extension interface for UI components that position themselves relative to a
/// screen anchor. Components that do not implement this are treated as
/// <see cref="UIAnchor.TopLeft"/> with no offset.
/// </summary>
public interface IAnchoredUIComponent : IUIComponent
{
    /// <summary>
    /// The screen anchor point this component is positioned relative to.
    /// </summary>
    UIAnchor Anchor { get; set; }

    /// <summary>
    /// Pixel offset from the resolved anchor point.
    /// </summary>
    Vector2 AnchorOffset { get; set; }
}