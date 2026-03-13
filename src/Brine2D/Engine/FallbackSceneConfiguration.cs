namespace Brine2D.Engine;

/// <summary>
/// Carries the fallback scene type registered via
/// <see cref="Hosting.GameApplicationBuilder.UseFallbackScene{T}"/>.
/// Defaults to <see cref="DefaultFallbackScene"/>.
/// Consumed by <see cref="SceneManager.RaiseSceneLoadFailedIfPending"/>.
/// </summary>
internal sealed class FallbackSceneConfiguration(Type fallbackSceneType)
{
    internal Type FallbackSceneType { get; } = fallbackSceneType;
}