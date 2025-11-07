using Brine2D.Core.Hosting;
using Brine2D.Core.Input;
using Brine2D.Core.Math;
using Brine2D.Core.Scene;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Components;

/// <summary>
///     Component that owns and drives a <see cref="Camera2D" /> for 2D worlds.
/// </summary>
/// <remarks>
///     <para>Follows a target with dead zone, optional world bounds, and smoothing.</para>
///     <para>Supports authoring/debug controls for manual panning and mouse-wheel zoom.</para>
///     <para>Exposes convenience properties that forward to the underlying <see cref="Camera2D" />.</para>
/// </remarks>
/// <example>
///     <code>
///     // Attach to an entity, optionally set a target, and use its camera when rendering:
///     var ctrl = entity.Add(new CameraController2D());
///     ctrl.Target = playerTransform;
///
///     // In Draw:
///     engine.Sprites.Begin(ctrl.Camera);
///     // draw world-space sprites...
///     engine.Sprites.End();
///     </code>
/// </example>
public sealed class CameraController2D : Component
{
    private IEngineContext _engine = default!;

    /// <summary>
    ///     Creates a controller with an optional existing camera.
    /// </summary>
    /// <param name="camera">
    ///     Optional preconfigured camera instance. If <c>null</c>, a camera is created with (0,0) view size; the renderer
    ///     fills the view size on Begin(camera).
    /// </param>
    public CameraController2D(Camera2D? camera = null)
    {
        // ViewWidth/Height are filled by the renderer Begin(camera) when zero.
        Camera = camera ?? new Camera2D(0, 0);
    }

    /// <summary>
    ///     The world-space camera used for rendering and coordinate transforms.
    /// </summary>
    /// <remarks>
    ///     Pass this to <c>ISpriteRenderer.Begin(camera)</c>.
    /// </remarks>
    public Camera2D Camera { get; }
    
    /// <summary>
    ///     Dead zone rectangle in virtual/view pixels, centered on the screen.
    /// </summary>
    /// <remarks>
    ///     Disabled when <see cref="Rectangle.Width" /> or <see cref="Rectangle.Height" /> is less than or equal to 0.
    /// </remarks>
    public Rectangle DeadZone
    {
        get => Camera.DeadZone;
        set => Camera.DeadZone = value;
    }
    
    /// <summary>
    ///     Enables authoring-time manual panning with arrow keys (Left/Right/Up/Down).
    /// </summary>
    /// <value>Defaults to <see langword="false" />.</value>
    public bool EnableManualPan { get; set; } = false;

    /// <summary>
    ///     Enables mouse wheel zoom control. Zoom is clamped to <see cref="MinZoom" />..<see cref="MaxZoom" />.
    /// </summary>
    /// <value>Defaults to <see langword="true" />.</value>
    public bool EnableWheelZoom { get; set; } = true;

    /// <summary>
    ///     Follow smoothing factor (units per second). Use 0 for snap/instant movement.
    /// </summary>
    /// <value>Defaults to 10.</value>
    public float FollowLerp
    {
        get => Camera.FollowLerp;
        set => Camera.FollowLerp = value;
    }

    /// <summary>
    ///     Maximum zoom when using wheel zoom.
    /// </summary>
    /// <value>Defaults to 4.</value>
    public float MaxZoom { get; set; } = 4f;

    /// <summary>
    ///     Minimum zoom when using wheel zoom.
    /// </summary>
    /// <value>Defaults to 0.25.</value>
    public float MinZoom { get; set; } = 0.25f;

    /// <summary>
    ///     Manual pan speed in world units per second (not scaled by zoom).
    /// </summary>
    /// <value>Defaults to 500.</value>
    public float PanSpeed { get; set; } = 500f; // world units per second

    /// <summary>
    ///     Enables pixel snapping of final screen positions (renderer applies snapping at draw time).
    /// </summary>
    /// <remarks>
    ///     Most effective when <see cref="Camera2D.Rotation" /> is near zero.
    /// </remarks>
    /// <value>Defaults to <see langword="true" />.</value>
    public bool PixelSnap
    {
        get => Camera.PixelSnap;
        set => Camera.PixelSnap = value;
    }

    /// <summary>
    ///     Optional transform to follow. When set, the camera follows the target with smoothing/deadzone/bounds.
    /// </summary>
    public Transform2D? Target { get; set; }

    /// <summary>
    ///     World-space offset applied to <see cref="Target.Position" /> when following (e.g., look-ahead bias).
    /// </summary>
    /// <value>Defaults to <see cref="Vector2.Zero" />.</value>
    public Vector2 TargetOffset { get; set; } = Vector2.Zero;

    /// <summary>
    ///     Virtual height for letterboxing/pillarboxing. Set to 0 to disable virtual resolution.
    /// </summary>
    public int VirtualHeight
    {
        get => Camera.VirtualHeight;
        set => Camera.VirtualHeight = value;
    }

    /// <summary>
    ///     Virtual width for letterboxing/pillarboxing. Set to 0 to disable virtual resolution.
    /// </summary>
    public int VirtualWidth
    {
        get => Camera.VirtualWidth;
        set => Camera.VirtualWidth = value;
    }

    /// <summary>
    ///     World bounds that clamp the camera center so the view doesn't go outside the world.
    /// </summary>
    public Rectangle? WorldBounds
    {
        get => Camera.WorldBounds;
        set => Camera.WorldBounds = value;
    }

    /// <summary>
    ///     Camera zoom scale. 1 = 1:1; values &lt; 1 zoom out; values &gt; 1 zoom in.
    /// </summary>
    /// <value>Defaults to 1.</value>
    public float Zoom
    {
        get => Camera.Zoom;
        set => Camera.Zoom = value;
    }

    /// <summary>
    ///     Multiplicative zoom factor applied per wheel delta tick: <c>zoom *= 1 + wheel * factor</c>.
    /// </summary>
    /// <value>Defaults to 0.1.</value>
    public float ZoomFactorPerWheel { get; set; } = 0.1f; // zoom *= 1 + wheel * factor

    /// <summary>
    ///     Captures the engine context for input access (keyboard/mouse).
    /// </summary>
    public override void Initialize(IEngineContext engine)
    {
        _engine = engine;
    }

    /// <summary>
    ///     Per-frame update driving manual controls and target following.
    /// </summary>
    /// <param name="time">Game time containing frame delta.</param>
    public override void Update(GameTime time)
    {
        var dt = (float)time.DeltaSeconds;

        HandleManualPan(dt);
        HandleWheelZoom();

        // Follow target (if any) using Camera2D's follow logic (dead zone + bounds + smoothing).
        if (Target != null)
        {
            var targetWorld = new Vector2(Target.Position.X + TargetOffset.X, Target.Position.Y + TargetOffset.Y);
            Camera.Follow(targetWorld, dt);
        }
    }

    /// <summary>
    ///     Handles authoring-time manual camera panning via arrow keys.
    ///     Uses normalized input to keep diagonal speed consistent.
    /// </summary>
    private void HandleManualPan(float dt)
    {
        if (!EnableManualPan)
        {
            return;
        }

        var input = _engine.Input;
        float dx = 0f, dy = 0f;
        if (input.IsKeyDown(Key.Left))
        {
            dx -= 1f;
        }

        if (input.IsKeyDown(Key.Right))
        {
            dx += 1f;
        }

        if (input.IsKeyDown(Key.Up))
        {
            dy -= 1f;
        }

        if (input.IsKeyDown(Key.Down))
        {
            dy += 1f;
        }

        if (dx == 0f && dy == 0f)
        {
            return;
        }

        // Normalize to keep consistent speed diagonally.
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len > 0f)
        {
            dx /= len;
            dy /= len;
        }

        // Move in world units; not scaled by zoom (more predictable for authors).
        var pos = Camera.Position;
        pos = new Vector2(pos.X + dx * PanSpeed * dt, pos.Y + dy * PanSpeed * dt);
        Camera.Position = pos;
    }

    /// <summary>
    ///     Handles mouse wheel zoom with clamping between <see cref="MinZoom" /> and <see cref="MaxZoom" />.
    /// </summary>
    private void HandleWheelZoom()
    {
        if (!EnableWheelZoom)
        {
            return;
        }

        var wheel = _engine.Mouse.WheelY;
        if (wheel == 0f)
        {
            return;
        }

        var z = Camera.Zoom;
        // Multiplicative scaling keeps zoom behavior consistent across ranges.
        z *= 1f + wheel * ZoomFactorPerWheel;
        if (z < MinZoom)
        {
            z = MinZoom;
        }

        if (z > MaxZoom)
        {
            z = MaxZoom;
        }

        Camera.Zoom = z;
    }
}