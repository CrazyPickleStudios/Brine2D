namespace Brine2D.Animation;

public partial class AnimationStateMachine
{
	/// <summary>
	/// Imperatively stops the animator and clears the active state, firing
	/// <see cref="OnStateChanged"/> with the previous state name and <c>null</c> as the new
	/// state. The state timer is reset and <see cref="SpriteAnimator.Stop"/> is called with
	/// <paramref name="fireCallbacks"/>.
	/// </summary>
	public AnimationStateMachine ForceStop(bool fireCallbacks = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		_suppressDepth++;
		try { _animator.Stop(fireCallbacks); }
		finally { _suppressDepth--; }

		if (previous != null)
			OnStateChanged?.Invoke(previous, null);

		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation.
	/// </summary>
	public AnimationStateMachine ForceState(string animationName, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.Play(animationName, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Queues the named animation to play after the current non-looping clip finishes, or
	/// immediately if nothing is playing.
	/// </summary>
	public AnimationStateMachine ForceStateQueued(string animationName)
	{
		ArgumentNullException.ThrowIfNull(animationName);
		_animator.PlayQueued(animationName);
		return this;
	}

	/// <summary>
	/// Queues the named animation to play with a cross-fade after the current non-looping clip
	/// finishes, or immediately if nothing is playing.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
	public AnimationStateMachine ForceStateQueuedWithCrossFade(string animationName, float fadeDuration)
	{
		ArgumentNullException.ThrowIfNull(animationName);
		if (fadeDuration <= 0f)
			throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");
		_animator.PlayQueuedWithCrossFade(animationName, fadeDuration);
		return this;
	}

	/// <summary>
	/// Queues a clip instance to play directly after the current non-looping clip finishes, or
	/// immediately if nothing is playing. The clip does not need to be pre-registered.
	/// </summary>
	public AnimationStateMachine ForceStateDirectQueued(AnimationClip clip)
	{
		ArgumentNullException.ThrowIfNull(clip);
		_animator.PlayDirectQueued(clip);
		return this;
	}

	/// <summary>
	/// Queues a clip instance to play directly with a cross-fade after the current non-looping
	/// clip finishes, or immediately if nothing is playing. The clip does not need to be
	/// pre-registered.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
	public AnimationStateMachine ForceStateDirectQueuedWithCrossFade(AnimationClip clip, float fadeDuration)
	{
		ArgumentNullException.ThrowIfNull(clip);
		if (fadeDuration <= 0f)
			throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");
		_animator.PlayDirectQueuedWithCrossFade(clip, fadeDuration);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation beginning at a specific frame.
	/// </summary>
	public AnimationStateMachine ForceStateFromFrame(string animationName, int startFrame, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayFromFrame(animationName, startFrame, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation beginning at a normalised time.
	/// </summary>
	public AnimationStateMachine ForceStateFromNormalizedTime(string animationName, float normalizedTime, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayFromNormalizedTime(animationName, normalizedTime, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation with a cross-fade.
	/// </summary>
	public AnimationStateMachine ForceStateWithCrossFade(string animationName, float fadeDuration, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayWithCrossFade(animationName, fadeDuration, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation beginning at a specific frame,
	/// with a cross-fade over <paramref name="fadeDuration"/> seconds.
	/// </summary>
	public AnimationStateMachine ForceStateFromFrameWithCrossFade(string animationName, int startFrame, float fadeDuration, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayFromFrameWithCrossFade(animationName, startFrame, fadeDuration, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to the named animation beginning at a normalised
	/// time, with a cross-fade over <paramref name="fadeDuration"/> seconds.
	/// </summary>
	public AnimationStateMachine ForceStateFromNormalizedTimeWithCrossFade(string animationName, float normalizedTime, float fadeDuration, bool restart = false)
	{
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayFromNormalizedTimeWithCrossFade(animationName, normalizedTime, fadeDuration, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == animationName &&
			(previous != animationName || restart || wasFinished))
			OnStateChanged?.Invoke(previous, animationName);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to play a clip instance directly, without requiring
	/// it to be pre-registered on the animator.
	/// </summary>
	public AnimationStateMachine ForceStateDirect(AnimationClip clip, bool restart = false)
	{
		ArgumentNullException.ThrowIfNull(clip);
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayDirect(clip, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == clip.Name &&
			(previous != clip.Name || restart || wasFinished))
			OnStateChanged?.Invoke(previous, clip.Name);
		return this;
	}

	/// <summary>
	/// Imperatively drives the state machine to play a clip instance directly with a cross-fade,
	/// without requiring it to be pre-registered on the animator.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
	public AnimationStateMachine ForceStateDirectWithCrossFade(AnimationClip clip, float fadeDuration, bool restart = false)
	{
		ArgumentNullException.ThrowIfNull(clip);
		if (fadeDuration <= 0f)
			throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");
		var previous = _animator.CurrentAnimation?.Name;
		var wasFinished = _animator.IsFinished;
		_suppressDepth++;
		try { _animator.PlayDirectWithCrossFade(clip, fadeDuration, restart); }
		finally { _suppressDepth--; }
		if (_animator.CurrentAnimation?.Name == clip.Name &&
			(previous != clip.Name || restart || wasFinished))
			OnStateChanged?.Invoke(previous, clip.Name);
		return this;
	}
}