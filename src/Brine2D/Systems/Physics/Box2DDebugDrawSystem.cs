using Box2D.NET.Bindings;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Physics;
using Brine2D.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brine2D.Systems.Physics;

public sealed unsafe class Box2DDebugDrawSystem : RenderSystemBase, IDisposable
{
    private readonly PhysicsWorld _physicsWorld;
    private B2.DebugDraw _debugDraw;
    private bool _initialized;

    private readonly DrawContext _drawContext = new();
    private readonly GCHandle _contextHandle;

    private sealed class DrawContext
    {
        public IRenderer Renderer { get; set; } = null!;
        public ICamera? Camera { get; set; }
        public bool DrawStrings { get; set; }
    }

    public Box2DDebugDrawSystem(PhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;
        _contextHandle = GCHandle.Alloc(_drawContext);
    }

    public override int RenderOrder => SystemRenderOrder.Debug + 1;

    /// <summary>
    /// When set, all debug draw coordinates are transformed through
    /// <see cref="ICamera.WorldToScreen"/> so shapes align with the game view
    /// when the camera pans or zooms. Leave <c>null</c> to draw in raw world space.
    /// </summary>
    public ICamera? Camera { get; set; }

    public bool DrawShapes { get; set; } = true;
    public bool DrawJoints { get; set; } = true;
    public bool DrawJointExtras { get; set; }
    public bool DrawBounds { get; set; }
    public bool DrawMass { get; set; }
    public bool DrawBodyNames { get; set; }
    public bool DrawContacts { get; set; }
    public bool DrawGraphColors { get; set; }
    public bool DrawContactNormals { get; set; }
    public bool DrawContactImpulses { get; set; }
    public bool DrawContactFeatures { get; set; }
    public bool DrawFrictionImpulses { get; set; }
    public bool DrawIslands { get; set; }

    /// <summary>
    /// When <c>true</c>, Box2D debug string annotations (body names, mass centers, etc.) are
    /// forwarded to <see cref="IRenderer.DrawText"/>. Defaults to <c>false</c> because most
    /// renderers do not have a debug font loaded and the call would silently fail or throw.
    /// </summary>
    public bool DrawStrings { get; set; }

    public override void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime)
    {
        if (!_physicsWorld.IsValid)
            return;

        if (!_initialized)
        {
            InitializeDebugDraw();
            _initialized = true;
        }

        _debugDraw.drawShapes = DrawShapes;
        _debugDraw.drawJoints = DrawJoints;
        _debugDraw.drawJointExtras = DrawJointExtras;
        _debugDraw.drawBounds = DrawBounds;
        _debugDraw.drawMass = DrawMass;
        _debugDraw.drawBodyNames = DrawBodyNames;
        _debugDraw.drawContacts = DrawContacts;
        _debugDraw.drawGraphColors = DrawGraphColors;
        _debugDraw.drawContactNormals = DrawContactNormals;
        _debugDraw.drawContactImpulses = DrawContactImpulses;
        _debugDraw.drawContactFeatures = DrawContactFeatures;
        _debugDraw.drawFrictionImpulses = DrawFrictionImpulses;
        _debugDraw.drawIslands = DrawIslands;

        _drawContext.Renderer = renderer;
        _drawContext.Camera = Camera;
        _drawContext.DrawStrings = DrawStrings;
        _debugDraw.context = (void*)GCHandle.ToIntPtr(_contextHandle);

        fixed (B2.DebugDraw* drawPtr = &_debugDraw)
            B2.WorldDraw(_physicsWorld.WorldId, drawPtr);
    }

    public void Dispose()
    {
        if (_contextHandle.IsAllocated)
            _contextHandle.Free();
    }

    private void InitializeDebugDraw()
    {
        _debugDraw = B2.DefaultDebugDraw();
        _debugDraw.DrawPolygonFcn = &DrawPolygonCallback;
        _debugDraw.DrawSolidPolygonFcn = &DrawSolidPolygonCallback;
        _debugDraw.DrawCircleFcn = &DrawCircleCallback;
        _debugDraw.DrawSolidCircleFcn = &DrawSolidCircleCallback;
        _debugDraw.DrawSolidCapsuleFcn = &DrawSolidCapsuleCallback;
        _debugDraw.DrawSegmentFcn = &DrawSegmentCallback;
        _debugDraw.DrawTransformFcn = &DrawTransformCallback;
        _debugDraw.DrawPointFcn = &DrawPointCallback;
        _debugDraw.DrawStringFcn = &DrawStringCallback;
    }

    private static DrawContext GetContext(void* context)
        => (DrawContext)GCHandle.FromIntPtr((nint)context).Target!;

    private static Color ToColor(B2.HexColor hex)
    {
        uint c = (uint)hex;
        byte r = (byte)((c >> 16) & 0xFF);
        byte g = (byte)((c >> 8) & 0xFF);
        byte b = (byte)(c & 0xFF);
        return new Color(r, g, b, 200);
    }

    private static Vector2 W2S(DrawContext ctx, float wx, float wy)
        => ctx.Camera != null ? ctx.Camera.WorldToScreen(new Vector2(wx, wy)) : new Vector2(wx, wy);

    private static float WScale(DrawContext ctx, float worldLength)
        => ctx.Camera != null ? worldLength * ctx.Camera.Zoom : worldLength;

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawPolygonCallback(B2.Vec2* vertices, int vertexCount, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        if (vertexCount < 2) return;
        var c = ToColor(color);

        for (int i = 0; i < vertexCount; i++)
        {
            int next = (i + 1) % vertexCount;
            var a = W2S(ctx, vertices[i].x, vertices[i].y);
            var b = W2S(ctx, vertices[next].x, vertices[next].y);
            ctx.Renderer.DrawLine(a.X, a.Y, b.X, b.Y, c, 1f);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawSolidPolygonCallback(B2.Transform xf, B2.Vec2* vertices, int vertexCount, float radius, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        if (vertexCount < 2) return;
        var c = ToColor(color);

        for (int i = 0; i < vertexCount; i++)
        {
            var world = B2.TransformPoint(xf, vertices[i]);
            int next = (i + 1) % vertexCount;
            var worldNext = B2.TransformPoint(xf, vertices[next]);
            var a = W2S(ctx, world.x, world.y);
            var b = W2S(ctx, worldNext.x, worldNext.y);
            ctx.Renderer.DrawLine(a.X, a.Y, b.X, b.Y, c, 2f);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawCircleCallback(B2.Vec2 center, float radius, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        var s = W2S(ctx, center.x, center.y);
        ctx.Renderer.DrawCircleOutline(s.X, s.Y, WScale(ctx, radius), ToColor(color), 1f);
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawSolidCircleCallback(B2.Transform xf, float radius, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        var c = ToColor(color);
        var s = W2S(ctx, xf.p.x, xf.p.y);
        var sr = WScale(ctx, radius);
        ctx.Renderer.DrawCircleOutline(s.X, s.Y, sr, c, 2f);

        var xAxis = B2.RotGetXAxis(xf.q);
        var tip = W2S(ctx, xf.p.x + xAxis.x * radius, xf.p.y + xAxis.y * radius);
        ctx.Renderer.DrawLine(s.X, s.Y, tip.X, tip.Y, c, 1.5f);
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawSolidCapsuleCallback(B2.Vec2 p1, B2.Vec2 p2, float radius, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        var c = ToColor(color);
        var sr = WScale(ctx, radius);

        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < float.Epsilon)
        {
            // Degenerate capsule — draw as a single circle.
            var s = W2S(ctx, p1.x, p1.y);
            ctx.Renderer.DrawCircleOutline(s.X, s.Y, sr, c, 2f);
            return;
        }

        // Perpendicular (outward) unit vector.
        float px = -dy / len;
        float py = dx / len;

        // Barrel side lines.
        var la = W2S(ctx, p1.x + px * radius, p1.y + py * radius);
        var lb = W2S(ctx, p2.x + px * radius, p2.y + py * radius);
        var ra = W2S(ctx, p1.x - px * radius, p1.y - py * radius);
        var rb = W2S(ctx, p2.x - px * radius, p2.y - py * radius);
        ctx.Renderer.DrawLine(la.X, la.Y, lb.X, lb.Y, c, 2f);
        ctx.Renderer.DrawLine(ra.X, ra.Y, rb.X, rb.Y, c, 2f);

        // Semicircular end-caps as polyline approximations (16 segments per half-circle).
        const int segments = 16;
        const float step = MathF.PI / segments;

        // Angle of the axis from p1 toward p2.
        float axisAngle = MathF.Atan2(dy, dx);

        // Cap at p1: the outward half faces away from p2 (axisAngle + PI/2 → axisAngle + 3*PI/2).
        float startAngle1 = axisAngle + MathF.PI / 2f;
        var prev1 = W2S(ctx,
            p1.x + MathF.Cos(startAngle1) * radius,
            p1.y + MathF.Sin(startAngle1) * radius);
        for (int i = 1; i <= segments; i++)
        {
            float a = startAngle1 + step * i;
            var next = W2S(ctx, p1.x + MathF.Cos(a) * radius, p1.y + MathF.Sin(a) * radius);
            ctx.Renderer.DrawLine(prev1.X, prev1.Y, next.X, next.Y, c, 2f);
            prev1 = next;
        }

        // Cap at p2: the outward half faces away from p1 (axisAngle - PI/2 → axisAngle + PI/2).
        float startAngle2 = axisAngle - MathF.PI / 2f;
        var prev2 = W2S(ctx,
            p2.x + MathF.Cos(startAngle2) * radius,
            p2.y + MathF.Sin(startAngle2) * radius);
        for (int i = 1; i <= segments; i++)
        {
            float a = startAngle2 + step * i;
            var next = W2S(ctx, p2.x + MathF.Cos(a) * radius, p2.y + MathF.Sin(a) * radius);
            ctx.Renderer.DrawLine(prev2.X, prev2.Y, next.X, next.Y, c, 2f);
            prev2 = next;
        }
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawSegmentCallback(B2.Vec2 p1, B2.Vec2 p2, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        var a = W2S(ctx, p1.x, p1.y);
        var b = W2S(ctx, p2.x, p2.y);
        ctx.Renderer.DrawLine(a.X, a.Y, b.X, b.Y, ToColor(color), 1f);
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawTransformCallback(B2.Transform xf, void* context)
    {
        var ctx = GetContext(context);
        const float axisScreenLength = 20f;

        var xAxis = B2.RotGetXAxis(xf.q);
        var yAxis = B2.RotGetYAxis(xf.q);
        var origin = W2S(ctx, xf.p.x, xf.p.y);

        var xScreenDir = W2S(ctx, xf.p.x + xAxis.x, xf.p.y + xAxis.y) - origin;
        var yScreenDir = W2S(ctx, xf.p.x + yAxis.x, xf.p.y + yAxis.y) - origin;

        float xLen = xScreenDir.Length();
        float yLen = yScreenDir.Length();

        var xTip = xLen > 0f ? origin + xScreenDir / xLen * axisScreenLength : origin;
        var yTip = yLen > 0f ? origin + yScreenDir / yLen * axisScreenLength : origin;

        ctx.Renderer.DrawLine(origin.X, origin.Y, xTip.X, xTip.Y, new Color(255, 0, 0, 200), 2f);
        ctx.Renderer.DrawLine(origin.X, origin.Y, yTip.X, yTip.Y, new Color(0, 255, 0, 200), 2f);
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawPointCallback(B2.Vec2 p, float size, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        var s = W2S(ctx, p.x, p.y);
        ctx.Renderer.DrawCircleFilled(s.X, s.Y, WScale(ctx, size * 0.5f), ToColor(color));
    }

    [ExcludeFromCodeCoverage(Justification = "All draw callbacks are invoked by Box2D native debug draw; coverage tooling cannot trace native→managed transitions.")]
    [UnmanagedCallersOnly]
    private static void DrawStringCallback(B2.Vec2 p, byte* str, B2.HexColor color, void* context)
    {
        var ctx = GetContext(context);
        if (!ctx.DrawStrings || str == null) return;
        string text = Marshal.PtrToStringUTF8((nint)str) ?? string.Empty;
        var s = W2S(ctx, p.x, p.y);
        ctx.Renderer.DrawText(text, s.X, s.Y, ToColor(color));
    }
}