using Brine2D.Core;

namespace Brine2D.Engine;

/// <summary>
/// Represents the context of the running game application.
/// Similar to HttpContext in ASP.NET.
/// </summary>
public interface IGameContext
{
    /// <summary>Gets the current game time information.</summary>
    GameTime GameTime { get; }

    /// <summary>Gets a value indicating whether the game is running.</summary>
    bool IsRunning { get; }

    /// <summary>Requests the game to exit.</summary>
    void RequestExit();

    /// <summary>
    /// Updates the current game time. Called once per frame by <see cref="GameLoop"/>.
    /// Not part of the public game API; exposed only within the engine assembly.
    /// </summary>
    internal void UpdateGameTime(GameTime gameTime);
}
