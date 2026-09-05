using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Rendering;

/// <summary>
/// Demonstrates the fixed-aspect-ratio/letterbox post-processing effect (<c>AddLetterbox</c>).
/// The game world renders at a fixed 1280x720 design resolution regardless of the actual
/// window size — resize the window (including to a very wide or very tall aspect ratio) to
/// see letterbox (top/bottom) or pillarbox (left/right) bars appear, while the design-resolution
/// content itself stays fully visible, undistorted, and centered.
/// </summary>
/// <remarks>
/// <c>AddLetterbox(designWidth, designHeight, barColor)</c> is configured once at startup in
/// <c>Program.cs</c> (design resolution must be fixed before the renderer initializes) — this
/// scene only draws content that makes the effect visible.
/// </remarks>
public class LetterboxDemoScene : DemoSceneBase
{
    private const int DesignWidth = 1280;
    private const int DesignHeight = 720;

    private float _time;

    public LetterboxDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Renderer.ClearColor = new Color(20, 24, 32);

        Logger.LogInformation("=== Letterbox / Fixed-Aspect-Ratio Demo ===");
        Logger.LogInformation("Design resolution: {Width}x{Height}", DesignWidth, DesignHeight);
        Logger.LogInformation("Resize the window (try very wide or very tall) to see letterbox/pillarbox bars.");
        Logger.LogInformation("The border frame below should always stay fully visible and undistorted.");

        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (CheckReturnToMenu()) return;
        _time += (float)gameTime.DeltaTime;
        HandlePerformanceHotkeys();
    }

    protected override void OnRender(GameTime gameTime)
    {
        DrawDesignResolutionBorder();
        DrawCorderMarkers();
        DrawBouncingBall();
        DrawHeader();
        RenderPerformanceOverlay();
    }

    private void DrawHeader()
    {
        Renderer.DrawText("Letterbox / Fixed-Aspect-Ratio Demo", 20, 18, Color.White);
        Renderer.DrawText(
            $"Design resolution: {DesignWidth}x{DesignHeight}  |  Resize the window to see bars appear",
            20, 44, new Color(160, 160, 180));
        Renderer.DrawText(
            "The white border below marks the edges of the design resolution — it should never be clipped or stretched.",
            20, 68, new Color(160, 160, 180));
    }

    private void DrawDesignResolutionBorder()
    {
        var border = new Color(255, 255, 255, 220);
        Renderer.DrawLine(1, 1, DesignWidth - 1, 1, border, 3f);
        Renderer.DrawLine(1, DesignHeight - 1, DesignWidth - 1, DesignHeight - 1, border, 3f);
        Renderer.DrawLine(1, 1, 1, DesignHeight - 1, border, 3f);
        Renderer.DrawLine(DesignWidth - 1, 1, DesignWidth - 1, DesignHeight - 1, border, 3f);
    }

    private void DrawCorderMarkers()
    {
        var markerColor = new Color(255, 210, 90);
        const float size = 24f;

        DrawCornerMarker(size, size, markerColor);
        DrawCornerMarker(DesignWidth - size, size, markerColor);
        DrawCornerMarker(size, DesignHeight - size, markerColor);
        DrawCornerMarker(DesignWidth - size, DesignHeight - size, markerColor);
    }

    private void DrawCornerMarker(float x, float y, Color color)
    {
        Renderer.DrawCircleFilled(x, y, 10, color);
        Renderer.DrawCircleOutline(x, y, 10, Color.White, 2f);
    }

    private void DrawBouncingBall()
    {
        float radius = 40f;
        float rangeX = DesignWidth - radius * 2f;
        float rangeY = DesignHeight - radius * 2f;

        float x = radius + (MathF.Sin(_time * 0.7f) * 0.5f + 0.5f) * rangeX;
        float y = radius + (MathF.Cos(_time * 0.9f) * 0.5f + 0.5f) * rangeY;

        Renderer.DrawCircleFilled(x, y, radius, new Color(120, 180, 255, 220));
        Renderer.DrawCircleOutline(x, y, radius, Color.White, 2f);
    }
}
