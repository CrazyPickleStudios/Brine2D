using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// A rectangular drop zone that participates in the canvas drag-and-drop system.
/// Add it to a <see cref="UICanvas"/> and call <see cref="UICanvas.RegisterDropTarget"/>
/// to activate it. Subscribe to <see cref="OnDrop"/> to receive dropped payloads.
/// </summary>
public class UIDropTarget : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <summary>
    /// Normal background color. Use <see cref="Color.Transparent"/> to make the zone
    /// invisible when idle.
    /// </summary>
    public Color IdleColor { get; set; } = new Color(80, 80, 80, 60);

    /// <summary>
    /// Background color rendered while a compatible drag payload is hovering over this zone.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(100, 180, 255, 100);

    /// <summary>
    /// Border color drawn around the zone. Set alpha to 0 to hide.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(120, 180, 255, 180);

    /// <summary>
    /// Border thickness in pixels.
    /// </summary>
    public float BorderThickness { get; set; } = 2f;

    /// <summary>
    /// Whether a compatible drag payload is currently hovering over this zone.
    /// Set by <see cref="UICanvas"/> during drag updates.
    /// </summary>
    public bool IsHovered { get; private set; }

    /// <summary>
    /// Fired when a compatible payload is dropped onto this zone.
    /// The argument is the dropped <see cref="IDragPayload"/>.
    /// </summary>
    public event Action<IDragPayload>? OnDrop;

    /// <summary>
    /// Optional predicate called by the canvas to decide whether this target
    /// accepts a given payload type. Return <c>true</c> to accept. Defaults to
    /// accepting all payloads when not set.
    /// </summary>
    public Func<IDragPayload, bool>? AcceptsPayload { get; set; }

    public void Update(float deltaTime) { }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        var bg = IsHovered ? HoverColor : IdleColor;
        if (bg.A > 0)
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, bg);

        if (BorderColor.A > 0)
            renderer.DrawRectangleOutline(Position.X, Position.Y, Size.X, Size.Y, BorderColor, BorderThickness);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    internal void SetHovered(bool hovered) => IsHovered = hovered;

    internal void FireDrop(IDragPayload payload) => OnDrop?.Invoke(payload);
}
