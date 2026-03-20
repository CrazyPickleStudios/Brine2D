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
    /// When <see langword="true"/>, systems execute in parallel across worker threads.
    /// </summary>
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
    /// <see cref="EntityBehavior.FixedUpdate"/>. Default (~16.667ms) gives 60 steps/s.
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