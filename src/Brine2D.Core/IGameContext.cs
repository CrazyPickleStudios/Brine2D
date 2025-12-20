using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Core
{
    /// <summary>
    /// Represents the context of the running game application.
    /// Similar to HttpContext in ASP.NET.
    /// </summary>
    public interface IGameContext
    {
        /// <summary>
        /// Gets the service provider for the current game context.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Gets the current game time information.
        /// </summary>
        GameTime GameTime { get; }

        /// <summary>
        /// Gets a value indicating whether the game is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Requests the game to exit.
        /// </summary>
        void RequestExit();
    }
}
