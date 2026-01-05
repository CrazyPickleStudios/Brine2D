using Brine2D.Core;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component for countdown timers with optional looping and events.
/// Useful for delays, cooldowns, and timed actions.
/// </summary>
public class TimerComponent : Component
{
    /// <summary>
    /// Total duration of the timer in seconds.
    /// </summary>
    public float Duration { get; set; } = 1f;

    /// <summary>
    /// Current time remaining in seconds.
    /// </summary>
    public float TimeRemaining { get; set; }

    /// <summary>
    /// Whether the timer should loop when it reaches zero.
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Whether the timer is currently running.
    /// </summary>
    public bool IsRunning { get; set; } = true;

    /// <summary>
    /// Whether the timer has finished (reached zero).
    /// </summary>
    public bool IsFinished => TimeRemaining <= 0 && !Loop;

    /// <summary>
    /// Progress from 0 (just started) to 1 (finished).
    /// </summary>
    public float Progress => Duration > 0 ? 1f - (TimeRemaining / Duration) : 1f;

    /// <summary>
    /// Event fired when timer reaches zero.
    /// </summary>
    public event Action? OnComplete;

    /// <summary>
    /// Event fired every loop iteration (if looping is enabled).
    /// </summary>
    public event Action? OnLoop;

    protected internal override void OnAdded()
    {
        base.OnAdded();
        TimeRemaining = Duration;
    }

    protected internal override void OnUpdate(GameTime gameTime)
    {
        if (!IsRunning || !IsEnabled)
            return;

        TimeRemaining -= (float)gameTime.DeltaTime;

        if (TimeRemaining <= 0)
        {
            OnComplete?.Invoke();

            if (Loop)
            {
                TimeRemaining = Duration;
                OnLoop?.Invoke();
            }
            else
            {
                IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Resets the timer to its full duration.
    /// </summary>
    public void Reset()
    {
        TimeRemaining = Duration;
        IsRunning = true;
    }

    /// <summary>
    /// Pauses the timer.
    /// </summary>
    public void Pause()
    {
        IsRunning = false;
    }

    /// <summary>
    /// Resumes the timer.
    /// </summary>
    public void Resume()
    {
        IsRunning = true;
    }

    /// <summary>
    /// Starts or restarts the timer with a new duration.
    /// </summary>
    public void Start(float duration)
    {
        Duration = duration;
        TimeRemaining = duration;
        IsRunning = true;
    }
}