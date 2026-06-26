using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Helpers for resolving <see cref="UIAnchor"/> origins and computing hit-test
/// offsets for anchored components.
/// </summary>
internal static class UIAnchorResolver
{
    internal static Vector2 Resolve(UIAnchor anchor, float screenWidth, float screenHeight) =>
        anchor switch
        {
            UIAnchor.TopLeft => new Vector2(0, 0),
            UIAnchor.TopCenter => new Vector2(screenWidth / 2f, 0),
            UIAnchor.TopRight => new Vector2(screenWidth, 0),
            UIAnchor.MiddleLeft => new Vector2(0, screenHeight / 2f),
            UIAnchor.MiddleCenter => new Vector2(screenWidth / 2f, screenHeight / 2f),
            UIAnchor.MiddleRight => new Vector2(screenWidth, screenHeight / 2f),
            UIAnchor.BottomLeft => new Vector2(0, screenHeight),
            UIAnchor.BottomCenter => new Vector2(screenWidth / 2f, screenHeight),
            UIAnchor.BottomRight => new Vector2(screenWidth, screenHeight),
            _ => Vector2.Zero
        };

    /// <summary>
    /// Returns the resolved screen-space position for an anchored component without
    /// modifying its stored <see cref="IUIComponent.Position"/>.
    /// </summary>
    internal static Vector2 ResolveAnchoredPosition(IAnchoredUIComponent component, Vector2 screenSize)
    {
        var anchorOrigin = Resolve(component.Anchor, screenSize.X, screenSize.Y);
        return anchorOrigin + component.AnchorOffset;
    }

    /// <summary>
    /// Returns the offset to add to incoming screen positions before hit-testing
    /// an anchored component. Returns <see cref="Vector2.Zero"/> for non-anchored
    /// components and for <see cref="UIAnchor.TopLeft"/> with no offset.
    /// </summary>
    internal static Vector2 ComputeInputOffsetForAnchored(IUIComponent component, Vector2 screenSize)
    {
        if (component is not IAnchoredUIComponent anchored)
            return Vector2.Zero;

        if (anchored.Anchor == UIAnchor.TopLeft && anchored.AnchorOffset == Vector2.Zero)
            return Vector2.Zero;

        var resolved = ResolveAnchoredPosition(anchored, screenSize);
        return anchored.Position - resolved;
    }
}
