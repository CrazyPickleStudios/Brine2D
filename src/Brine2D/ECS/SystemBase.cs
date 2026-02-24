using Brine2D.Core;

namespace Brine2D.ECS;

/// <summary>
/// Base class for systems with common functionality.
/// Provides default implementation of IsEnabled property.
/// </summary>
public abstract class SystemBase : ISystem
{
    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;
    
    /// <inheritdoc/>
    public abstract void Update(IEntityWorld world, GameTime gameTime);
}