namespace Brine2D.Hosting;

/// <summary>
/// Holds the set of scene types pre-registered at build time via
/// <see cref="GameApplicationBuilder.AddScene{T}"/>.
/// Consumed by <see cref="Brine2D.Engine.SceneManager"/> for fast scene-type lookup.
/// </summary>
internal sealed record RegisteredSceneRegistry(IReadOnlySet<Type> SceneTypes);