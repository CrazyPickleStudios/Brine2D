namespace Brine2D.Engine;

/// <summary>
/// Thrown when the game engine fails to initialize (renderer, SDL3, etc.).
/// Inherits from <see cref="InvalidOperationException"/> so existing catch sites remain valid.
/// </summary>
public sealed class EngineInitializationException : InvalidOperationException
{
    public EngineInitializationException(string message) : base(message) { }

    public EngineInitializationException(string message, Exception innerException)
        : base(message, innerException) { }
}