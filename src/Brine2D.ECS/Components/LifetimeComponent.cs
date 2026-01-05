using Brine2D.Core;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component that automatically destroys an entity after a specified lifetime.
/// Useful for temporary entities like projectiles, particles, or effects.
/// </summary>
public class LifetimeComponent : Component
{
    /// <summary>
    /// Total lifetime in seconds.
    /// </summary>
    public float Lifetime { get; set; } = 5f;

    /// <summary>
    /// Time remaining before entity is destroyed.
    /// </summary>
    public float TimeRemaining { get; set; }

    /// <summary>
    /// Whether to destroy the entity when lifetime expires.
    /// </summary>
    public bool AutoDestroy { get; set; } = true;

    /// <summary>
    /// Event fired just before the entity is destroyed.
    /// </summary>
    public event Action? OnLifetimeExpired;

    protected internal override void OnAdded()
    {
        base.OnAdded();
        TimeRemaining = Lifetime;
    }

    protected internal override void OnUpdate(GameTime gameTime)
    {
        if (!IsEnabled)
            return;

        TimeRemaining -= (float)gameTime.DeltaTime;

        if (TimeRemaining <= 0)
        {
            OnLifetimeExpired?.Invoke();

            if (AutoDestroy && Entity != null)
            {
                Entity.Destroy();
            }
        }
    }

    /// <summary>
    /// Resets the lifetime to the original duration.
    /// </summary>
    public void ResetLifetime()
    {
        TimeRemaining = Lifetime;
    }

    /// <summary>
    /// Extends the lifetime by the specified amount.
    /// </summary>
    public void ExtendLifetime(float additionalTime)
    {
        TimeRemaining += additionalTime;
    }
}