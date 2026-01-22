namespace Brine2D.ECS;

/// <summary>
/// Configuration options for the ECS system.
/// Follows ASP.NET Core configuration patterns.
/// </summary>
public class ECSOptions
{
    /// <summary>
    /// Enables parallel execution of queries and systems.
    /// Default: true on multi-core systems, false on single-core.
    /// </summary>
    public bool EnableParallelExecution { get; set; } = Environment.ProcessorCount > 1;

    /// <summary>
    /// Minimum number of entities required before using parallel execution.
    /// Avoids overhead of threading for small datasets.
    /// Default: 100 entities.
    /// </summary>
    public int ParallelEntityThreshold { get; set; } = 100;

    /// <summary>
    /// Maximum degree of parallelism for query execution.
    /// Default: -1 (use all available cores).
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = -1;
}