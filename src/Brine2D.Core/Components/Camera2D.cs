using Brine2D.Core.Math;

namespace Brine2D.Core.Components;

/// <summary>
///     2D camera for world-space rendering with virtual resolution, transforms, and target following.
/// </summary>
/// <remarks>
///     <para>Supports virtual resolution with letterboxing/pillarboxing.</para>
///     <para>Converts between world and screen coordinates with zoom and rotation.</para>
///     <para>Optionally follows a target using a dead zone and clamps to world bounds.</para>
/// </remarks>
public sealed class Camera2D
{
    /// <summary>
    ///     Creates a new camera using the provided render target size.
    /// </summary>
    /// <param name="viewWidth">Render target width in pixels.</param>
    /// <param name="viewHeight">Render target height in pixels.</param>
    public Camera2D(int viewWidth, int viewHeight)
    {
        ViewWidth = viewWidth;
        ViewHeight = viewHeight;
    }

    /// <summary>
    ///     Dead zone rectangle, in virtual or view pixels, centered in the screen.
    /// </summary>
    /// <remarks>
    ///     Disabled when <see cref="Rectangle.Width" /> or <see cref="Rectangle.Height" /> is less than or equal to 0.
    /// </remarks>
    public Rectangle DeadZone { get; set; } = new(0, 0, 0, 0);

    /// <summary>
    ///     Smoothing factor for following movement (units per second).
    /// </summary>
    /// <value>Defaults to 10.</value>
    public float FollowLerp { get; set; } = 10f;

    /// <summary>
    ///     Hints the renderer to snap final screen positions to integer pixels.
    /// </summary>
    /// <remarks>
    ///     Applied by the renderer at draw time; most effective when <see cref="Rotation" /> is near zero.
    /// </remarks>
    /// <value>Defaults to <see langword="true" />.</value>
    public bool PixelSnap { get; set; } = true;

    /// <summary>
    ///     Camera center in world space (pixels/units).
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    ///     Camera rotation in radians, rotating the view around the camera center.
    /// </summary>
    /// <remarks>
    ///     Positive is counter-clockwise.
    /// </remarks>
    public float Rotation { get; set; }

    /// <summary>
    ///     Current render target height in pixels (set by the renderer).
    /// </summary>
    public int ViewHeight { get; set; }

    /// <summary>
    ///     Current render target width in pixels (set by the renderer).
    /// </summary>
    public int ViewWidth { get; set; }

    /// <summary>
    ///     Virtual height used for letterboxing/pillarboxing.
    /// </summary>
    /// <remarks>
    ///     If 0, virtual resolution is disabled and the full back buffer is used.
    /// </remarks>
    public int VirtualHeight { get; set; }

    /// <summary>
    ///     Virtual width used for letterboxing/pillarboxing.
    /// </summary>
    /// <remarks>
    ///     If 0, virtual resolution is disabled and the full back buffer is used.
    /// </remarks>
    public int VirtualWidth { get; set; }

    /// <summary>
    ///     Optional world-space rectangle that clamps the camera center so the view does not go outside the world.
    /// </summary>
    public Rectangle? WorldBounds { get; set; }

    /// <summary>
    ///     Camera zoom scale. 1 = 1:1; values &lt; 1 zoom out; values &gt; 1 zoom in.
    /// </summary>
    /// <value>Defaults to 1.</value>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    ///     Computes a letterboxed viewport inside a back buffer, preserving aspect ratio of the virtual size.
    /// </summary>
    /// <remarks>
    ///     If virtual resolution is disabled (VirtualWidth/Height == 0), returns the full back buffer with scale 1.
    /// </remarks>
    /// <param name="backBufferW">Back buffer width in pixels.</param>
    /// <param name="backBufferH">Back buffer height in pixels.</param>
    /// <returns>(x, y, w, h, scale) of the letterboxed viewport.</returns>
    public (int x, int y, int w, int h, float scale) ComputeViewport(int backBufferW, int backBufferH)
    {
        if (VirtualWidth <= 0 || VirtualHeight <= 0)
        {
            return (0, 0, backBufferW, backBufferH, 1f);
        }

        // Compute uniform scale to fit virtual size inside the back buffer
        var sx = (float)backBufferW / VirtualWidth;
        var sy = (float)backBufferH / VirtualHeight;
        var s = MathF.Min(sx, sy);

        // Final viewport size centered in the back buffer
        var w = (int)MathF.Round(VirtualWidth * s);
        var h = (int)MathF.Round(VirtualHeight * s);
        var x = (backBufferW - w) / 2;
        var y = (backBufferH - h) / 2;
        return (x, y, w, h, s);
    }

    /// <summary>
    ///     Follows a target position by keeping it within the dead zone (if enabled) and clamping camera center to world
    ///     bounds (if set).
    /// </summary>
    /// <remarks>
    ///     Call once per update with delta time.
    /// </remarks>
    /// <param name="target">Target world position to follow.</param>
    /// <param name="dt">Delta time in seconds.</param>
    public void Follow(Vector2 target, float dt)
    {
        // Choose the pixel-space used for dead zone: virtual if enabled, else current view size
        var vw = VirtualWidth > 0 ? VirtualWidth : ViewWidth;
        var vh = VirtualHeight > 0 ? VirtualHeight : ViewHeight;
        if (vw <= 0 || vh <= 0)
        {
            return;
        }

        // Effective half-view extents in world units (convert pixels to world by dividing by Zoom)
        var halfW = vw * 0.5f / (Zoom <= 0 ? 0.0001f : Zoom);
        var halfH = vh * 0.5f / (Zoom <= 0 ? 0.0001f : Zoom);

        // Target position relative to camera center in world
        var dx = target.X - Position.X;
        var dy = target.Y - Position.Y;

        // Convert relative position to screen pixels (ignores rotation for dead zone simplicity)
        var sx = dx * Zoom + vw * 0.5f;
        var sy = dy * Zoom + vh * 0.5f;

        var dz = DeadZone;
        var dzEnabled = dz is { Width: > 0, Height: > 0 };

        var desiredX = Position.X;
        var desiredY = Position.Y;

        if (dzEnabled)
        {
            // Shift camera so the target stays within the dead zone rectangle
            // Note: divide by Zoom to convert pixel correction back to world units
            if (sx < dz.Left)
            {
                desiredX -= (dz.Left - sx) / (Zoom <= 0 ? 0.0001f : Zoom);
            }

            if (sx > dz.Right)
            {
                desiredX += (sx - dz.Right) / (Zoom <= 0 ? 0.0001f : Zoom);
            }

            if (sy < dz.Top)
            {
                desiredY -= (dz.Top - sy) / (Zoom <= 0 ? 0.0001f : Zoom);
            }

            if (sy > dz.Bottom)
            {
                desiredY += (sy - dz.Bottom) / (Zoom <= 0 ? 0.0001f : Zoom);
            }
        }
        else
        {
            // No dead zone: center the camera on the target
            desiredX = target.X;
            desiredY = target.Y;
        }

        // Clamp the desired camera center to world bounds, leaving half-view margins
        if (WorldBounds.HasValue)
        {
            var b = WorldBounds.Value;
            var minX = b.Left + halfW;
            var maxX = b.Right - halfW;
            var minY = b.Top + halfH;
            var maxY = b.Bottom - halfH;

            // Handle tiny worlds smaller than the viewport by collapsing to center
            if (minX > maxX)
            {
                var m = (minX + maxX) * 0.5f;
                minX = maxX = m;
            }

            if (minY > maxY)
            {
                var m = (minY + maxY) * 0.5f;
                minY = maxY = m;
            }

            desiredX = System.Math.Clamp(desiredX, minX, maxX);
            desiredY = System.Math.Clamp(desiredY, minY, maxY);
        }

        // Smoothly approach the desired point; FollowLerp == 0 means snap instantly
        var t = FollowLerp <= 0 ? 1f : MathF.Min(1f, FollowLerp * dt);
        Position = new Vector2(
            Position.X + (desiredX - Position.X) * t,
            Position.Y + (desiredY - Position.Y) * t
        );
    }

    /// <summary>
    ///     Converts a back buffer pixel coordinate to world space, accounting for camera transform and letterbox.
    /// </summary>
    /// <param name="screen">Screen-space pixel coordinate inside the back buffer.</param>
    /// <param name="backBufferW">Back buffer width in pixels.</param>
    /// <param name="backBufferH">Back buffer height in pixels.</param>
    /// <returns>World-space coordinate.</returns>
    public Vector2 ScreenToWorld(Vector2 screen, int backBufferW, int backBufferH)
    {
        var (vx, vy, vw, vh, _) = ComputeViewport(backBufferW, backBufferH);

        // Remove viewport offset (letterbox origin)
        var sx = screen.X - vx;
        var sy = screen.Y - vy;

        // Undo scale and center shift; guard zero zoom to avoid NaN/Inf
        var halfW = vw * 0.5f;
        var halfH = vh * 0.5f;
        sx = (sx - halfW) / (Zoom == 0 ? 0.0001f : Zoom);
        sy = (sy - halfH) / (Zoom == 0 ? 0.0001f : Zoom);

        // Rotate by +Rotation to invert the earlier -Rotation
        var cos = MathF.Cos(Rotation);
        var sin = MathF.Sin(Rotation);
        var wx = sx * cos - sy * sin;
        var wy = sx * sin + sy * cos;

        // Translate by +camera position back into world
        wx += Position.X;
        wy += Position.Y;
        return new Vector2(wx, wy);
    }

    /// <summary>
    ///     Converts a world-space point to back buffer pixel coordinates, accounting for camera transform and letterbox.
    /// </summary>
    /// <param name="world">World-space coordinate to transform.</param>
    /// <param name="backBufferW">Back buffer width in pixels.</param>
    /// <param name="backBufferH">Back buffer height in pixels.</param>
    /// <returns>Screen-space pixel coordinate inside the back buffer.</returns>
    public Vector2 WorldToScreen(Vector2 world, int backBufferW, int backBufferH)
    {
        var (vx, vy, vw, vh, _) = ComputeViewport(backBufferW, backBufferH);

        // Translate by -camera position to move camera center to origin
        var cx = world.X - Position.X;
        var cy = world.Y - Position.Y;

        // Rotate by -Rotation (view-space is rotated opposite to camera)
        var cos = MathF.Cos(Rotation);
        var sin = MathF.Sin(Rotation);
        var rx = cx * cos + cy * sin;
        var ry = -cx * sin + cy * cos;

        // Scale by Zoom, then shift to viewport center
        var halfW = vw * 0.5f;
        var halfH = vh * 0.5f;
        rx = rx * Zoom + halfW;
        ry = ry * Zoom + halfH;

        // Add viewport offset (letterbox origin)
        rx += vx;
        ry += vy;
        return new Vector2(rx, ry);
    }
}