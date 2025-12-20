namespace Brine2D.Engine;

/// <summary>
/// Core game engine that coordinates subsystems (rendering, input, audio, etc.).
/// </summary>
public interface IGameEngine
{
    /// <summary>
    /// Gets a value indicating whether the engine is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes all engine subsystems.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down all engine subsystems.
    /// </summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}