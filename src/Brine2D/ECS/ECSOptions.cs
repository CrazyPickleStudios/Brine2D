namespace Brine2D.ECS;

/// <summary>
/// Configuration options for the Entity Component System.
/// </summary>
/// <remarks>
/// These options control both the data-oriented ECS (queries, systems) and
/// object-oriented ECS (component methods, update loops) aspects of Brine2D's
/// hybrid ECS architecture.
/// </remarks>
public class ECSOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "ECS";
    
    /// <summary>
    /// Gets or sets the initial entity capacity to pre-allocate.
    /// </summary>
    /// <remarks>
    /// The ECS will pre-allocate internal storage for this many entities
    /// to avoid resizing during gameplay. If you expect more entities,
    /// increase this value to reduce memory allocations during runtime.
    /// </remarks>
    public int InitialEntityCapacity { get; set; } = 1024;
    
    /// <summary>
    /// Gets or sets whether query results should be cached for performance.
    /// </summary>
    /// <remarks>
    /// When enabled, the results of entity queries are cached and only
    /// recomputed when entities are added/removed or components change.
    /// This significantly improves performance for queries that run every frame.
    /// Disable only if you have extreme memory constraints.
    /// </remarks>
    public bool EnableQueryCaching { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether multi-threading is enabled for system execution.
    /// </summary>
    /// <remarks>
    /// When enabled, systems can execute in parallel across multiple threads
    /// for improved performance on multi-core processors. This applies to both
    /// data-oriented systems (via system pipelines) and object-oriented systems
    /// (via parallel entity processing).
    /// </remarks>
    public bool EnableMultiThreading { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the number of worker threads for parallel system execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If null (default), the ECS will use <see cref="Environment.ProcessorCount"/>
    /// to automatically determine the optimal thread count based on available CPU cores.
    /// </para>
    /// <para>
    /// Set to a specific value to limit thread usage (useful for leaving cores
    /// available for other systems or for platforms with limited resources).
    /// </para>
    /// <para>
    /// This setting is only used when <see cref="EnableMultiThreading"/> is true.
    /// </para>
    /// </remarks>
    public int? WorkerThreadCount { get; set; } = null;
    
    /// <summary>
    /// Gets or sets the minimum number of entities required before parallel processing is used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When processing entities in object-oriented ECS mode (calling Update/FixedUpdate
    /// on components), parallel processing has overhead. For small entity counts, 
    /// sequential processing is faster.
    /// </para>
    /// <para>
    /// This threshold determines when to switch from sequential to parallel processing.
    /// If the number of entities being processed is less than this value, they will
    /// be processed sequentially even if <see cref="EnableMultiThreading"/> is true.
    /// </para>
    /// <para>
    /// Default is 100. Lower values use parallel processing more aggressively (better
    /// for CPU-heavy component logic). Higher values prefer sequential processing
    /// (better for lightweight components with high iteration overhead).
    /// </para>
    /// </remarks>
    public int ParallelEntityThreshold { get; set; } = 100;
}