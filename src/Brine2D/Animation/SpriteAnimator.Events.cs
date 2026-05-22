namespace Brine2D.Animation;

public partial class SpriteAnimator
{
    private void FireClipEvents(float prevTime, float newTime, bool wasForward = true)
    {
        var clip = _currentAnimation!;
        var events = clip.Events;
        var total = clip.TotalDuration;

        if (total <= 0f)
            return;

        switch (clip.PlaybackMode)
        {
            case PlaybackMode.OnceHoldFirst:
            case PlaybackMode.OnceHoldLast:
            case PlaybackMode.OnceStop:
                {
                    if (_reversed)
                    {
                        foreach (var e in events)
                        {
                            if (e.Time >= newTime && e.Time < prevTime)
                                e.Callback(BuildArgs(e, clip, total));
                        }
                    }
                    else
                    {
                        var clampedNew = Math.Min(newTime, total);
                        foreach (var e in events)
                        {
                            if (e.Time > prevTime && e.Time <= clampedNew && e.Time < total)
                                e.Callback(BuildArgs(e, clip, total));
                        }
                    }
                    break;
                }
            case PlaybackMode.Loop:
                {
                    if (_reversed)
                        FireEventsInWindowReversed(events, prevTime, newTime, total, clip, backwardOnly: false);
                    else
                        FireEventsInWindow(events, prevTime, newTime, total, clip);
                    break;
                }
            case PlaybackMode.PingPong:
            case PlaybackMode.PingPongOnce:
                {
                    if (newTime < prevTime)
                    {
                        FireEventsInWindowReversed(events, prevTime % total, newTime % total, total, clip, backwardOnly: true);
                        break;
                    }

                    long prevCycle = (long)(prevTime / total);
                    long newCycle = (long)(newTime / total);
                    bool flippedThisStep = newCycle > prevCycle;

                    if (!flippedThisStep)
                    {
                        if (wasForward)
                            FireEventsInWindow(events, prevTime % total, newTime % total, total, clip);
                        else
                            FireEventsInWindowReversed(events, prevTime % total, newTime % total, total, clip, backwardOnly: true);
                    }
                    else
                    {
                        if (wasForward)
                        {
                            float forwardFrom = prevTime % total;
                            foreach (var e in events)
                            {
                                if (e.Time > forwardFrom)
                                    e.Callback(BuildArgs(e, clip, total));
                            }
                            FireEventsInWindowReversed(events, total, newTime % total, total, clip, backwardOnly: true);
                        }
                        else
                        {
                            FireEventsInWindowReversed(events, prevTime % total, 0f, total, clip, backwardOnly: true);
                            FireEventsInWindow(events, 0f, newTime % total, total, clip);
                        }
                    }
                    break;
                }
        }
    }

    private ClipEventArgs BuildArgs(ClipEvent e, AnimationClip clip, float total) =>
        new(e.Name, clip.Name, e.Time, total > 0f ? e.Time / total : 0f);

    private void FireEventsInWindow(IReadOnlyList<ClipEvent> events, float prevTime, float newTime, float total, AnimationClip clip)
    {
        float wrappedPrev = prevTime % total;
        float wrappedNew = newTime % total;
        bool wrapped = wrappedNew < wrappedPrev;

        if (!wrapped)
        {
            foreach (var e in events)
            {
                if (e.Time > wrappedPrev && e.Time <= wrappedNew)
                    e.Callback(BuildArgs(e, clip, total));
            }
        }
        else
        {
            foreach (var e in events)
            {
                if (e.Time > wrappedPrev || e.Time <= wrappedNew)
                    e.Callback(BuildArgs(e, clip, total));
            }
        }
    }

    private void FireEventsInWindowReversed(
        IReadOnlyList<ClipEvent> events,
        float prevTime,
        float newTime,
        float total,
        AnimationClip clip,
        bool backwardOnly)
    {
        bool wrapped = newTime > prevTime;

        if (!wrapped)
        {
            foreach (var e in events)
            {
                if (backwardOnly && !e.FireBothDirections) continue;
                if (e.Time < prevTime && e.Time >= newTime)
                    e.Callback(BuildArgs(e, clip, total));
            }
        }
        else
        {
            foreach (var e in events)
            {
                if (backwardOnly && !e.FireBothDirections) continue;
                if (e.Time < prevTime || e.Time >= newTime)
                    e.Callback(BuildArgs(e, clip, total));
            }
        }
    }
}