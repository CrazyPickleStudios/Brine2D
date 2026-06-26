using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Entry in the UICanvas input dispatch buffer. Carries the component, a coordinate
/// offset for non-screen-space children, and an optional clip rect that blocks hits
/// outside the owning container's visible bounds.
/// </summary>
internal readonly record struct InputDispatchEntry(
    IUIComponent Component,
    Vector2 InputOffset,
    bool HasClip,
    float ClipLeft,
    float ClipTop,
    float ClipRight,
    float ClipBottom)
{
    /// <summary>
    /// Creates an entry for a top-level or tab-container child.
    /// Pass a non-zero <paramref name="anchorOffset"/> for <see cref="IAnchoredUIComponent"/> instances.
    /// </summary>
    internal static InputDispatchEntry ForComponent(IUIComponent component, Vector2 anchorOffset) =>
        new(component, anchorOffset, false, 0, 0, 0, 0);

    /// <summary>
    /// Creates an entry for a direct child of a <see cref="UIScrollView"/>.
    /// The offset converts incoming screen-space positions into content space before
    /// <see cref="IUIComponent.Contains"/> runs. The clip blocks hits outside the
    /// scroll view's visible rect.
    /// </summary>
    internal static InputDispatchEntry ForScrollChild(IUIComponent component, UIScrollView owner) =>
        ForScrollChild(component, owner, owner.Position, owner.Size);

    /// <summary>
    /// Variant of <see cref="ForScrollChild(IUIComponent,UIScrollView)"/> that takes an
    /// explicitly resolved screen-space position for anchored scroll-view owners.
    /// </summary>
    internal static InputDispatchEntry ForScrollChild(IUIComponent component, UIScrollView owner,
        Vector2 resolvedPosition, Vector2 resolvedSize) =>
        new(component,
            owner.ScrollOffset - resolvedPosition,
            true,
            resolvedPosition.X,
            resolvedPosition.Y,
            resolvedPosition.X + resolvedSize.X,
            resolvedPosition.Y + resolvedSize.Y);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="screenPosition"/> hits this component,
    /// applying the clip rect and coordinate offset.
    /// </summary>
    internal bool IsHit(Vector2 screenPosition)
    {
        if (HasClip &&
            (screenPosition.X < ClipLeft || screenPosition.X > ClipRight ||
             screenPosition.Y < ClipTop || screenPosition.Y > ClipBottom))
            return false;

        return Component.Contains(screenPosition + InputOffset);
    }
}