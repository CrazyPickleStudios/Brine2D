using Brine2D.Core;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Base class for fixed update systems with default implementations.
/// </summary>
public abstract class FixedUpdateSystemBase : IFixedUpdateSystem, IDisposable
{
    private bool _disposed;
    private bool _started;
    private bool _startFailed;

    /// <summary>
    /// Gets whether <see cref="OnStart"/> threw an exception on its last attempt.
    /// When <see langword="true"/>, the system is silently skipped each tick.
    /// Call <see cref="ResetStart"/> to allow <see cref="OnStart"/> to run again.
    /// </summary>
    public bool StartFailed => _startFailed;

    /// <summary>
    /// Clears the start-failed state so that <see cref="OnStart"/> will be retried
    /// on the next tick. Use this to recover from a transient initialization failure.
    /// </summary>
    /// <remarks>
    /// <see cref="OnStart"/> will run again exactly once on the next tick. If it throws
    /// again, <see cref="StartFailed"/> will be set to <see langword="true"/> again.
    /// </remarks>
    public void ResetStart()
    {
        _startFailed = false;
        _started = false;
    }

    /// <summary>
    /// Whether this system is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Execution order for this system. Override to customize.
    /// Default is <see cref="SystemFixedUpdateOrder.Physics"/> (0).
    /// </summary>
    public virtual int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

    /// <summary>
    /// Called once before the first <see cref="FixedUpdate"/> dispatch.
    /// Override to perform initialization that depends on the world being fully set up.
    /// </summary>
    public virtual void OnStart(IEntityWorld world) { }

    void IFixedUpdateSystem.FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        if (_startFailed) return;
        if (!_started)
        {
            _started = true;
            try { OnStart(world); }
            catch { _startFailed = true; throw; }
        }
        FixedUpdate(world, fixedTime);
    }

    /// <summary>
    /// Called at a fixed timestep to update this system.
    /// </summary>
    public abstract void FixedUpdate(IEntityWorld world, GameTime fixedTime);

    /// <summary>
    /// Override to release cached queries and other resources held by this system.
    /// Always call <c>base.Dispose(disposing)</c>.
    /// </summary>
    /// <remarks>
    /// Any <see cref="Brine2D.ECS.Query.CachedEntityQuery{T1}"/> (or higher-arity sibling) created via
    /// <see cref="IEntityWorld.CreateCachedQuery{T1}"/> must be disposed here. Failing to do so leaves
    /// the query registered in the world's invalidation index, preventing GC until the world itself
    /// is disposed.
    /// <code>
    /// private CachedEntityQuery&lt;TransformComponent&gt;? _query;
    ///
    /// public override void OnStart(IEntityWorld world)
    ///     => _query = world.CreateCachedQuery&lt;TransformComponent&gt;().Build();
    ///
    /// protected override void Dispose(bool disposing)
    /// {
    ///     if (disposing) _query?.Dispose();
    ///     base.Dispose(disposing);
    /// }
    /// </code>
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}