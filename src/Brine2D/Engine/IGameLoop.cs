using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Engine
{
    /// <summary>
    /// Represents the main game loop.
    /// </summary>
    public interface IGameLoop
    {
        /// <summary>
        /// Gets a value indicating whether the game loop is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the target frames per second.
        /// </summary>
        int TargetFramesPerSecond { get; set; }

        /// <summary>
        /// Starts the game loop.
        /// </summary>
        Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the game loop.
        /// </summary>
        void Stop();
    }
}
