using System.Numerics;
using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Animation;

/// <summary>
/// Represents a single frame in a sprite animation.
/// </summary>
public class SpriteFrame
{
    private float _duration = 0.1f;
    private List<WeakReference<AnimationClip>>? _ownerClips;
    private Dictionary<string, Rectangle>? _namedHitBoxes;

    /// <summary>
    /// Source rectangle in the sprite sheet (in pixels).
    /// </summary>
    public Rectangle SourceRect { get; set; }

    /// <summary>
    /// Gets the number of <see cref="AnimationClip"/> instances this frame currently belongs to.
    /// A value greater than 1 means this frame is shared; mutating <see cref="Duration"/> will
    /// invalidate the <see cref="AnimationClip.TotalDuration"/> cache on all owning clips.
    /// </summary>
    public int OwnerClipCount => _ownerClips?.Count(wr => wr.TryGetTarget(out _)) ?? 0;

    /// <summary>
    /// Duration to display this frame (in seconds). Minimum 0.001 seconds (1 ms).
    /// Automatically invalidates the owning <see cref="AnimationClip"/>'s duration cache when
    /// changed. Clips that share this frame are all invalidated via weak references, so a clip
    /// that has been abandoned (GC eligible) will not prevent collection.
    /// </summary>
    public float Duration
    {
        get => _duration;
        set
        {
            var clamped = MathF.Max(value, 0.001f);
            if (clamped == _duration)
                return;
            _duration = clamped;
            NotifyOwners();
        }
    }

    /// <summary>
    /// Origin/pivot point for this frame (relative to frame, 0–1 range).
    /// Applied by <see cref="Brine2D.Systems.Animation.AnimationSystem"/> to <see cref="Brine2D.Systems.Rendering.SpriteComponent.Origin"/>.
    /// </summary>
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

    /// <summary>
    /// Pixel offset applied to the sprite's draw position for this frame.
    /// Used by <see cref="Brine2D.Systems.Animation.AnimationSystem"/> to compensate for
    /// Aseprite trim offsets (<c>spriteSourceSize</c>) so that trimmed frames appear at the
    /// correct canvas-relative position. Zero by default (no offset).
    /// </summary>
    public Vector2 DrawOffset { get; set; }

    /// <summary>
    /// Optional texture path override for this frame. Takes priority over
    /// <see cref="AnimationClip.TexturePath"/> when non-null.
    /// </summary>
    public string? TexturePath { get; set; }

    /// <summary>
    /// Optional pre-loaded texture override for this frame. Takes priority over
    /// <see cref="AnimationClip.Texture"/> when non-null.
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Optional per-frame horizontal flip override. When non-null and the layer mask includes
    /// <see cref="AnimationLayerMask.FlipX"/>, <see cref="Brine2D.Systems.Animation.AnimationSystem"/>
    /// writes this to <see cref="Brine2D.Systems.Rendering.SpriteComponent.FlipX"/>.
    /// </summary>
    public bool? FlipX { get; set; }

    /// <summary>
    /// Optional per-frame vertical flip override. When non-null and the layer mask includes
    /// <see cref="AnimationLayerMask.FlipY"/>, <see cref="Brine2D.Systems.Animation.AnimationSystem"/>
    /// writes this to <see cref="Brine2D.Systems.Rendering.SpriteComponent.FlipY"/>.
    /// </summary>
    public bool? FlipY { get; set; }

    /// <summary>
    /// Optional per-frame tint override. When non-null and the layer mask includes
    /// <see cref="AnimationLayerMask.Tint"/>, <see cref="Brine2D.Systems.Animation.AnimationSystem"/>
    /// writes this to <see cref="Brine2D.Systems.Rendering.SpriteComponent.Tint"/>.
    /// </summary>
    public Color? Tint { get; set; }

    /// <summary>
    /// Optional per-frame hit/collision box in local space (pixels, relative to the frame's
    /// top-left corner). Shorthand for the named box stored under <see cref="AsepriteClipLoader.HitBoxSliceName"/>.
    /// </summary>
    public Rectangle? HitBox { get; set; }

    /// <summary>
    /// Arbitrary per-frame payload. Not consumed by the animation system.
    /// </summary>
    public object? UserData { get; set; }

    /// <summary>
    /// Raised once when this frame becomes the active frame.
    /// Multiple subscribers are supported; all are invoked in subscription order.
    /// </summary>
    public event Action? OnEnter;

    /// <summary>
    /// Raised once when this frame is no longer the active frame.
    /// Multiple subscribers are supported; all are invoked in subscription order.
    /// </summary>
    public event Action? OnExit;

    /// <summary>Read-only view of all named hit boxes on this frame.</summary>
    public IReadOnlyDictionary<string, Rectangle>? NamedHitBoxes => _namedHitBoxes;

    public SpriteFrame(Rectangle sourceRect, float duration = 0.1f)
    {
        SourceRect = sourceRect;
        _duration = MathF.Max(duration, 0.001f);
    }

    internal void RaiseOnEnter() => OnEnter?.Invoke();
    internal void RaiseOnExit() => OnExit?.Invoke();

    /// <summary>Sets a named hit box.</summary>
    public void SetHitBox(string name, Rectangle rect)
    {
        ArgumentNullException.ThrowIfNull(name);
        _namedHitBoxes ??= new Dictionary<string, Rectangle>(StringComparer.Ordinal);
        _namedHitBoxes[name] = rect;
    }

    /// <summary>Returns the named hit box, or throws <see cref="KeyNotFoundException"/>.</summary>
    public Rectangle GetHitBox(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_namedHitBoxes != null && _namedHitBoxes.TryGetValue(name, out var rect))
            return rect;
        throw new KeyNotFoundException($"Hit box '{name}' not found on this frame.");
    }

    /// <summary>Returns the named hit box if present, or <c>null</c>.</summary>
    public Rectangle? TryGetHitBox(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_namedHitBoxes != null && _namedHitBoxes.TryGetValue(name, out var rect))
            return rect;
        return null;
    }

    /// <summary>Removes the named hit box. Returns <c>true</c> if it existed.</summary>
    public bool RemoveHitBox(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _namedHitBoxes?.Remove(name) ?? false;
    }

    /// <summary>
    /// Creates a shallow clone of this frame with the same visual properties, hit boxes, and
    /// user data. <see cref="OnEnter"/> and <see cref="OnExit"/> event subscriptions are
    /// <em>not</em> copied — the clone starts with no subscribers. The clone is not registered
    /// as belonging to any <see cref="AnimationClip"/>; add it to a clip via
    /// <see cref="AnimationClip.AddFrame"/> as normal.
    /// </summary>
    public SpriteFrame Clone()
    {
        var clone = new SpriteFrame(SourceRect, _duration)
        {
            Origin = Origin,
            DrawOffset = DrawOffset,
            TexturePath = TexturePath,
            Texture = Texture,
            FlipX = FlipX,
            FlipY = FlipY,
            Tint = Tint,
            HitBox = HitBox,
            UserData = UserData
        };

        if (_namedHitBoxes != null)
        {
            foreach (var (key, value) in _namedHitBoxes)
                clone.SetHitBox(key, value);
        }

        return clone;
    }

    internal void RegisterOwningClip(AnimationClip clip)
    {
        _ownerClips ??= new List<WeakReference<AnimationClip>>();
        _ownerClips.Add(new WeakReference<AnimationClip>(clip));
    }

    internal void UnregisterOwningClip(AnimationClip clip)
    {
        if (_ownerClips == null)
            return;
        _ownerClips.RemoveAll(wr => !wr.TryGetTarget(out var target) || ReferenceEquals(target, clip));
    }

    private void NotifyOwners()
    {
        if (_ownerClips == null)
            return;
        foreach (var wr in _ownerClips)
        {
            if (wr.TryGetTarget(out var clip))
                clip.InvalidateDurationCache();
        }
    }
}