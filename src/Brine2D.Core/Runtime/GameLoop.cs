using System.Diagnostics;
using Brine2D.Core.Timing;

namespace Brine2D.Core.Runtime;

/// <summary>
///     A fixed-timestep game loop with optional frame limiting via sleep.
///     It accumulates real time into an <c>accumulator</c>, runs a fixed number of simulation updates,
///     and renders once per frame using the actual frame time.
/// </summary>
public sealed class GameLoop
{
    private readonly GameLoopOptions _options;

    /// <summary>
    ///     Creates a new game loop instance with sanitized options.
    /// </summary>
    /// <param name="options">
    ///     Configuration that controls target FPS, max updates per frame, frame delta clamp, and sleeping.
    ///     Values are clamped to safe ranges.
    /// </param>
    public GameLoop(GameLoopOptions options)
    {
        _options = options with
        {
            // Clamp target FPS to avoid degenerate or extreme values
            TargetFps = System.Math.Clamp(options.TargetFps, 15, 1000),
            // Ensure at least one update can occur per frame
            MaxUpdatesPerFrame = System.Math.Max(1, options.MaxUpdatesPerFrame),
            // Prevent a single frame from contributing an excessively large delta
            MaxFrameDeltaSeconds = System.Math.Max(1.0 / 240.0, options.MaxFrameDeltaSeconds)
        };
    }

    /// <summary>
    ///     Runs the main loop until <paramref name="update" /> returns false.
    /// </summary>
    /// <param name="update">
    ///     Simulation step called with a fixed delta time: <see cref="GameTime.DeltaSeconds" /> equals 1 / TargetFps.
    ///     Return false to request loop termination.
    /// </param>
    /// <param name="draw">
    ///     Render step called once per frame using the real frame delta time (clamped via options).
    /// </param>
    public void Run(Func<GameTime, bool> update, Action<GameTime> draw)
    {
        // Fixed simulation delta based on target FPS
        var fixedDt = 1.0 / _options.TargetFps;

        // High-resolution timer for frame timing
        var sw = Stopwatch.StartNew();
        var last = sw.Elapsed.TotalSeconds; // time of previous frame
        var accumulator = 0.0; // accumulated un-simulated time
        var simTime = 0.0; // total simulated time (monotonic by fixedDt)

        while (true)
        {
            // Measure frame duration
            var now = sw.Elapsed.TotalSeconds;
            var frameDelta = now - last;
            last = now;

            // Clamp large hitches to stabilize physics
            if (frameDelta > _options.MaxFrameDeltaSeconds)
            {
                frameDelta = _options.MaxFrameDeltaSeconds;
            }

            accumulator += frameDelta;

            // Run a bounded number of fixed updates per frame to catch up
            var updates = 0;
            while (accumulator >= fixedDt && updates < _options.MaxUpdatesPerFrame)
            {
                // Update with fixed timestep; exit loop if update returns false
                if (!update(new GameTime(simTime, fixedDt)))
                {
                    return;
                }

                simTime += fixedDt;
                accumulator -= fixedDt;
                updates++;
            }

            // Draw uses the real (clamped) frame delta for interpolation-friendly rendering
            draw(new GameTime(simTime, frameDelta));

            if (_options.EnableSleep)
            {
                // Basic frame limiting: sleep the remainder of the target frame if under budget
                var targetFrame = fixedDt;
                var workTime = sw.Elapsed.TotalSeconds - now; // time spent this frame so far
                var sleepSeconds = targetFrame - workTime;
                if (sleepSeconds > 0)
                {
                    var ms = (int)(sleepSeconds * 1000.0);
                    if (ms > 0)
                    {
                        Thread.Sleep(ms);
                    }
                }
            }
        }
    }
}

public readonly record struct GameLoopOptions(
    double TargetFps = 60.0,
    int MaxUpdatesPerFrame = 4,
    double MaxFrameDeltaSeconds = 0.25, // clamp large hitches
    bool EnableSleep = true);