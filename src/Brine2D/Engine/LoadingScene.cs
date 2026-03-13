using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.Engine;

/// <summary>
/// Base class for loading screens.
/// Framework properties (Logger, Renderer) are set automatically by SceneManager.
/// Loading screens do not have a World; they render between scene scopes.
/// Override <see cref="SceneBase.OnEnter"/> and <see cref="SceneBase.OnExit"/> for
/// one-time setup and teardown (e.g., starting music). Both are called on the main thread.
/// </summary>
public abstract class LoadingScene : SceneBase
{
    private const float BarWidthRatio = 0.4f;
    private const float BarHeightRatio = 0.025f;
    private const float LabelYOffsetMultiplier = 2f;
    private const float PercentYOffsetMultiplier = 1.5f;
    private const float OutlineThickness = 2f;

    private static readonly TextRenderOptions DefaultLoadingTextOptions = new() { Color = Color.White, FontSize = 16f };

    private sealed record ProgressSnapshot(float Progress, string Message);

    // Single volatile reference; a reference write is an atomic pointer store on all .NET platforms,
    // so readers always observe a consistent progress + message pair from the same snapshot.
    private volatile ProgressSnapshot _progress = new(0f, "Loading...");

    protected LoadingScene() { }

    /// <summary>Gets the current loading progress (0.0–1.0). Safe to read from any thread.</summary>
    protected float LoadingProgress => _progress.Progress;

    /// <summary>Gets the current loading message. Safe to read from any thread.</summary>
    protected string LoadingMessage => _progress.Message;

    /// <summary>
    /// Text render options used by <see cref="OnRenderLoading"/>.
    /// Override to specify a font or adjust the size for the default progress bar UI.
    /// </summary>
    protected virtual TextRenderOptions LoadingTextOptions => DefaultLoadingTextOptions;

    /// <summary>
    /// Updates the loading progress (0.0 to 1.0).
    /// Safe to call from background threads during scene loading.
    /// </summary>
    public void UpdateProgress(float progress, string? message = null)
    {
        var current = _progress;
        _progress = new ProgressSnapshot(
            Math.Clamp(progress, 0f, 1f),
            message ?? current.Message);
    }

    protected internal sealed override void OnRender(GameTime gameTime) => OnRenderLoading(gameTime);

    /// <summary>Override to customize the loading screen appearance.</summary>
    protected virtual void OnRenderLoading(GameTime gameTime)
    {
        var barWidth = Renderer.Width * BarWidthRatio;
        var barHeight = Renderer.Height * BarHeightRatio;
        var barX = Renderer.Width / 2f - barWidth / 2f;
        var barY = Renderer.Height / 2f - barHeight / 2f;

        var textOptions = LoadingTextOptions;

        Renderer.DrawText(LoadingMessage, barX, barY - barHeight * LabelYOffsetMultiplier, textOptions);

        Renderer.DrawRectangleFilled(barX, barY, barWidth, barHeight, new Color(50, 50, 50));
        Renderer.DrawRectangleFilled(barX, barY, barWidth * LoadingProgress, barHeight, Color.White);
        Renderer.DrawRectangleOutline(barX, barY, barWidth, barHeight, new Color(150, 150, 150), OutlineThickness);

        Renderer.DrawText($"{(int)(LoadingProgress * 100)}%", barX, barY + barHeight * PercentYOffsetMultiplier, textOptions);
    }
}