namespace Brine2D.Animation;

public partial class AnimationStateMachine
{
    /// <summary>
    /// Adds a transition from one named animation to another.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddTransition(
        string from,
        string to,
        Func<bool> condition,
        bool canInterrupt = false,
        float crossFadeDuration = 0f,
        float minStateDuration = 0f,
        float minNormalizedTime = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        var t = new AnimationTransition(
            From: from,
            To: to,
            Condition: condition,
            CanInterrupt: canInterrupt,
            CrossFadeDuration: crossFadeDuration,
            MinStateDuration: minStateDuration,
            MinNormalizedTime: minNormalizedTime,
            Priority: priority,
            RestartSelf: restartSelf);
        AddSorted(_transitions, t);
        return t;
    }

    /// <summary>
    /// Adds a transition that fires automatically when the non-looping clip named
    /// <paramref name="from"/> reaches its natural end.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddOnCompleteTransition(
        string from,
        string to,
        Func<bool>? condition = null,
        float crossFadeDuration = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        var t = new AnimationTransition(
            From: from,
            To: to,
            Condition: condition ?? (() => true),
            CanInterrupt: false,
            CrossFadeDuration: crossFadeDuration,
            OnComplete: true,
            Priority: priority,
            RestartSelf: restartSelf);
        AddSorted(_transitions, t);
        return t;
    }

    /// <summary>
    /// Adds a transition that fires automatically when any non-looping clip reaches its natural end.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddAnyOnCompleteTransition(
        string to,
        Func<bool>? condition = null,
        float crossFadeDuration = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        var t = new AnimationTransition(
            From: null,
            To: to,
            Condition: condition ?? (() => true),
            CanInterrupt: false,
            CrossFadeDuration: crossFadeDuration,
            OnComplete: true,
            Priority: priority,
            RestartSelf: restartSelf);
        AddSorted(_anyTransitions, t);
        return t;
    }

    /// <summary>
    /// Adds a transition that can fire from any currently playing animation.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddAnyTransition(
        string to,
        Func<bool> condition,
        bool canInterrupt = true,
        float crossFadeDuration = 0f,
        float minStateDuration = 0f,
        float minNormalizedTime = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        var t = new AnimationTransition(
            From: null,
            To: to,
            Condition: condition,
            CanInterrupt: canInterrupt,
            CrossFadeDuration: crossFadeDuration,
            MinStateDuration: minStateDuration,
            MinNormalizedTime: minNormalizedTime,
            Priority: priority,
            RestartSelf: restartSelf);
        AddSorted(_anyTransitions, t);
        return t;
    }

    /// <summary>
    /// Adds a transition from <paramref name="from"/> to <paramref name="to"/> that fires when
    /// the named trigger is armed, consuming it via <see cref="AnimationTransition.OnFired"/>.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddTriggerTransition(
        string from,
        string to,
        AnimationParameters parameters,
        string triggerName,
        bool canInterrupt = false,
        float crossFadeDuration = 0f,
        float minStateDuration = 0f,
        float minNormalizedTime = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(triggerName);
        var t = new AnimationTransition(
            From: from,
            To: to,
            Condition: () => parameters.IsTriggerArmed(triggerName),
            CanInterrupt: canInterrupt,
            CrossFadeDuration: crossFadeDuration,
            MinStateDuration: minStateDuration,
            MinNormalizedTime: minNormalizedTime,
            Priority: priority,
            RestartSelf: restartSelf)
        {
            OnFired = () => parameters.ResetTrigger(triggerName)
        };
        AddSorted(_transitions, t);
        return t;
    }

    /// <summary>
    /// Adds an AnyState transition to <paramref name="to"/> that fires when the named trigger
    /// is armed, consuming it safely via <see cref="AnimationTransition.OnFired"/>.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddAnyTriggerTransition(
        string to,
        AnimationParameters parameters,
        string triggerName,
        bool canInterrupt = true,
        float crossFadeDuration = 0f,
        float minStateDuration = 0f,
        float minNormalizedTime = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(triggerName);
        var t = new AnimationTransition(
            From: null,
            To: to,
            Condition: () => parameters.IsTriggerArmed(triggerName),
            CanInterrupt: canInterrupt,
            CrossFadeDuration: crossFadeDuration,
            MinStateDuration: minStateDuration,
            MinNormalizedTime: minNormalizedTime,
            Priority: priority,
            RestartSelf: restartSelf)
        {
            OnFired = () => parameters.ResetTrigger(triggerName)
        };
        AddSorted(_anyTransitions, t);
        return t;
    }

    /// <summary>
    /// Adds an on-complete transition from <paramref name="from"/> to <paramref name="to"/> that
    /// additionally requires the named trigger to be armed, consuming it safely on fire.
    /// Returns the created <see cref="AnimationTransition"/> handle for targeted removal.
    /// </summary>
    public AnimationTransition AddOnCompleteTriggerTransition(
        string from,
        string to,
        AnimationParameters parameters,
        string triggerName,
        float crossFadeDuration = 0f,
        int priority = 0,
        bool restartSelf = false)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(triggerName);
        var t = new AnimationTransition(
            From: from,
            To: to,
            Condition: () => parameters.IsTriggerArmed(triggerName),
            CanInterrupt: false,
            CrossFadeDuration: crossFadeDuration,
            OnComplete: true,
            Priority: priority,
            RestartSelf: restartSelf)
        {
            OnFired = () => parameters.ResetTrigger(triggerName)
        };
        AddSorted(_transitions, t);
        return t;
    }

    /// <summary>
    /// Removes all condition-based (non-on-complete) transitions from <paramref name="from"/>
    /// that target <paramref name="to"/>. When <paramref name="from"/> is <c>null</c>, removes
    /// matching AnyState transitions instead.
    /// </summary>
    public void RemoveTransitions(string? from, string to)
    {
        if (from == null)
            _anyTransitions.RemoveAll(t => t.To == to && !t.OnComplete);
        else
            _transitions.RemoveAll(t => t.From == from && t.To == to && !t.OnComplete);
    }

    /// <summary>Removes the specific transition instance obtained from an <c>Add*</c> call.</summary>
    public bool RemoveTransition(AnimationTransition transition) =>
        _transitions.Remove(transition) || _anyTransitions.Remove(transition);

    /// <summary>Removes all AnyState condition-based transitions targeting the given animation.</summary>
    public void RemoveAnyTransitions(string to) =>
        _anyTransitions.RemoveAll(t => t.To == to && !t.OnComplete);

    /// <summary>Removes all on-complete transitions from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public void RemoveOnCompleteTransitions(string from, string to) =>
        _transitions.RemoveAll(t => t.From == from && t.To == to && t.OnComplete);

    /// <summary>Removes all AnyState on-complete transitions targeting the given animation.</summary>
    public void RemoveAnyOnCompleteTransition(string to) =>
        _anyTransitions.RemoveAll(t => t.To == to && t.OnComplete);

    /// <summary>Removes all regular and AnyState transitions.</summary>
    public void ClearTransitions()
    {
        _transitions.Clear();
        _anyTransitions.Clear();
    }

    private static void AddSorted(List<AnimationTransition> list, AnimationTransition transition)
    {
        int insertAt = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Priority < transition.Priority)
            {
                insertAt = i;
                break;
            }
        }
        list.Insert(insertAt, transition);
    }
}