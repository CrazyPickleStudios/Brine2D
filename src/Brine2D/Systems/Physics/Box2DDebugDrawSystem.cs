using System.Numerics;
using System.Runtime.InteropServices;
using Box2D.NET.Bindings;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Physics;
using Brine2D.Rendering;

namespace Brine2D.Systems.Physics;

public sealed unsafe class Box2DDebugDrawSystem : RenderSystemBase
{
    private readonly PhysicsWorld _physicsWorld;
    private B2.DebugDraw _debugDraw;
    private bool _initialized;

    // SDL3 requires all GPU/render commands to be issued on the main thread.
    // [ThreadStatic] is intentional here: the fields are only ever written and read
    // on that same main thread, so no cross-thread visibility issues arise.
    [ThreadStatic]
    private static IRenderer? _activeRenderer;

    [ThreadStatic]
    private static ICamera? _activeCamera;

    public Box2DDebugDrawSystem(PhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld; 
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

    public override void Render(IEntityWorld world, IRenderer renderer)
    {
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

        _activeRenderer = renderer;
        _activeCamera = Camera;
        try
        {
            fixed (B2.DebugDraw* drawPtr = &_debugDraw)
                B2.WorldDraw(_physicsWorld.WorldId, drawPtr);
        }
        finally
        {
            _activeRenderer = null;
            _activeCamera = null;
        }
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

    private static Color ToColor(B2.HexColor hex)
    {
        uint c = (uint)hex;
        byte r = (byte)((c >> 16) & 0xFF);
        byte g = (byte)((c >> 8) & 0xFF);
        byte b = (byte)(c & 0xFF);
        return new Color(r, g, b, 200);
    }

    // Converts a world-space point to screen space when a camera is active.
    private static Vector2 W2S(float wx, float wy)
    {
        var cam = _activeCamera;
        return cam != null ? cam.WorldToScreen(new Vector2(wx, wy)) : new Vector2(wx, wy);
    }

    // Scales a world-space length (radius, axis length, point size) by camera zoom.
    private static float WScale(float worldLength)
    {
        var cam = _activeCamera;
        return cam != null ? worldLength * cam.Zoom : worldLength;
    }

    [UnmanagedCallersOnly]
    private static void DrawPolygonCallback(B2.Vec2* vertices, int vertexCount, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer || vertexCount < 2) return;
        var c = ToColor(color);

        for (int i = 0; i < vertexCount; i++)
        {
            int next = (i + 1) % vertexCount;
            var a = W2S(vertices[i].x, vertices[i].y);
            var b = W2S(vertices[next].x, vertices[next].y);
            renderer.DrawLine(a.X, a.Y, b.X, b.Y, c, 1f);
        }
    }

    [UnmanagedCallersOnly]
    private static void DrawSolidPolygonCallback(B2.Transform xf, B2.Vec2* vertices, int vertexCount, float radius, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer || vertexCount < 2) return;
        var c = ToColor(color);

        for (int i = 0; i < vertexCount; i++)
        {
            var world = B2.TransformPoint(xf, vertices[i]);
            int next = (i + 1) % vertexCount;
            var worldNext = B2.TransformPoint(xf, vertices[next]);
            var a = W2S(world.x, world.y);
            var b = W2S(worldNext.x, worldNext.y);
            renderer.DrawLine(a.X, a.Y, b.X, b.Y, c, 2f);
        }
    }

    [UnmanagedCallersOnly]
    private static void DrawCircleCallback(B2.Vec2 center, float radius, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        var s = W2S(center.x, center.y);
        renderer.DrawCircleOutline(s.X, s.Y, WScale(radius), ToColor(color), 1f);
    }

    [UnmanagedCallersOnly]
    private static void DrawSolidCircleCallback(B2.Transform xf, float radius, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        var c = ToColor(color);
        var s = W2S(xf.p.x, xf.p.y);
        var sr = WScale(radius);
        renderer.DrawCircleOutline(s.X, s.Y, sr, c, 2f);

        var xAxis = B2.RotGetXAxis(xf.q);
        var tip = W2S(xf.p.x + xAxis.x * radius, xf.p.y + xAxis.y * radius);
        renderer.DrawLine(s.X, s.Y, tip.X, tip.Y, c, 1.5f);
    }

    [UnmanagedCallersOnly]
    private static void DrawSolidCapsuleCallback(B2.Vec2 p1, B2.Vec2 p2, float radius, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        var c = ToColor(color);
        var s1 = W2S(p1.x, p1.y);
        var s2 = W2S(p2.x, p2.y);
        var sr = WScale(radius);

        renderer.DrawCircleOutline(s1.X, s1.Y, sr, c, 2f);
        renderer.DrawCircleOutline(s2.X, s2.Y, sr, c, 2f);

        float dx = p2.x - p1.x;
        float dy = p2.y - p1.y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len > 0f)
        {
            float px = -dy / len * radius;
            float py = dx / len * radius;
            var la = W2S(p1.x + px, p1.y + py);
            var lb = W2S(p2.x + px, p2.y + py);
            var ra = W2S(p1.x - px, p1.y - py);
            var rb = W2S(p2.x - px, p2.y - py);
            renderer.DrawLine(la.X, la.Y, lb.X, lb.Y, c, 2f);
            renderer.DrawLine(ra.X, ra.Y, rb.X, rb.Y, c, 2f);
        }
    }

    [UnmanagedCallersOnly]
    private static void DrawSegmentCallback(B2.Vec2 p1, B2.Vec2 p2, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        var a = W2S(p1.x, p1.y);
        var b = W2S(p2.x, p2.y);
        renderer.DrawLine(a.X, a.Y, b.X, b.Y, ToColor(color), 1f);
    }

    [UnmanagedCallersOnly]
    private static void DrawTransformCallback(B2.Transform xf, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        const float axisScreenLength = 20f;

        var xAxis = B2.RotGetXAxis(xf.q);
        var yAxis = B2.RotGetYAxis(xf.q);
        var origin = W2S(xf.p.x, xf.p.y);

        // Project a unit step along each axis into screen space, then extend to a fixed
        // screen-pixel length so the gizmo stays visible at any zoom level.
        var xScreenDir = W2S(xf.p.x + xAxis.x, xf.p.y + xAxis.y) - origin;
        var yScreenDir = W2S(xf.p.x + yAxis.x, xf.p.y + yAxis.y) - origin;

        float xLen = xScreenDir.Length();
        float yLen = yScreenDir.Length();

        var xTip = xLen > 0f ? origin + xScreenDir / xLen * axisScreenLength : origin;
        var yTip = yLen > 0f ? origin + yScreenDir / yLen * axisScreenLength : origin;

        renderer.DrawLine(origin.X, origin.Y, xTip.X, xTip.Y, new Color(255, 0, 0, 200), 2f);
        renderer.DrawLine(origin.X, origin.Y, yTip.X, yTip.Y, new Color(0, 255, 0, 200), 2f);
    }

    [UnmanagedCallersOnly]
    private static void DrawPointCallback(B2.Vec2 p, float size, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer) return;
        var s = W2S(p.x, p.y);
        renderer.DrawCircleFilled(s.X, s.Y, WScale(size * 0.5f), ToColor(color));
    }

    [UnmanagedCallersOnly]
    private static void DrawStringCallback(B2.Vec2 p, byte* str, B2.HexColor color, void* context)
    {
        if (_activeRenderer is not { } renderer || str == null) return;
        string text = Marshal.PtrToStringUTF8((nint)str) ?? string.Empty;
        var s = W2S(p.x, p.y);
        renderer.DrawText(text, s.X, s.Y, ToColor(color));
    }
}