using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

public partial class SpriteAnimator
{
    private void StartClip(AnimationClip animation, int frameIndex, float frameTimer = 0f)
    {
        if (_currentAnimation != null)
        {
            if (_currentFrameIndex < _currentAnimation.Frames.Count)
                _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
            _currentAnimation.RaiseOnExit();
        }

        _currentAnimation = animation;
        _currentFrameIndex = frameIndex;
        _frameTimer = frameTimer;
        _pingPongForward = !_reversed;
        _pingPongFirstPassDone = false;
        _loopCountRemaining = animation.RepeatCount > 0 ? animation.RepeatCount : -1;
        _isPaused = false;

        float clipTime = frameTimer;
        for (int i = 0; i < frameIndex; i++)
            clipTime += animation.Frames[i].Duration;
        _clipTime = clipTime;

        _isPlaying = true;

        animation.RaiseOnEnter();
        OnAnimationStart?.Invoke(animation);
        animation.Frames[_currentFrameIndex].RaiseOnEnter();
        OnFrameChanged?.Invoke(animation.Frames[_currentFrameIndex]);
    }

    private static (int frameIndex, float frameTimer) ResolveTimeToFrame(AnimationClip animation, float time)
    {
        var total = animation.TotalDuration;
        bool isNonLooping = animation.PlaybackMode is PlaybackMode.OnceHoldLast
            or PlaybackMode.OnceHoldFirst
            or PlaybackMode.OnceStop
            or PlaybackMode.PingPongOnce
            or PlaybackMode.PingPong;
        var t = isNonLooping
            ? Math.Clamp(time, 0f, total)
            : (total > 0f ? ((time % total) + total) % total : 0f);

        float elapsed = 0f;
        for (int i = 0; i < animation.Frames.Count; i++)
        {
            float frameDuration = animation.Frames[i].Duration;
            if (t < elapsed + frameDuration || i == animation.Frames.Count - 1)
                return (i, t - elapsed);
            elapsed += frameDuration;
        }

        return (animation.Frames.Count - 1, 0f);
    }

    private void CompleteWithClear(AnimationClip clip)
    {
        _isPlaying = false;
        OnAnimationComplete?.Invoke(clip);
        if (_currentAnimation == null)
            return;
        if (!_isPlaying)
        {
            var stoppedClip = _currentAnimation;
            _currentAnimation = null;
            _currentFrameIndex = 0;
            _frameTimer = 0f;
            _clipTime = 0f;
            OnStopped?.Invoke(stoppedClip);
        }
    }

    private void AdvanceForward()
    {
        var frames = _currentAnimation!.Frames;
        var currentFrame = frames[_currentFrameIndex];
        int iterations = 0;

        while (_isPlaying && _currentAnimation != null && _frameTimer >= currentFrame.Duration)
        {
            if (++iterations > MaxFrameAdvanceIterations)
            {
                _logger?.LogWarning(
                    "AdvanceForward: frame-skip limit ({Limit}) reached for clip '{Name}'. " +
                    "Delta is too large or frame durations are extremely short.",
                    MaxFrameAdvanceIterations, _currentAnimation.Name);
                _frameTimer %= currentFrame.Duration;
                break;
            }

            _frameTimer -= currentFrame.Duration;
            currentFrame.RaiseOnExit();

            if (_currentAnimation == null)
                return;

            _currentFrameIndex++;

            if (_currentFrameIndex >= frames.Count)
            {
                switch (_currentAnimation.PlaybackMode)
                {
                    case PlaybackMode.Loop:
                        {
                            if (_loopCountRemaining > 0)
                            {
                                _loopCountRemaining--;
                                if (_loopCountRemaining == 0)
                                {
                                    _currentFrameIndex = frames.Count - 1;
                                    _frameTimer = 0f;
                                    _clipTime = _currentAnimation.TotalDuration;
                                    _isPlaying = false;
                                    _logger?.LogDebug("Animation finished (RepeatCount): {Name}", _currentAnimation.Name);
                                    OnLoopComplete?.Invoke(_currentAnimation);
                                    OnAnimationComplete?.Invoke(_currentAnimation);
                                    if (!_isPlaying) PlayQueuedIfPending();
                                    return;
                                }
                            }
                            _currentFrameIndex = 0;
                            _clipTime %= _currentAnimation.TotalDuration;
                            frames[0].RaiseOnEnter();
                            OnFrameChanged?.Invoke(frames[0]);
                            OnLoopComplete?.Invoke(_currentAnimation);
                            break;
                        }
                    case PlaybackMode.OnceHoldFirst:
                        {
                            _currentFrameIndex = 0;
                            _frameTimer = 0f;
                            _clipTime = 0f;
                            _isPlaying = false;
                            _logger?.LogDebug("Animation finished (OnceHoldFirst): {Name}", _currentAnimation.Name);
                            frames[0].RaiseOnEnter();
                            OnFrameChanged?.Invoke(frames[0]);
                            OnAnimationComplete?.Invoke(_currentAnimation);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                    case PlaybackMode.OnceStop:
                        {
                            _logger?.LogDebug("Animation finished (OnceStop): {Name}", _currentAnimation.Name);
                            var clip = _currentAnimation;
                            CompleteWithClear(clip);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                    case PlaybackMode.OnceHoldLast:
                    default:
                        {
                            _currentFrameIndex = frames.Count - 1;
                            _frameTimer = 0f;
                            _clipTime = _currentAnimation.TotalDuration;
                            _isPlaying = false;
                            _logger?.LogDebug("Animation finished: {Name}", _currentAnimation.Name);
                            OnAnimationComplete?.Invoke(_currentAnimation);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                }
            }
            else
            {
                frames[_currentFrameIndex].RaiseOnEnter();
                OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
            }

            if (_currentAnimation == null)
                return;

            currentFrame = frames[_currentFrameIndex];
        }
    }

    private void AdvanceReversed()
    {
        var frames = _currentAnimation!.Frames;
        var currentFrame = frames[_currentFrameIndex];
        int iterations = 0;

        while (_isPlaying && _currentAnimation != null && _frameTimer >= currentFrame.Duration)
        {
            if (++iterations > MaxFrameAdvanceIterations)
            {
                _logger?.LogWarning(
                    "AdvanceReversed: frame-skip limit ({Limit}) reached for clip '{Name}'. " +
                    "Delta is too large or frame durations are extremely short.",
                    MaxFrameAdvanceIterations, _currentAnimation.Name);
                _frameTimer %= currentFrame.Duration;
                break;
            }

            _frameTimer -= currentFrame.Duration;
            currentFrame.RaiseOnExit();

            if (_currentAnimation == null)
                return;

            _currentFrameIndex--;

            if (_currentFrameIndex < 0)
            {
                switch (_currentAnimation.PlaybackMode)
                {
                    case PlaybackMode.Loop:
                        {
                            if (_loopCountRemaining > 0)
                            {
                                _loopCountRemaining--;
                                if (_loopCountRemaining == 0)
                                {
                                    _currentFrameIndex = 0;
                                    _frameTimer = 0f;
                                    _clipTime = 0f;
                                    _isPlaying = false;
                                    _logger?.LogDebug("Animation finished (RepeatCount reversed): {Name}", _currentAnimation.Name);
                                    OnLoopComplete?.Invoke(_currentAnimation);
                                    OnAnimationComplete?.Invoke(_currentAnimation);
                                    if (!_isPlaying) PlayQueuedIfPending();
                                    return;
                                }
                            }
                            _currentFrameIndex = frames.Count - 1;
                            frames[_currentFrameIndex].RaiseOnEnter();
                            OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
                            OnLoopComplete?.Invoke(_currentAnimation);
                            break;
                        }
                    case PlaybackMode.OnceHoldFirst:
                        {
                            _currentFrameIndex = frames.Count - 1;
                            _frameTimer = 0f;
                            _isPlaying = false;
                            _logger?.LogDebug("Animation finished (OnceHoldFirst reversed): {Name}", _currentAnimation.Name);
                            frames[_currentFrameIndex].RaiseOnEnter();
                            OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
                            OnAnimationComplete?.Invoke(_currentAnimation);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                    case PlaybackMode.OnceStop:
                        {
                            _logger?.LogDebug("Animation finished (OnceStop reversed): {Name}", _currentAnimation.Name);
                            var clip = _currentAnimation;
                            CompleteWithClear(clip);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                    case PlaybackMode.OnceHoldLast:
                    default:
                        {
                            _currentFrameIndex = 0;
                            _frameTimer = 0f;
                            _clipTime = 0f;
                            _isPlaying = false;
                            _logger?.LogDebug("Animation finished (reversed): {Name}", _currentAnimation.Name);
                            OnAnimationComplete?.Invoke(_currentAnimation);
                            if (!_isPlaying) PlayQueuedIfPending();
                            return;
                        }
                }
            }
            else
            {
                frames[_currentFrameIndex].RaiseOnEnter();
                OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
            }

            if (_currentAnimation == null)
                return;

            currentFrame = frames[_currentFrameIndex];
        }
    }

    private void AdvancePingPong()
    {
        var frames = _currentAnimation!.Frames;
        bool isPingPongOnce = _currentAnimation.PlaybackMode == PlaybackMode.PingPongOnce;

        if (frames.Count <= 1)
        {
            if (_frameTimer >= frames[0].Duration)
            {
                _frameTimer %= frames[0].Duration;
                var total1 = _currentAnimation.TotalDuration;
                if (total1 > 0f)
                    _clipTime %= total1 * 2f;

                if (isPingPongOnce)
                {
                    _frameTimer = 0f;
                    _isPlaying = false;
                    _logger?.LogDebug("Animation finished (PingPongOnce single-frame): {Name}", _currentAnimation.Name);
                    OnAnimationComplete?.Invoke(_currentAnimation);
                    if (!_isPlaying) PlayQueuedIfPending();
                }
                else
                {
                    OnLoopComplete?.Invoke(_currentAnimation);

                    if (_currentAnimation != null && _loopCountRemaining > 0)
                    {
                        _loopCountRemaining--;
                        if (_loopCountRemaining == 0)
                        {
                            _frameTimer = 0f;
                            _isPlaying = false;
                            _logger?.LogDebug("Animation finished (PingPong RepeatCount single-frame): {Name}", _currentAnimation.Name);
                            OnAnimationComplete?.Invoke(_currentAnimation);
                            if (!_isPlaying) PlayQueuedIfPending();
                        }
                    }
                }
            }
            return;
        }

        int iterations = 0;
        while (_isPlaying && _currentAnimation != null && _frameTimer >= frames[_currentFrameIndex].Duration)
        {
            if (++iterations > MaxFrameAdvanceIterations)
            {
                _logger?.LogWarning(
                    "AdvancePingPong: frame-skip limit ({Limit}) reached for clip '{Name}'. " +
                    "Delta is too large or frame durations are extremely short.",
                    MaxFrameAdvanceIterations, _currentAnimation.Name);
                _frameTimer %= frames[_currentFrameIndex].Duration;
                break;
            }

            _frameTimer -= frames[_currentFrameIndex].Duration;
            frames[_currentFrameIndex].RaiseOnExit();

            if (_currentAnimation == null)
                return;

            if (_pingPongForward)
            {
                _currentFrameIndex++;
                if (_currentFrameIndex >= frames.Count)
                {
                    var total = _currentAnimation.TotalDuration;
                    if (total > 0f)
                        _clipTime %= total * 2f;

                    if (isPingPongOnce && _pingPongFirstPassDone)
                    {
                        _currentFrameIndex = frames.Count - 1;
                        _frameTimer = 0f;
                        _isPlaying = false;
                        _logger?.LogDebug("Animation finished (PingPongOnce): {Name}", _currentAnimation.Name);
                        frames[_currentFrameIndex].RaiseOnEnter();
                        OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
                        OnAnimationComplete?.Invoke(_currentAnimation);
                        if (!_isPlaying) PlayQueuedIfPending();
                        break;
                    }

                    _pingPongForward = false;
                    _currentFrameIndex = frames.Count - 2;
                    _pingPongFirstPassDone = true;

                    if (!isPingPongOnce)
                        OnLoopComplete?.Invoke(_currentAnimation);
                }
            }
            else
            {
                _currentFrameIndex--;
                if (_currentFrameIndex < 0)
                {
                    var total = _currentAnimation.TotalDuration;
                    if (total > 0f)
                        _clipTime %= total * 2f;

                    if (isPingPongOnce && _pingPongFirstPassDone)
                    {
                        _currentFrameIndex = 0;
                        _frameTimer = 0f;
                        _isPlaying = false;
                        _logger?.LogDebug("Animation finished (PingPongOnce): {Name}", _currentAnimation.Name);
                        frames[0].RaiseOnEnter();
                        OnFrameChanged?.Invoke(frames[0]);
                        OnAnimationComplete?.Invoke(_currentAnimation);
                        if (!_isPlaying) PlayQueuedIfPending();
                        break;
                    }

                    if (!isPingPongOnce)
                    {
                        OnLoopComplete?.Invoke(_currentAnimation);

                        if (_currentAnimation == null)
                            return;

                        if (_loopCountRemaining > 0)
                        {
                            _loopCountRemaining--;
                            if (_loopCountRemaining == 0)
                            {
                                _currentFrameIndex = 0;
                                _frameTimer = 0f;
                                _isPlaying = false;
                                _logger?.LogDebug("Animation finished (PingPong RepeatCount): {Name}", _currentAnimation.Name);
                                frames[0].RaiseOnEnter();
                                OnFrameChanged?.Invoke(frames[0]);
                                OnAnimationComplete?.Invoke(_currentAnimation);
                                if (!_isPlaying) PlayQueuedIfPending();
                                break;
                            }
                        }
                    }

                    _pingPongForward = true;
                    _currentFrameIndex = 1;
                    _pingPongFirstPassDone = true;
                }
            }

            if (_currentAnimation == null)
                return;

            frames[_currentFrameIndex].RaiseOnEnter();
            OnFrameChanged?.Invoke(frames[_currentFrameIndex]);
        }
    }

    private void PlayQueuedIfPending()
    {
        if (_animationQueue.Count == 0)
            return;

        var (name, fadeDuration, directClip) = _animationQueue.Dequeue();

        if (directClip != null)
        {
            if (fadeDuration > 0f)
                PlayDirectWithCrossFade(directClip, fadeDuration);
            else
                PlayDirect(directClip);
        }
        else
        {
            if (fadeDuration > 0f)
                PlayWithCrossFade(name, fadeDuration);
            else
                Play(name);
        }
    }

    private void RemoveFromQueue(string name)
    {
        if (_animationQueue.Count == 0)
            return;

        var items = _animationQueue.ToArray();
        _animationQueue.Clear();
        foreach (var item in items)
        {
            if (item.Name != name)
                _animationQueue.Enqueue(item);
        }
    }
}