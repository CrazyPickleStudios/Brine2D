using Brine2D.Core;

namespace Brine2D.Engine;

/// <summary>
/// Frame-level contract consumed exclusively by <see cref="GameLoop"/>.
/// Drives the scene system through each frame's lifecycle: begin, fixed update, update, render, and deferred work.
/// </summary>
internal interface ISceneLoop
{
    /// <summary>
    /// The currently in-flight scene load task, or <see langword="null"/> if no load is active.
    /// <see cref="GameLoop"/> polls this each frame to detect and report faulted loads independently
    /// of the deferred-transition mechanism.
    /// </summary>
    Task? ActiveLoadTask { get; }

    /// <summary>
    /// Maximum time to wait for an in-flight scene load to complete during shutdown.
    /// Sourced from <see cref="Hosting.Brine2DOptions.ShutdownTimeout"/>.
    /// </summary>
    TimeSpan ShutdownTimeout { get; }

    void BeginFrame();
    void ProcessDeferredTransitions(CancellationToken ct);
    void RaiseSceneLoadFailedIfPending();
    void FixedUpdate(GameTime fixedTime);
    void Update(GameTime gameTime);
    void Render(GameTime gameTime);
}