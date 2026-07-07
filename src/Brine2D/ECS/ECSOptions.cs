using System.ComponentModel.DataAnnotations;

namespace Brine2D.ECS;

/// <summary>
/// Configuration for the hybrid ECS: entity capacity, parallelism, and fixed-timestep tuning.
/// </summary>
public class ECSOptions
{
    private ParallelOptions? _parallelOptions;
    private int? _workerThreadCount;

    /// <summary>
    /// Pre-allocated entity slot count. Avoids resizing during gameplay.
    /// </summary>
    [Range(16, 1_000_000, ErrorMessage = "InitialEntityCapacity must be between 16 and 1,000,000")]
    public int InitialEntityCapacity { get; set; } = 1000;

    /// <summary>
    /// When <see langword="true"/>, cached query iteration runs in parallel across worker threads
    /// for queries that exceed <see cref="ParallelEntityThreshold"/>.
    /// </summary>
    /// <remarks>
    /// This controls <b>query-iteration parallelism only</b> — system and behavior dispatch
    /// always executes sequentially on the calling thread. Individual systems can additionally
    /// opt out of parallel query iteration by applying <see cref="Systems.SequentialAttribute"/>.
    /// </remarks>
    public bool EnableMultiThreading { get; set; } = true;

    /// <summary>
    /// Worker thread count for parallel execution.
    /// <see langword="null"/> (default) uses <see cref="Environment.ProcessorCount"/>.
    /// Only applies when <see cref="EnableMultiThreading"/> is <see langword="true"/>.
    /// </summary>
    [Range(1, 128, ErrorMessage = "WorkerThreadCount must be between 1 and 128 if specified")]
    public int? WorkerThreadCount
    {
        get => _workerThreadCount;
        set
        {
            _workerThreadCount = value;
            _parallelOptions = null;
        }
    }

    /// <summary>
    /// Entity count below which parallel processing falls back to sequential.
    /// Lower = more aggressive parallelism (good for CPU-heavy logic);
    /// higher = less thread overhead (good for lightweight components).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ParallelEntityThreshold must be at least 1")]
    public int ParallelEntityThreshold { get; set; } = 100;

    /// <summary>
    /// Fixed timestep in milliseconds for <see cref="Systems.IFixedUpdateSystem"/> and
    /// <see cref="Behavior.FixedUpdate"/>. Default (~16.667ms) gives 60 steps/s.
    /// </summary>
    [Range(1.0, 200.0, ErrorMessage = "FixedTimeStepMs must be between 1 and 200")]
    public double FixedTimeStepMs { get; set; } = 1000.0 / 60.0;

    /// <summary>
    /// Maximum fixed steps per frame. Caps the catch-up loop after long frames
    /// (debugger pauses, heavy loads) so the game doesn't freeze. Excess time is discarded.
    /// </summary>
    [Range(1, 60, ErrorMessage = "MaxFixedStepsPerFrame must be between 1 and 60")]
    public int MaxFixedStepsPerFrame { get; set; } = 8;

    /// <summary>
    /// When <see langword="true"/>, exceptions thrown by systems or behaviors are
    /// re-thrown after logging instead of being swallowed. Defaults to
    /// <see langword="true"/> in DEBUG builds and <see langword="false"/> in release
    /// so that a single faulting system does not crash the game in production, but
    /// crashes are surfaced immediately during development.
    /// </summary>
    public bool PropagateExceptions { get; set; } =
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    /// Optional callback invoked when an exception is swallowed because
    /// <see cref="PropagateExceptions"/> is <see langword="false"/>.
    /// Use this to route errors to a crash reporter or telemetry service in release builds
    /// without forcing the exception to propagate and crash the game loop.
    /// </summary>
    /// <remarks>
    /// The callback receives the exception and a context string describing where the
    /// exception originated (e.g., system type name or behavior type name + entity name).
    /// The callback itself must not throw; any exception it raises is silently ignored.
    /// </remarks>
    public Action<Exception, string>? OnExceptionSwallowed { get; set; }

    /// <summary>
    /// Optional callback invoked when the fixed-update accumulator is clamped because
    /// a frame took longer than <see cref="MaxFixedStepsPerFrame"/> × <see cref="FixedTimeStepMs"/>.
    /// The argument is the number of milliseconds of simulation time that were discarded.
    /// </summary>
    /// <remarks>
    /// Use this to route simulation stall events to telemetry or logging in release builds.
    /// The callback must not throw; any exception it raises is silently ignored.
    /// </remarks>
    public Action<double>? OnFixedStepsDiscarded { get; set; }

    /// <summary>
    /// Returns a cached <see cref="ParallelOptions"/> instance derived from
    /// <see cref="WorkerThreadCount"/>. The cached instance is automatically
    /// invalidated when <see cref="WorkerThreadCount"/> changes.
    /// </summary>
    internal ParallelOptions GetParallelOptions()
        => _parallelOptions ??= new ParallelOptions
        {
            MaxDegreeOfParallelism = WorkerThreadCount ?? -1
        };
}