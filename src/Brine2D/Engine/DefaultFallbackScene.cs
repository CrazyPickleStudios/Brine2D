using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Engine;

/// <summary>
/// The built-in fallback scene displayed when a scene load fails and
/// <see cref="ISceneManager.SceneLoadFailed"/> has no handler that queues a recovery transition.
/// Shows the failed scene name and exception message.
/// Replace it project-wide with <c>builder.UseFallbackScene&lt;T&gt;()</c>.
/// </summary>
public sealed class DefaultFallbackScene : Scene
{
    private readonly ISceneLoadErrorInfo _errorInfo;

    public DefaultFallbackScene(ISceneLoadErrorInfo errorInfo)
    {
        _errorInfo = errorInfo;
    }

    internal protected override void OnRender(GameTime gameTime)
    {
        var centerX = Renderer.Width / 2f;
        var centerY = Renderer.Height / 2f;

        Renderer.DrawText("Scene Load Failed", centerX - 100, centerY - 60, Color.Red);
        Renderer.DrawText($"Scene:  {_errorInfo.FailedSceneName ?? "unknown"}",
            centerX - 150, centerY - 20, Color.White);
        Renderer.DrawText($"Error:  {_errorInfo.Exception?.Message ?? "unknown error"}",
            centerX - 150, centerY + 20, new Color(200, 200, 200));
        Renderer.DrawText("Replace with builder.UseFallbackScene<T>()",
            centerX - 175, centerY + 60, new Color(150, 150, 100));
    }
}