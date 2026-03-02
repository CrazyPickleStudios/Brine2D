namespace Brine2D.Hosting;

/// <summary>
/// Thrown when Brine2D configuration is invalid at build time or host startup.
/// Inherits from <see cref="InvalidOperationException"/> so existing catch sites remain valid.
/// </summary>
public sealed class GameConfigurationException : InvalidOperationException
{
    public GameConfigurationException(string message) : base(message) { }

    public GameConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}