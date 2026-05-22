using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

public partial class SpriteAnimator
{
    /// <summary>
    /// Captures all runtime playback state into an immutable <see cref="AnimatorPlaybackSnapshot"/>.
    /// Use with <see cref="RestorePlaybackSnapshot"/> for save/load, rollback netcode, and
    /// cutscene/ability override systems.
    /// </summary>
    public AnimatorPlaybackSnapshot CapturePlaybackSnapshot() =>
        new(
            ClipName: _currentAnimation?.Name,
            FrameIndex: _currentFrameIndex,
            ClipTime: _clipTime,
            FrameTimer: _frameTimer,
            IsPlaying: _isPlaying,
            IsPaused: _isPaused,
            Reversed: _reversed,
            PingPongForward: _pingPongForward,
            PingPongFirstPassDone: _pingPongFirstPassDone,
            LoopCountRemaining: _loopCountRemaining,
            Speed: _speed,
            CrossFadeAlpha: _crossFadeAlpha,
            CrossFadeTimer: _crossFadeTimer,
            CrossFadeDuration: _crossFadeDuration,
            CrossFadeBaseAlpha: CrossFadeBaseAlpha,
            CrossFadeOutgoingClipName: CrossFadeOutgoingClip?.Name,
            CrossFadeOutgoingFrameIndex: CrossFadeOutgoingFrame != null && CrossFadeOutgoingClip != null
                ? FindFrameIndex(CrossFadeOutgoingClip, CrossFadeOutgoingFrame)
                : -1);

    /// <remarks>
    /// No events fire during restore (<see cref="OnAnimationStart"/>, frame enter/exit, clip
    /// enter/exit). Raise them afterward if needed.
    /// </remarks>
    public bool RestorePlaybackSnapshot(AnimatorPlaybackSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        AnimationClip? clip = null;
        if (snapshot.ClipName != null && !_animations.TryGetValue(snapshot.ClipName, out clip))
        {
            _logger?.LogWarning(
                "RestorePlaybackSnapshot: clip '{Name}' is not registered on this animator. " +
                "Snapshot was not applied. Register the clip before restoring, or play it via PlayDirect.",
                snapshot.ClipName);
            return false;
        }

        _currentAnimation = clip;
        _currentFrameIndex = clip != null
            ? Math.Clamp(snapshot.FrameIndex, 0, clip.Frames.Count - 1)
            : 0;
        _clipTime = snapshot.ClipTime;
        _frameTimer = snapshot.FrameTimer;
        _isPlaying = snapshot.IsPlaying;
        _isPaused = snapshot.IsPaused;
        _reversed = snapshot.Reversed;
        _pingPongForward = snapshot.PingPongForward;
        _pingPongFirstPassDone = snapshot.PingPongFirstPassDone;
        _loopCountRemaining = snapshot.LoopCountRemaining;
        _speed = MathF.Max(snapshot.Speed, 0f);
        _crossFadeAlpha = snapshot.CrossFadeAlpha;
        _crossFadeTimer = snapshot.CrossFadeTimer;
        _crossFadeDuration = snapshot.CrossFadeDuration;
        CrossFadeBaseAlpha = snapshot.CrossFadeBaseAlpha;

        if (snapshot.CrossFadeOutgoingClipName != null
            && _animations.TryGetValue(snapshot.CrossFadeOutgoingClipName, out var outgoingClip)
            && snapshot.CrossFadeOutgoingFrameIndex >= 0
            && snapshot.CrossFadeOutgoingFrameIndex < outgoingClip.Frames.Count)
        {
            CrossFadeOutgoingClip = outgoingClip;
            CrossFadeOutgoingFrame = outgoingClip.Frames[snapshot.CrossFadeOutgoingFrameIndex];
        }
        else
        {
            CrossFadeOutgoingClip = null;
            CrossFadeOutgoingFrame = null;
            if (snapshot.CrossFadeDuration > 0f && snapshot.CrossFadeOutgoingClipName != null)
            {
                _crossFadeDuration = 0f;
                _crossFadeAlpha = 1f;
            }
        }

        return true;
    }

    private static int FindFrameIndex(AnimationClip clip, SpriteFrame frame)
    {
        var frames = clip.Frames;
        for (int i = 0; i < frames.Count; i++)
            if (ReferenceEquals(frames[i], frame)) return i;
        return -1;
    }
}