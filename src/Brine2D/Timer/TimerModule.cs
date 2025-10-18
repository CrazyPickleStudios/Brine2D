using static SDL.SDL3;

namespace Brine2D.Timer;

/// <summary>
///     Provides high-resolution timing functionality.
/// </summary>
public sealed class TimerModule
{
    private static readonly ulong s_frequency = SDL_GetPerformanceFrequency();
    private static readonly ulong s_start = SDL_GetPerformanceCounter();
    private double _averageDelta;
    private double _currTime;
    private double _dt;
    private int _fps;
    private readonly double _fpsUpdateFrequency;
    private int _frames;
    private double _prevFpsUpdate;
    private double _prevTime;

    internal TimerModule()
    {
        _currTime = 0;
        _prevFpsUpdate = 0;
        _fps = 0;
        _averageDelta = 0;
        _fpsUpdateFrequency = 1;
        _frames = 0;
        _dt = 0;
    }

    /// <summary>
    ///     Returns the average delta time (seconds per frame) over the last second.
    /// </summary>
    /// <returns>
    ///     The average delta time over the last second.
    /// </returns>
    public double GetAverageDelta()
    {
        return _averageDelta;
    }

    /// <summary>
    ///     Returns the time between the last two frames.
    /// </summary>
    /// <returns>
    ///     The time passed (in seconds).
    /// </returns>
    public double GetDelta()
    {
        return _dt;
    }

    /// <summary>
    ///     Returns the current number of frames per second.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The returned value is the number of frames rendered during the last second, rounded to the nearest integer
    ///         value.
    ///     </para>
    ///     <para>
    ///         It is one divided by what love.timer.getAverageDelta returns, otherwise known as the reciprocal, or
    ///         multiplicative inverse of it.
    ///     </para>
    ///     <para>
    ///         To get instantaneous frame rate values, use 1.0 / love.timer.getDelta(), or 1.0 / dt if in love.update, with
    ///         dt given as the parameter.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The current FPS.
    /// </returns>
    public double GetFPS()
    {
        return _fps;
        ;
    }

    /// <summary>
    ///     <para>Returns the value of a precise timer with an unspecified starting time.</para>
    ///     <para>
    ///         This function should only be used to calculate differences between points in time, as the starting time of
    ///         the timer is unknown prior to version 11.4. Beginning with 11.4, the timer is initialized to 0 when the
    ///         love.timer module is first loaded (usually before main.lua is loaded).
    ///     </para>
    /// </summary>
    /// <returns>
    ///     The time in seconds. Given as a decimal, accurate to the microsecond.
    /// </returns>
    public double GetTime()
    {
        var now = SDL_GetPerformanceCounter();
        var rel = now - s_start;
        return (double)rel / s_frequency;
    }

    /// <summary>
    ///     Pauses the current thread for the specified amount of time.
    /// </summary>
    /// <param name="s">Seconds to sleep for.</param>
    public void Sleep(double s)
    {
        if (s >= 0)
            SDL_DelayNS(unchecked((ulong)(SDL_NS_PER_MS * (s * 1000))));
    }

    /// <summary>
    ///     <para>Measures the time between two frames.</para>
    ///     <para>Calling this changes the return value of love.timer.getDelta.</para>
    /// </summary>
    /// <returns>The time passed (in seconds).</returns>
    public double Step()
    {
        _frames++;
        _prevTime = _currTime;
        _currTime = GetTime();
        _dt = _currTime - _prevTime;

        var timeSinceLast = _currTime - _prevFpsUpdate;

        if (timeSinceLast > _fpsUpdateFrequency)
        {
            _fps = (int)SysMath.Round(_frames / timeSinceLast);
            _averageDelta = timeSinceLast / _frames;
            _prevFpsUpdate = _currTime;
            _frames = 0;
        }

        return _dt;
    }
}