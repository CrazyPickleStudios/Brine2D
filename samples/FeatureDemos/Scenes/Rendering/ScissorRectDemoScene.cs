using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Performance;
using Brine2D.Rendering;
using FeatureDemos.Scenes;
using Microsoft.Extensions.Logging;

namespace FeatureDemos.Scenes.Rendering;

/// <summary>
/// Demonstrates the raw scissor-rect clipping API available on IRenderer.
/// Shows three distinct use-cases:
///   1. SetScissorRect  — hard clip with no stack
///   2. Push/Pop stack  — nested clips that intersect correctly
///   3. WithScissorRect — scope-based clip via Action callback
/// </summary>
public class ScissorRectDemoScene : DemoSceneBase
{
    private float _time;

    public ScissorRectDemoScene(
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Renderer.ClearColor = new Color(25, 25, 35);
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
        DrawHeader();
        DrawSetScissorDemo();
        DrawPushPopDemo();
        DrawWithScissorDemo();
        DrawFooter();
        RenderPerformanceOverlay();
    }

    private void DrawHeader()
    {
        Renderer.DrawText("Scissor Rect Demo", 20, 18, Color.White);
        Renderer.DrawText("Each panel clips a moving circle to illustrate a different clipping API call.",
            20, 44, new Color(160, 160, 180));
        Renderer.DrawLine(20, 68, Renderer.Width - 20, 68, new Color(60, 60, 80));
    }

    private void DrawSetScissorDemo()
    {
        var panel = new Rectangle(40, 90, 350, 240);
        DrawPanelBackground(panel, new Color(50, 50, 70), new Color(120, 180, 255), "SetScissorRect");

        var clip = new Rectangle(panel.X + 10, panel.Y + 42, panel.Width - 20, panel.Height - 52);
        DrawClipOutline(clip, new Color(120, 180, 255, 100));

        float cx = clip.X + clip.Width / 2f + MathF.Sin(_time * 1.4f) * (clip.Width / 2f + 30f);
        float cy = clip.Y + clip.Height / 2f;

        Renderer.SetScissorRect(clip);
        Renderer.DrawCircleFilled(cx, cy, 50, new Color(120, 180, 255, 200));
        Renderer.DrawCircleOutline(cx, cy, 50, Color.White, 2f);
        Renderer.SetScissorRect(null);

        DrawPanelLabel(panel,
            "Renderer.SetScissorRect(rect)",
            "Replaces any current clip with a single rectangle.");
    }

    private void DrawPushPopDemo()
    {
        var panel = new Rectangle(430, 90, 390, 240);
        DrawPanelBackground(panel, new Color(40, 60, 40), new Color(100, 220, 120), "Push / Pop");

        var outer = new Rectangle(panel.X + 10, panel.Y + 42, panel.Width - 20, panel.Height - 52);
        var inner = new Rectangle(outer.X + 40, outer.Y + 30, outer.Width - 80, outer.Height - 60);

        DrawClipOutline(outer, new Color(100, 220, 120, 80));
        DrawClipOutline(inner, new Color(220, 220, 80, 140));

        float cx = outer.X + outer.Width / 2f + MathF.Sin(_time * 0.7f) * (outer.Width / 2f + 30f);
        float cy = outer.Y + outer.Height / 2f;

        Renderer.PushScissorRect(outer);
        Renderer.DrawCircleFilled(cx, cy, 55, new Color(100, 220, 120, 160));

        Renderer.PushIntersectedScissorRect(inner);
        Renderer.DrawCircleFilled(cx, cy, 55, new Color(240, 240, 100, 230));
        Renderer.PopScissorRect();

        Renderer.PopScissorRect();

        DrawPanelLabel(panel,
            "PushScissorRect → PushIntersectedScissorRect → Pop × 2",
            "Green = outer only.  Yellow = intersection of outer + inner.");
    }

    private void DrawWithScissorDemo()
    {
        var panel = new Rectangle(860, 90, 370, 240);
        DrawPanelBackground(panel, new Color(60, 40, 40), new Color(220, 120, 80), "WithScissorRect");

        var zone = new Rectangle(panel.X + 10, panel.Y + 42, panel.Width - 20, panel.Height - 52);
        int thirds = (int)(zone.Width / 3);

        var columns = new[]
        {
            (new Rectangle(zone.X,           zone.Y, thirds, zone.Height), new Color(220, 80,  80,  210), -1f),
            (new Rectangle(zone.X + thirds,  zone.Y, thirds, zone.Height), new Color(220, 160, 60,  210),  0f),
            (new Rectangle(zone.X + thirds*2,zone.Y, thirds, zone.Height), new Color(80,  140, 220, 210),  1f),
        };

        float t = _time * 1.1f;
        foreach (var (col, color, phase) in columns)
        {
            var c = col;
            float cy = c.Y + c.Height / 2f + MathF.Sin(t + phase) * (c.Height / 2f + 20f);
            Renderer.WithScissorRect(c, () =>
                Renderer.DrawCircleFilled(c.X + c.Width / 2f, cy, 38, color));
            Renderer.DrawRectangleOutline(c, new Color(70, 70, 70), 1f);
        }

        DrawPanelLabel(panel,
            "Renderer.WithScissorRect(rect, () => { … })",
            "Scope-based: clip is automatically restored after the callback.");
    }

    private void DrawFooter()
    {
        float y = Renderer.Height - 36;
        Renderer.DrawLine(20, y - 8, Renderer.Width - 20, y - 8, new Color(60, 60, 80));
        Renderer.DrawText("ESC — back to menu", 20, y, new Color(140, 140, 160));
    }

    private void DrawPanelBackground(Rectangle panel, Color bg, Color border, string title)
    {
        Renderer.DrawRectangleFilled(panel, bg);
        Renderer.DrawRectangleOutline(panel, border, 2f);
        Renderer.DrawText(title, panel.X + 8, panel.Y + 8, border);
    }

    private void DrawClipOutline(Rectangle clip, Color color) =>
        Renderer.DrawRectangleOutline(clip, color, 1f);

    private void DrawPanelLabel(Rectangle panel, string code, string description)
    {
        float y = panel.Y + panel.Height + 10;
        Renderer.DrawText(code, panel.X, y, new Color(200, 200, 220));
        Renderer.DrawText(description, panel.X, y + 22, new Color(130, 130, 150));
    }
}